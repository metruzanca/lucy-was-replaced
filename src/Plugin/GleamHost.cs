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
        private GleamHighlightBridge? _highlight;
        private volatile bool _compiling;
        private volatile bool _stepOnStart;
        private float _lastReblink;

        private GleamHost(GleamRunner runner, ConfigEntry<bool> enabled)
        {
            _runner = runner;
            _enabled = enabled;
        }

        public bool Enabled => _enabled.Value;

        /// <summary>External-editor project sync (the `gleam-project` folder mirror).</summary>
        public GleamProjectSync? ProjectSync { get; private set; }

        /// <summary>Pump main-thread actions queued by the Gleam worker. Called from Update().</summary>
        public void PumpDispatcher() => _dispatcher.Pump();

        /// <summary>Apply external `.gleam` edits to windows. Called from Update().</summary>
        public void PumpProjectSync() => ProjectSync?.Pump();

        /// <summary>Drain pending line highlights into the game's overlay. Called from Update().</summary>
        public void PumpHighlights()
        {
            _highlight?.Pump();

            // While paused in step-through mode, re-blink the current statement so it stays
            // lit instead of fading out after the game's blink interval.
            if (_activeRun?.StepGate is { IsActive: true } && _highlight != null
                && UnityEngine.Time.realtimeSinceStartup - _lastReblink > 0.2f)
            {
                _lastReblink = UnityEngine.Time.realtimeSinceStartup;
                _highlight.ReBlink();
            }
        }

        /// <summary>
        /// Toggle step-through: advance one line when already stepping, otherwise enter step
        /// mode (starting a run if none is active).
        /// </summary>
        public void OnStepPressed(CodeWindow window)
        {
            lock (_runLock)
            {
                if (_activeRun != null)
                {
                    var gate = _activeRun.StepGate;
                    if (gate.IsActive)
                    {
                        gate.Next();
                        return;
                    }
                    gate.Enter();
                    window.StartStepByStepMode();
                    return;
                }
            }

            _stepOnStart = true;
            Run(window);
        }

        public static void Init(ManualLogSource log, ConfigEntry<bool> enabled, ConfigEntry<bool> externalProject)
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

                var docsPath = Path.Combine(embedded, "docs");
                var gameRef = Path.Combine(docsPath, "game-reference.md");
                var primer = Path.Combine(docsPath, "gleam-primer.md");
                if (File.Exists(gameRef))
                {
                    // One combined reference: the game.* library, the beginner primer,
                    // and the stdlib pages extracted at runtime from the embedded sources.
                    var combined = new System.Text.StringBuilder(File.ReadAllText(gameRef));
                    if (File.Exists(primer))
                        combined.Append("\n\n").Append(File.ReadAllText(primer));
                    combined.Append("\n\n").Append(GleamStdlibDocs.Generate(stdlib));
                    GleamDocs.Parse(combined.ToString());
                    Log.LogInfo($"GleamFarmer: in-game reference loaded ({GleamDocs.Builtins().Count} entries).");
                }
                else
                {
                    Log.LogWarning($"GleamFarmer: missing in-game reference: {docsPath}");
                }

                InstallGleamTheme(embedded);
                InstallGleamFirstProgramDoc(embedded, enabled.Value);

                var runner = new GleamRunner(wasmBytes, stdlib, runtimeFiles, extraModules);
                Instance = new GleamHost(runner, enabled);
                Instance.ProjectSync = new GleamProjectSync(Log, embedded, externalProject);
                Log.LogInfo($"GleamFarmer runtime ready ({stdlib.Count} stdlib modules).");
            }
            catch (Exception ex)
            {
                Log.LogError($"GleamFarmer failed to initialise: {ex}");
            }
        }

        /// <summary>
        /// Bundle the Gleam-branded editor theme into the game's theme folder so it shows up
        /// under Settings → color theme. Runs on the Unity main thread from Plugin.Awake; the
        /// game's own ThemeManager.OnEnable rescan picks it up if the manager isn't started yet
        /// (ThemeManager.Inst is null), otherwise we hot-reload it in place.
        /// </summary>
        private static void InstallGleamTheme(string embedded)
        {
            try
            {
                var source = Path.Combine(embedded, "themes", "Gleam.tfwrTheme");
                if (!File.Exists(source))
                {
                    Log.LogWarning($"GleamFarmer: missing bundled theme: {source}");
                    return;
                }

                var themesDir = Path.Combine(Helper.persistentDataPath, "Themes");
                Directory.CreateDirectory(themesDir);
                var dest = Path.Combine(themesDir, "Gleam.tfwrTheme");
                File.Copy(source, dest, overwrite: true);
                ThemeManager.Inst?.ReloadJsonThemes();
                Log.LogInfo("GleamFarmer: installed the Gleam editor theme.");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"GleamFarmer: failed to install the Gleam editor theme: {ex.Message}");
            }
        }

        /// <summary>
        /// Rewrite the base game's "First Program" docs page (docs/first_program.md, English)
        /// with the bundled Gleam version, so the info panel's starting tutorial matches what
        /// the player actually writes. English only for now; other languages keep the game's
        /// Python page. Runs on the Unity main thread from Plugin.Awake.
        /// </summary>
        private static void InstallGleamFirstProgramDoc(string embedded, bool gleamMode)
        {
            if (!gleamMode) return;
            try
            {
                var source = Path.Combine(embedded, "docs", "first_program.md");
                if (!File.Exists(source))
                {
                    Log.LogWarning($"GleamFarmer: missing bundled first-program doc: {source}");
                    return;
                }

                var dest = Path.Combine(
                    UnityEngine.Application.streamingAssetsPath,
                    "Languages", "EN", "docs", "first_program.md");
                File.Copy(source, dest, overwrite: true);
                Log.LogInfo("GleamFarmer: rewrote the in-game 'First Program' docs page.");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"GleamFarmer: could not rewrite the 'First Program' docs page: {ex.Message}");
            }
        }

        /// <summary>Run/stop the code in the given window. Returns true if handled.</summary>
        public bool Run(CodeWindow window)
        {
            lock (_runLock)
            {
                if (_activeRun != null)
                {
                    // While stepping, Execute exits step mode (and the run continues).
                    var gate = _activeRun.StepGate;
                    if (gate is { IsActive: true })
                    {
                        Log.LogInfo("GleamFarmer: exiting step mode…");
                        gate.Exit();
                        window.StartExecutionMode();
                        return true;
                    }

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

                // Snapshot the windows + line offsets for the live highlight (the worker
                // thread must not touch Unity objects).
                _highlight?.Reset();
                _highlight = BuildHighlightBridge(window, entryName);

                // Compile off the Unity main thread so the WASM compile (~100-300ms) doesn't
                // stutter the game; the cheap, Unity-touching window gathering above stays on
                // the main thread.
                _compiling = true;
                if (ProjectSync is { Enabled: true })
                {
                    // External-editor mode: persist every window to the project first, then
                    // compile the on-disk project so the LSP and the runtime read the same bytes.
                    // If the save dir can't be resolved yet (rare, before the game loads),
                    // fall back to compiling the window text in-memory.
                    ProjectSync.EnsureScaffold();
                    if (ProjectSync.ProjectDir != null)
                    {
                        ProjectSync.FlushWindows();
                        new Thread(() => CompileProjectAndStart(window, source, entryName))
                        {
                            IsBackground = true,
                            Name = "GleamFarmer-compile",
                        }.Start();
                    }
                    else
                    {
                        new Thread(() => CompileAndStart(window, source, entryName, userModules))
                        {
                            IsBackground = true,
                            Name = "GleamFarmer-compile",
                        }.Start();
                    }
                }
                else
                {
                    new Thread(() => CompileAndStart(window, source, entryName, userModules))
                    {
                        IsBackground = true,
                        Name = "GleamFarmer-compile",
                    }.Start();
                }
                return true;
            }
            catch (Exception ex)
            {
                _compiling = false;
                ShowError(window, ex.Message);
                return true;
            }
        }

        /// <summary>Compile the on-disk project (the window being run is the entry module).</summary>
        private void CompileProjectAndStart(CodeWindow window, string source, string entryName)
        {
            try
            {
                var projectDir = ProjectSync?.ProjectDir;
                if (projectDir == null)
                    throw new InvalidOperationException("Gleam project not scaffolded.");
                var compiled = _runner.CompileFromProject(projectDir, entryName, source);
                _dispatcher.Invoke(() =>
                {
                    var sink = new PluginSink(Log);
                    var run = new PacedGleamRun(compiled, _dispatcher, sink, _highlight);
                    if (_stepOnStart) run.StepGate.Enter();
                    _stepOnStart = false;
                    run.Completed += () => OnRunFinished(window, run, sink, error: null);
                    run.Failed += error => OnRunFinished(window, run, sink, error);

                    lock (_runLock)
                    {
                        if (_activeRun != null) return true; // superseded before start
                        _activeRun = run;
                    }

                    window.StartExecutionMode();
                    if (run.StepGate.IsActive) window.StartStepByStepMode();
                    window.SetExecutionColor();
                    run.Start();
                    Log.LogInfo("GleamFarmer: run started (project).");
                    return true;
                });
            }
            catch (GleamCompileException ex)
            {
                _dispatcher.Invoke(() => { ShowError(window, ex.Message, entryName); return true; });
            }
            catch (Exception ex)
            {
                _dispatcher.Invoke(() => { ShowError(window, ex.Message, entryName); return true; });
            }
            finally
            {
                _compiling = false;
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
                    var run = new PacedGleamRun(compiled, _dispatcher, sink, _highlight);
                    if (_stepOnStart) run.StepGate.Enter();
                    _stepOnStart = false;
                    run.Completed += () => OnRunFinished(window, run, sink, error: null);
                    run.Failed += error => OnRunFinished(window, run, sink, error);

                    lock (_runLock)
                    {
                        if (_activeRun != null) return true; // superseded before start
                        _activeRun = run;
                    }

                    window.StartExecutionMode();
                    if (run.StepGate.IsActive) window.StartStepByStepMode();
                    window.SetExecutionColor();
                    run.Start();
                    Log.LogInfo("GleamFarmer: run started.");
                    return true;
                });
            }
            catch (GleamCompileException ex)
            {
                _dispatcher.Invoke(() => { ShowError(window, ex.Message, entryName); return true; });
            }
            catch (Exception ex)
            {
                _dispatcher.Invoke(() => { ShowError(window, ex.Message, entryName); return true; });
            }
            finally
            {
                _compiling = false;
            }
        }

        private void OnRunFinished(CodeWindow window, PacedGleamRun run, PluginSink sink, Exception? error)
        {
            _stepOnStart = false;
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
                // The BepInEx log keeps the full exception for debugging; the code
                // window gets a player-facing version (e.g. the `todo` placeholder
                // is explained instead of dumped as a Jint stack trace).
                Log.LogError($"GleamFarmer: runtime error: {real}");
                _dispatcher.Invoke(() =>
                {
                    ShowError(window, GleamErrors.Describe(real));
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

        private static void ShowError(CodeWindow window, string message, string? entryModuleName = null)
        {
            Log.LogError($"GleamFarmer error:\n{message}");

            // Compile diagnostics are formatted `src/<module>.gleam:N:C`; the window
            // being run is the entry module, so highlight against its text.
            var module = entryModuleName ?? "main";
            var match = Regex.Match(message, $@"{Regex.Escape(module)}\.gleam:(\d+):(\d+)");
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

        /// <summary>
        /// Snapshot the open code windows and their per-line char offsets so the worker
        /// thread can map executed Gleam lines to highlight ranges without touching Unity.
        /// Every validly-named window is importable, so module → window is just the map.
        /// </summary>
        private static GleamHighlightBridge? BuildHighlightBridge(CodeWindow active, string entryName)
        {
            var workspace = MainSim.Inst?.workspace;
            if (workspace?.codeWindows == null) return null;

            var windows = new Dictionary<string, (CodeWindow, int[])>();
            foreach (var pair in workspace.codeWindows)
            {
                var name = pair.Key;
                var window = pair.Value;
                if (ReferenceEquals(window, active) || !GleamModuleNames.IsValidModuleName(name))
                    continue;
                windows[name] = (window, ComputeLineOffsets(GetCodeText(window)));
            }
            windows[entryName] = (active, ComputeLineOffsets(GetCodeText(active)));
            return new GleamHighlightBridge(windows);
        }

        private static int[] ComputeLineOffsets(string text)
        {
            var offsets = new List<int> { 0 };
            for (var i = 0; i < text.Length; i++)
                if (text[i] == '\n') offsets.Add(i + 1);
            return offsets.ToArray();
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