using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using BepInEx.Logging;
using GleamRuntime;

namespace GleamFarmer
{
    /// <summary>
    /// Owns the Gleam runtime for the plugin: loads the embedded wasm compiler +
    /// stdlib, compiles the editor's text, and runs it paced on a worker thread.
    /// The Run/Execute button toggles: press once to start, again to stop.
    /// </summary>
    public sealed class GleamHost
    {
        public static GleamHost? Instance { get; private set; }

        /// <summary>The plugin's BepInEx log source (for other patches).</summary>
        internal static ManualLogSource LogSource => Log;

        private static ManualLogSource Log = null!;

        private readonly GleamRunner _runner;
        private readonly ConfigEntry<bool> _enabled;
        private readonly MainThreadDispatcher _dispatcher = new();
        private readonly object _runLock = new();
        private PacedGleamRun? _activeRun;

        private GleamHost(GleamRunner runner, ConfigEntry<bool> enabled)
        {
            _runner = runner;
            _enabled = enabled;
        }

        public bool Enabled => _enabled.Value;

        /// <summary>Pump main-thread actions queued by the Gleam worker. Called from Update().</summary>
        public void PumpDispatcher() => _dispatcher.Pump();

        public static void Init(ManualLogSource log, ConfigEntry<bool> enabled)
        {
            Log = log;
            try
            {
                var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                    ?? throw new InvalidOperationException("cannot locate plugin directory");

                // Make Mono find the native wasmtime.dll (P/Invoke "wasmtime").
                var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                if (!path.Split(';').Contains(dir, StringComparer.OrdinalIgnoreCase))
                    Environment.SetEnvironmentVariable("PATH", dir + ";" + path);

                var embedded = Path.Combine(dir, "Embedded");
                var wasmPath = Path.Combine(embedded, "gleam_wasm_bg.wasm");
                var stdlibDir = Path.Combine(embedded, "stdlib");

                foreach (var required in new[] { wasmPath, stdlibDir })
                {
                    if (!Directory.Exists(required) && !File.Exists(required))
                    {
                        Log.LogError($"GleamFarmer: missing embedded asset: {required}");
                        return;
                    }
                }

                var wasmBytes = File.ReadAllBytes(wasmPath);
                var stdlib = GleamStdlib.LoadSources(stdlibDir);
                var runtimeFiles = new Dictionary<string, string>
                {
                    ["gleam"] = GleamStdlib.LoadPrelude(embedded),
                    ["gleam_stdlib"] = GleamStdlib.LoadExternal(stdlibDir, "gleam_stdlib.mjs"),
                    ["dict"] = GleamStdlib.LoadExternal(stdlibDir, "dict.mjs"),
                    ["game_ffi"] = File.ReadAllText(Path.Combine(embedded, "game_ffi.mjs")),
                };
                var extraModules = GleamStdlib.LoadGameModules(embedded);

                var runner = new GleamRunner(wasmBytes, stdlib, runtimeFiles, extraModules);
                Instance = new GleamHost(runner, enabled);
                Log.LogInfo($"GleamFarmer runtime ready ({stdlib.Count} stdlib modules).");
            }
            catch (Exception ex)
            {
                Log.LogError($"GleamFarmer failed to initialise: {ex}");
            }
        }

        /// <summary>Run/stop the code in the given window. Returns true if handled.</summary>
        public bool Run(CodeWindow window)
        {
            lock (_runLock)
            {
                if (_activeRun != null)
                {
                    Log.LogInfo("GleamFarmer: stopping run…");
                    _activeRun.Dispose();
                    _activeRun = null;
                    window.StopExecutionMode();
                    return true;
                }
            }

            try
            {
                var source = GetCodeText(window);
                if (string.IsNullOrWhiteSpace(source))
                    return true;

                Log.LogInfo("GleamFarmer: compiling…");
                var compiled = _runner.Compile(source);

                var sink = new PluginSink(Log);
                var run = new PacedGleamRun(compiled, _dispatcher, sink);
                run.Completed += () => OnRunFinished(window, sink, error: null);
                run.Failed += error => OnRunFinished(window, sink, error);

                lock (_runLock)
                {
                    _activeRun = run;
                }

                window.StartExecutionMode();
                run.Start();
                Log.LogInfo("GleamFarmer: run started.");
                return true;
            }
            catch (GleamCompileException ex)
            {
                ShowError(window, ex.Message);
                return true;
            }
            catch (Exception ex)
            {
                ShowError(window, ex.Message);
                return true;
            }
        }

        private void OnRunFinished(CodeWindow window, PluginSink sink, Exception? error)
        {
            lock (_runLock)
            {
                _activeRun = null;
            }

            if (error != null)
            {
                Log.LogError($"GleamFarmer: runtime error: {error.Message}");
                _dispatcher.Invoke(() =>
                {
                    ShowError(window, error.Message);
                    return true;
                });
            }
            else
            {
                Log.LogInfo($"GleamFarmer: run finished. Output: {sink.Output.Count} lines");
                _dispatcher.Invoke(() =>
                {
                    window.StopExecutionMode();
                    return true;
                });
            }
        }

        private static void ShowError(CodeWindow window, string message)
        {
            Log.LogError($"GleamFarmer error:\n{message}");

            var match = Regex.Match(message, @"main\.gleam:(\d+):(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var line) &&
                int.TryParse(match.Groups[2].Value, out var column))
            {
                var text = GetCodeText(window);
                var offset = LineColumnToOffset(text, line, column);
                window.SetErrorMessage(message, offset, Math.Min(offset + 1, text.Length));
            }
            else
            {
                window.SetErrorMessage(message, 0, 0);
            }
        }

        private static string GetCodeText(CodeWindow window)
        {
            var input = HarmonyLib.Traverse.Create(window).Field("codeInput").GetValue();
            if (input == null) return string.Empty;
            var property = input.GetType().GetProperty("text");
            return property?.GetValue(input) as string ?? string.Empty;
        }

        private static int LineColumnToOffset(string text, int line, int column)
        {
            var index = 0;
            for (var i = 1; i < line; i++)
            {
                index = text.IndexOf('\n', index);
                if (index < 0) return Math.Min(column - 1, text.Length);
                index++;
            }
            return Math.Min(index + column - 1, text.Length);
        }
    }

    /// <summary>Collects console output and mirrors it to the BepInEx log.</summary>
    internal sealed class PluginSink : IGleamLogSink
    {
        private readonly ManualLogSource _log;

        public PluginSink(ManualLogSource log) => _log = log;

        public List<string> Output { get; } = new();

        public void Log(string message)
        {
            Output.Add(message);
            _log.LogInfo("[gleam] " + message);
        }

        public void Error(string message)
        {
            Output.Add(message);
            _log.LogError("[gleam] " + message);
        }
    }
}