using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GleamRuntime;
using Xunit;

namespace GleamRuntime.Tests
{
    /// <summary>
    /// One shared runner for the whole test collection: the wasm compiler is
    /// expensive to instantiate (~seconds), so compile+run only once.
    /// </summary>
    public sealed class GleamFixture : IDisposable
    {
        public GleamFixture()
        {
            var baseDir = AppContext.BaseDirectory;
            var embedded = Path.Combine(baseDir, "Embedded");

            WasmBytes = File.ReadAllBytes(Path.Combine(embedded, "gleam_wasm_bg.wasm"));
            var stdlibDir = Path.Combine(embedded, "stdlib");

            Stdlib = GleamStdlib.LoadSources(stdlibDir);
            GameModules = GleamStdlib.LoadGameModules(embedded);
            RuntimeFiles = new Dictionary<string, string>
            {
                ["gleam"] = GleamStdlib.LoadPrelude(embedded),
                ["gleam_stdlib"] = GleamStdlib.LoadExternal(stdlibDir, "gleam_stdlib.mjs"),
                ["dict"] = GleamStdlib.LoadExternal(stdlibDir, "dict.mjs"),
                ["game_ffi"] = File.ReadAllText(Path.Combine(embedded, "game_ffi.mjs")),
            };
            ExtraModules = new List<(string, string)>(GameModules);

            Runner = new GleamRunner(WasmBytes, Stdlib, RuntimeFiles, ExtraModules);
        }

        public byte[] WasmBytes { get; }
        public IReadOnlyList<(string Name, string Code)> Stdlib { get; }
        public IReadOnlyList<(string Name, string Code)> GameModules { get; }
        public IReadOnlyList<(string Name, string Code)> ExtraModules { get; }
        public IReadOnlyDictionary<string, string> RuntimeFiles { get; }
        public GleamRunner Runner { get; }

        public void Dispose() => Runner.Dispose();
    }

    [CollectionDefinition("gleam")]
    public sealed class GleamCollection : ICollectionFixture<GleamFixture> { }

    /// <summary>Captures console output routed through the Jint bridge.</summary>
    public sealed class CapturingSink : IGleamLogSink
    {
        public List<string> Output { get; } = new();
        public List<string> Errors { get; } = new();

        public void Log(string message) => Output.Add(message);
        public void Error(string message) => Errors.Add(message);
    }
}