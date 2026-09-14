using System;
using System.Collections.Generic;
using System.Linq;

namespace GleamRuntime
{
    /// <summary>
    /// Compiles Gleam source into a runnable package: stdlib + extra modules
    /// (e.g. the `tfwr` FFI module) + player module are written into a virtual
    /// project, compiled to JavaScript, and each module's ESM is collected into
    /// an in-memory map for the Jint host.
    /// </summary>
    public sealed class GleamRunner : IDisposable
    {
        private const string EntryModule = "__entry";
        private const string EntrySource = "import { main } from \"./main.mjs\";\nmain();\n";

        private readonly GleamCompiler _compiler;
        private readonly IReadOnlyList<(string Name, string Code)> _stdlibModules;
        private readonly IReadOnlyList<(string Name, string Code)> _extraModules;
        private readonly IReadOnlyDictionary<string, string> _runtimeFiles;

        /// <param name="stdlibModules">gleam_stdlib module (name, source) pairs.</param>
        /// <param name="runtimeFiles">JS runtime files keyed by module name: "gleam" (prelude), "gleam_stdlib", "dict", plus any FFI externals.</param>
        /// <param name="extraModules">Additional Gleam modules to write into every project (e.g. the "tfwr" FFI module).</param>
        public GleamRunner(
            byte[] wasmBytes,
            IReadOnlyList<(string Name, string Code)> stdlibModules,
            IReadOnlyDictionary<string, string> runtimeFiles,
            IReadOnlyList<(string Name, string Code)>? extraModules = null)
        {
            _compiler = new GleamCompiler(wasmBytes);
            _stdlibModules = stdlibModules;
            _runtimeFiles = runtimeFiles;
            _extraModules = extraModules ?? Array.Empty<(string, string)>();
        }

        /// <summary>Compile a Gleam program (must define a public `main`).</summary>
        /// <exception cref="GleamCompileException">The program failed to compile.</exception>
        public CompiledGleam Compile(string source)
        {
            var project = _compiler.NewProject();
            try
            {
                foreach (var (name, code) in _stdlibModules)
                    project.WriteModule(name, code);
                foreach (var (name, code) in _extraModules)
                    project.WriteModule(name, code);

                project.WriteModule("main", source);
                project.CompilePackage("javascript");

                var sources = _runtimeFiles.ToDictionary(kv => kv.Key, kv => kv.Value);

                foreach (var (name, _) in _stdlibModules)
                {
                    var js = project.ReadCompiledJavascript(name);
                    if (js != null) sources[name] = js;
                }
                foreach (var (name, _) in _extraModules)
                {
                    var js = project.ReadCompiledJavascript(name);
                    if (js != null) sources[name] = js;
                }

                var mainJs = project.ReadCompiledJavascript("main");
                if (mainJs == null)
                    throw new GleamCompileException("The compiler produced no output for the main module.");

                sources["main"] = mainJs;
                sources[EntryModule] = EntrySource;

                return new CompiledGleam(sources);
            }
            finally
            {
                project.Dispose();
            }
        }

        public void Dispose() => _compiler.Dispose();
    }

    /// <summary>A compiled, ready-to-run Gleam package.</summary>
    public sealed class CompiledGleam
    {
        private readonly IReadOnlyDictionary<string, string> _sources;

        internal CompiledGleam(IReadOnlyDictionary<string, string> sources) => _sources = sources;

        /// <summary>Execute the package (imports the entry wrapper, which calls main()).</summary>
        public GleamRunResult Run(IGleamLogSink? sink = null, TimeSpan? timeout = null)
        {
            using var js = new JsRuntime(_sources, sink, timeout);
            try
            {
                js.RunMain();
                return new GleamRunResult(null);
            }
            catch (Exception ex)
            {
                return new GleamRunResult(ex);
            }
        }
    }

    /// <summary>Outcome of running a compiled package.</summary>
    public sealed class GleamRunResult
    {
        public GleamRunResult(Exception? error) => Error = error;

        public Exception? Error { get; }
        public bool IsOk => Error == null;
    }
}