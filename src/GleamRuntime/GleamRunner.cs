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

        private readonly GleamCompiler _compiler;
        private readonly IReadOnlyList<(string Name, string Code)> _stdlibModules;
        private readonly IReadOnlyList<(string Name, string Code)> _extraModules;
        private readonly IReadOnlyDictionary<string, string> _runtimeFiles;

        /// <param name="stdlibModules">gleam_stdlib module (name, source) pairs.</param>
        /// <param name="runtimeFiles">JS runtime files keyed by module name: "gleam" (prelude), "gleam_stdlib", "dict", plus any FFI externals.</param>
        /// <param name="extraModules">Additional Gleam modules to write into every project (e.g. the "game" FFI module).</param>
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

        /// <summary>
        /// Compile the on-disk Gleam project: the entry module plus every other
        /// player module under `src/` are read from disk, so the runtime compiles
        /// exactly the same bytes an external Gleam LSP sees. The entry module
        /// source is <paramref name="entrySource"/> when provided (the window the
        /// player pressed Run on), else the entry module's file on disk.
        /// </summary>
        /// <param name="projectDir">Path of the `gleam-project` directory.</param>
        /// <param name="entryModuleName">Module name for the entry source (default "main").</param>
        /// <param name="entrySource">Optional override for the entry module's source (the window being run).</param>
        /// <exception cref="GleamCompileException">The program failed to compile.</exception>
        public CompiledGleam CompileFromProject(
            string projectDir,
            string entryModuleName = "main",
            string? entrySource = null)
        {
            var userModules = GleamProjectSource.LoadModules(projectDir)
                .Where(m => m.Name != entryModuleName)
                .ToList();
            var entry = entrySource ?? GleamProjectSource.ReadModule(projectDir, entryModuleName)
                ?? throw new GleamCompileException(
                    $"The entry module '{entryModuleName}.gleam' is missing from the Gleam project.");
            return Compile(entry, entryModuleName, userModules);
        }

        /// <summary>
        /// Compile a Gleam program (the entry module must define a public `main`).
        /// </summary>
        /// <param name="source">The entry module's source.</param>
        /// <param name="entryModuleName">Module name for the entry source (default "main").</param>
        /// <param name="userModules">Other user modules (e.g. the game's extra code windows) to make importable.</param>
        /// <exception cref="GleamCompileException">The program failed to compile.</exception>
        public CompiledGleam Compile(
            string source,
            string entryModuleName = "main",
            IEnumerable<(string Name, string Code)>? userModules = null)
        {
            var modules = userModules?.ToList() ?? new List<(string, string)>();
            var project = _compiler.NewProject();
            try
            {
                foreach (var (name, code) in _stdlibModules)
                    project.WriteModule(name, code);
                foreach (var (name, code) in _extraModules)
                    project.WriteModule(name, code);
                foreach (var (name, code) in modules)
                    project.WriteModule(name, code);

                project.WriteModule(entryModuleName, source);
                project.CompilePackage("javascript");

                var sources = _runtimeFiles.ToDictionary(kv => kv.Key, kv => kv.Value);

                foreach (var (name, _) in _stdlibModules)
                {
                    var js = project.ReadCompiledJavascript(name);
                    if (js != null) sources[name] = RewriteEcho(js);
                }
                foreach (var (name, _) in _extraModules)
                {
                    var js = project.ReadCompiledJavascript(name);
                    if (js != null) sources[name] = RewriteEcho(js);
                }
                foreach (var (name, _) in modules)
                {
                    var js = project.ReadCompiledJavascript(name);
                    if (js != null) sources[name] = RewriteEcho(js);
                }

                var entryJs = project.ReadCompiledJavascript(entryModuleName);
                if (entryJs == null)
                    throw new GleamCompileException(
                        $"The compiler produced no output for the entry module '{entryModuleName}'.");

                sources[entryModuleName] = RewriteEcho(entryJs);
                sources[EntryModule] = EntrySource(entryModuleName);

                // Build JS step → Gleam line maps for the windows' modules (entry + other
                // code windows). Stdlib/game modules have no editor window to highlight.
                var playerModules = new List<(string Name, string Code)> { (entryModuleName, source) };
                playerModules.AddRange(modules);
                var lineMaps = new Dictionary<string, GleamLineMap>();
                foreach (var (name, code) in playerModules)
                {
                    if (!sources.TryGetValue(name, out var js) || js == null) continue;
                    lineMaps[name] = GleamLineMapBuilder.Build(js, GleamStatementScanner.Scan(code));
                }

                return new CompiledGleam(sources, lineMaps);
            }
            finally
            {
                project.Dispose();
            }
        }

        private static string EntrySource(string entryModuleName) =>
            $"import {{ main }} from \"./{entryModuleName}.mjs\";\nmain();\n";

        /// <summary>
        /// The Gleam compiler injects a per-module `echo(value, message, file, line)`
        /// helper (echo.mjs template) that writes to `process.stderr` / `Deno` / falls
        /// back to `console.log` — which would funnel into the paced print handler. This
        /// reroutes echo to the free `__gleam_host.quick_print` sink, keeping the value
        /// inspection and `file:line\nvalue` format. No-op for modules that don't use
        /// echo (the template is embedded verbatim by the pinned compiler).
        /// </summary>
        private static string RewriteEcho(string js)
        {
            const string marker = "function echo(value, message, file, line) {";
            int fnStart = js.IndexOf(marker, StringComparison.Ordinal);
            if (fnStart < 0) return js;

            const string dispatch = "if (globalThis.process?.stderr?.write) {";
            int start = js.IndexOf(dispatch, fnStart, StringComparison.Ordinal);
            if (start < 0) return js;

            // Scan to the matching close brace of the dispatch block (skipping the
            // `else if` / `else` chain).
            int depth = 0;
            int end = start;
            for (; end < js.Length; end++)
            {
                if (js[end] == '{') depth++;
                else if (js[end] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        end++;
                        int peek = end;
                        while (peek < js.Length && char.IsWhiteSpace(js[peek])) peek++;
                        if (peek + 4 <= js.Length &&
                            string.CompareOrdinal(js, peek, "else", 0, 4) == 0)
                            continue;
                        break;
                    }
                }
            }
            if (depth != 0) return js;

            return js.Substring(0, start)
                + "__gleam_host.quick_print(`${file_line}${string_message}\\n${string_value}`);"
                + js.Substring(end);
        }

        public void Dispose() => _compiler.Dispose();
    }

    /// <summary>A compiled, ready-to-run Gleam package.</summary>
    public sealed class CompiledGleam
    {
        private readonly IReadOnlyDictionary<string, string> _sources;
        private readonly IReadOnlyDictionary<string, GleamLineMap> _lineMaps;

        internal CompiledGleam(
            IReadOnlyDictionary<string, string> sources,
            IReadOnlyDictionary<string, GleamLineMap>? lineMaps = null)
        {
            _sources = sources;
            _lineMaps = lineMaps ?? new Dictionary<string, GleamLineMap>();
        }

        /// <summary>The compiled ESM module map (for running on a custom host).</summary>
        public IReadOnlyDictionary<string, string> Sources => _sources;

        /// <summary>JS step → Gleam line maps, keyed by module name (player modules only).</summary>
        public IReadOnlyDictionary<string, GleamLineMap> LineMaps => _lineMaps;

        /// <summary>Execute the package (imports the entry wrapper, which calls main()).</summary>
        public GleamRunResult Run(
            IGleamLogSink? sink = null,
            TimeSpan? timeout = null,
            IGameBridge? bridge = null,
            TickEngine? ticks = null,
            bool enableDrones = false,
            IGleamLineSink? lineSink = null)
        {
            DroneController? drones = null;
            IGameDroneHost? droneHost = null;
            if (enableDrones)
            {
                ticks ??= new TickEngine();
                var stub = bridge ?? new StubGameBridge();
                drones = new DroneController(
                    _sources, sink, timeout ?? TimeSpan.FromSeconds(30), ticks,
                    run: null, cancellation: default, lineSink: lineSink, lineMaps: _lineMaps,
                    id => new StubGameBridge { Ops = ticks.Ops });
                droneHost = new DroneBridge(drones, 0, stub);
            }

            using var js = new JsRuntime(
                _sources, sink, timeout, bridge, cancellationToken: default, ticks: ticks,
                drones: droneHost, lineMaps: _lineMaps, lineSink: lineSink);
            try
            {
                js.RunMain();
                return drones?.Failure != null
                    ? new GleamRunResult(drones.Failure)
                    : new GleamRunResult(null);
            }
            catch (Exception ex)
            {
                return new GleamRunResult(ex);
            }
            finally
            {
                drones?.Dispose();
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