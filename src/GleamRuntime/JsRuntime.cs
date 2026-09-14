using System;
using System.Collections.Generic;
using System.Threading;
using Jint;
using Jint.Native;
using Jint.Runtime.Modules;

namespace GleamRuntime
{
    /// <summary>
    /// Executes a compiled Gleam package (JavaScript) in the embedded Jint engine.
    ///
    /// The compiled ESM modules are served from an in-memory map keyed by their
    /// canonical name (e.g. "main", "gleam/io", "gleam", "gleam_stdlib", "dict").
    /// Relative imports like "./gleam.mjs" and "../gleam_stdlib.mjs" are resolved
    /// against the importing module's key.
    /// </summary>
    public sealed class JsRuntime : IDisposable
    {
        private readonly Engine _engine;

        public JsRuntime(
            IReadOnlyDictionary<string, string> moduleSources,
            IGleamLogSink? sink = null,
            TimeSpan? timeout = null,
            IGameBridge? bridge = null,
            IGleamPrintHandler? print = null,
            CancellationToken cancellationToken = default)
        {
            var loader = new GleamModuleLoader(moduleSources);
            _engine = new Engine(options =>
            {
                options.Modules.ModuleLoader = loader;
                options.TimeoutInterval(timeout ?? TimeSpan.FromSeconds(5));
                options.LimitRecursion(4096);
                options.CancellationToken(cancellationToken);
            });
            _engine.SetValue("console", new ConsoleBridge(sink, print));
            _engine.SetValue("__gleam_host", bridge ?? (object)new StubGameBridge());
        }

        /// <summary>Execute the package: import the entry wrapper, which calls main().</summary>
        public void RunMain() => _engine.Modules.Import("__entry");

        public void Dispose() => _engine.Dispose();

        private sealed class ConsoleBridge
        {
            private readonly IGleamLogSink? _sink;
            private readonly IGleamPrintHandler? _print;

            public ConsoleBridge(IGleamLogSink? sink, IGleamPrintHandler? print)
            {
                _sink = sink;
                _print = print;
            }

            public void log(object? value)
            {
                var message = Convert(value);
                if (_print != null) _print.Print(message);
                else _sink?.Log(message);
            }

            public void error(object? value) => _sink?.Error(Convert(value));

            private static string Convert(object? value) => value switch
            {
                null => "undefined",
                JsValue js => js.ToString(),
                _ => value.ToString() ?? string.Empty,
            };
        }
    }

    /// <summary>
    /// Resolves module specifiers against the compiled-package layout produced by
    /// the Gleam JS backend. All modules live at the virtual package root, so the
    /// prelude is "gleam", externals are "gleam_stdlib"/"dict", and compiled Gleam
    /// modules are "gleam/io", "gleam/list", "main", ...
    /// </summary>
    public sealed class GleamModuleLoader : IModuleLoader
    {
        private readonly IReadOnlyDictionary<string, string> _sources;

        public GleamModuleLoader(IReadOnlyDictionary<string, string> sources) => _sources = sources;

        public ResolvedSpecifier Resolve(string? referencingModuleLocation, ModuleRequest moduleRequest)
        {
            var specifier = moduleRequest.Specifier;
            var key = ResolveKey(referencingModuleLocation, specifier);
            return new ResolvedSpecifier(moduleRequest, key, null, SpecifierType.Bare);
        }

        public Module LoadModule(Engine engine, ResolvedSpecifier resolved)
        {
            if (!_sources.TryGetValue(resolved.Key, out var source))
                throw new ModuleResolutionException(
                    $"Module not found: {resolved.Key}",
                    resolved.Key,
                    parent: null,
                    filePath: null);

            return ModuleFactory.BuildSourceTextModule(engine, resolved, source);
        }

        private static string ResolveKey(string? referencingModuleLocation, string specifier)
        {
            if (!specifier.StartsWith("./") && !specifier.StartsWith("../"))
                return StripMjs(specifier);

            // Directory of the importing module (e.g. "gleam/io" -> "gleam", "main" -> "").
            var location = referencingModuleLocation ?? string.Empty;
            var slash = location.LastIndexOf('/');
            var dir = slash < 0 ? string.Empty : location.Substring(0, slash);

            return Normalize(dir, specifier);
        }

        private static string Normalize(string dir, string specifier)
        {
            var combined = dir.Length == 0 ? specifier : dir + "/" + specifier;
            var segments = combined.Split('/');
            var stack = new List<string>();

            foreach (var segment in segments)
            {
                switch (segment)
                {
                    case "":
                    case ".":
                        break;
                    case "..":
                        if (stack.Count > 0) stack.RemoveAt(stack.Count - 1);
                        break;
                    default:
                        stack.Add(segment);
                        break;
                }
            }

            return StripMjs(string.Join("/", stack));
        }

        private static string StripMjs(string key) =>
            key.EndsWith(".mjs", StringComparison.Ordinal) ? key.Substring(0, key.Length - 4) : key;
    }
}