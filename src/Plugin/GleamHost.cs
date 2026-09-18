using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
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

        private static ManualLogSource Log = null!;

        private readonly GleamRunner _runner;
        private readonly ConfigEntry<bool> _enabled;
        private readonly MainThreadDispatcher _dispatcher = new();
        private readonly object _runLock = new();
        private PacedGleamRun? _activeRun;
        private volatile bool _compiling;

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

                var docsPath = Path.Combine(embedded, "docs", "game-reference.md");
                if (File.Exists(docsPath))
                {
                    GleamDocs.Parse(File.ReadAllText(docsPath));
                    Log.LogInfo($"GleamFarmer: in-game reference loaded ({GleamDocs.Builtins().Count} game functions).");
                }
                else
                {
                    Log.LogWarning($"GleamFarmer: missing in-game reference: {docsPath}");
                }

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
                    var run = _activeRun;
                    _activeRun = null;
                    run.Stop();                 // non-blocking; the worker cleans up + fires Completed (stale-guarded)
                    window.StopExecutionMode(); // immediate UI feedback
                    return true;
                }
            }

            if (_compiling)
            {
                Log.LogInfo("GleamFarmer: still compiling, ignoring Run…");
                return true;
            }

            try
            {
                var source = GetCodeText(window);
                if (string.IsNullOrWhiteSpace(source))
                    return true;

                // Every other code window is an importable Gleam module, just like the
                // game's own Python. The window being run is the entry module.
                var userModules = CollectUserModules(window, out var entryName);
                var moduleNames = new List<string> { entryName };
                moduleNames.AddRange(userModules.Select(m => m.Item1));
                Log.LogInfo($"GleamFarmer: compiling… modules: {string.Join(", ", moduleNames)}");

                // Compile off the Unity main thread so the WASM compile (~100-300ms) doesn't
                // stutter the game; the cheap, Unity-touching window gathering above stays on
                // the main thread.
                _compiling = true;
                new Thread(() => CompileAndStart(window, source, entryName, userModules))
                {
                    IsBackground = true,
                    Name = "GleamFarmer-compile",
                }.Start();
                return true;
            }
            catch (Exception ex)
            {
                _compiling = false;
                ShowError(window, ex.Message);
                return true;
            }
        }

        /// <summary>Runs on a background thread: compile, then start the run on the main thread.</summary>
        private void CompileAndStart(
            CodeWindow window, string source, string entryName, List<(string, string)> userModules)
        {
            try
            {
                var compiled = _runner.Compile(source, entryName, userModules);
                _dispatcher.Invoke(() =>
                {
                    var sink = new PluginSink(Log);
                    var run = new PacedGleamRun(compiled, _dispatcher, sink);
                    run.Completed += () => OnRunFinished(window, run, sink, error: null);
                    run.Failed += error => OnRunFinished(window, run, sink, error);

                    lock (_runLock)
                    {
                        if (_activeRun != null) return true; // superseded before start
                        _activeRun = run;
                    }

                    window.StartExecutionMode();
                    run.Start();
                    Log.LogInfo("GleamFarmer: run started.");
                    return true;
                });
            }
            catch (GleamCompileException ex)
            {
                _dispatcher.Invoke(() => { ShowError(window, ex.Message); return true; });
            }
            catch (Exception ex)
            {
                _dispatcher.Invoke(() => { ShowError(window, ex.Message); return true; });
            }
            finally
            {
                _compiling = false;
            }
        }

        private void OnRunFinished(CodeWindow window, PacedGleamRun run, PluginSink sink, Exception? error)
        {
            lock (_runLock)
            {
                // A stopped run's Completed can fire after the player already started (or is
                // starting) the next one; only the current run may touch the UI/state.
                if (!ReferenceEquals(run, _activeRun)) return;
                _activeRun = null;
            }

            if (error != null)
            {
                // The main-thread dispatcher wraps game-side failures in an AggregateException;
                // unwrap to the real error so the player sees the actual cause, not the wrapper.
                var real = Unwrap(error);
                Log.LogError($"GleamFarmer: runtime error: {real}");
                _dispatcher.Invoke(() =>
                {
                    ShowError(window, real.ToString());
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

        private static Exception Unwrap(Exception ex)
        {
            while (ex is AggregateException aggregate && aggregate.InnerExceptions.Count == 1)
                ex = aggregate.InnerExceptions[0];
            return ex;
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

        /// <summary>
        /// Collect the other open code windows as importable Gleam modules. The window being
        /// run becomes the entry module (by its own name when that is a valid Gleam module
        /// name, else "main"); windows whose names are not valid Gleam module names or would
        /// shadow the mod's own modules (e.g. "game") are left out.
        /// </summary>
        private static List<(string Name, string Code)> CollectUserModules(
            CodeWindow active, out string entryModuleName)
        {
            var modules = new List<(string, string)>();
            entryModuleName = "main";

            var workspace = MainSim.Inst?.workspace;
            if (workspace?.codeWindows == null) return modules;

            var activeName = "main";
            foreach (var pair in workspace.codeWindows)
            {
                if (ReferenceEquals(pair.Value, active)) { activeName = pair.Key; break; }
            }
            if (GleamModuleNames.IsValidModuleName(activeName)) entryModuleName = activeName;

            foreach (var pair in workspace.codeWindows)
            {
                if (ReferenceEquals(pair.Value, active)) continue;
                var name = pair.Key;
                if (!GleamModuleNames.IsValidModuleName(name) || GleamModuleNames.IsReservedName(name))
                    continue;
                var text = GetCodeText(pair.Value);
                if (string.IsNullOrWhiteSpace(text)) continue;
                modules.Add((name, text));
            }
            return modules;
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