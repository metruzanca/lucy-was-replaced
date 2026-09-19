using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using BepInEx.Logging;
using GleamRuntime;

namespace GleamFarmer
{
    /// <summary>
    /// Maintains the on-disk Gleam project (`&lt;save&gt;/gleam-project/`) that mirrors the
    /// game's code windows, so players can edit `.gleam` files in an external editor
    /// with full Gleam LSP support. Three flows keep the two sides in sync:
    ///
    /// 1. Windows → files: <see cref="FlushWindows"/> writes every open window with a
    ///    valid module name to `src/&lt;name&gt;.gleam` (called on Run and on game save).
    /// 2. Files → windows: a <see cref="FileSystemWatcher"/> records external `.gleam`
    ///    edits; <see cref="Pump"/> (called every frame on the main thread) pushes them
    ///    into the matching open window.
    /// 3. On load, windows are seeded from the project files (project is canonical).
    ///
    /// The scaffolded project is a real Gleam package (`gleam.toml` + `manifest.toml`
    /// pinning the embedded gleam_stdlib, plus `src/game.gleam` / `game/item.gleam` /
    /// `game_ffi.mjs` as LSP stubs), so `gleam check` and the language server work.
    /// </summary>
    public sealed class GleamProjectSync
    {
        private readonly ManualLogSource _log;
        private readonly string _embeddedDir;
        private readonly ConfigEntry<bool> _enabled;

        private FileSystemWatcher? _watcher;
        private readonly object _lock = new();
        private readonly HashSet<string> _changed = new(StringComparer.Ordinal);
        private string? _projectDir;
        private volatile bool _flushing;

        public GleamProjectSync(
            ManualLogSource log, string embeddedDir, ConfigEntry<bool> enabled)
        {
            _log = log;
            _embeddedDir = embeddedDir;
            _enabled = enabled;
        }

        public bool Enabled => _enabled.Value;

        /// <summary>Path of the current save's `gleam-project` dir (may be null before the game loads).</summary>
        public string? ProjectDir => _projectDir;

        /// <summary>
        /// Ensure the project exists for the active save and the file watcher is
        /// pointed at it. Idempotent; safe to call from any thread.
        /// </summary>
        public void EnsureScaffold()
        {
            if (!Enabled) return;

            var saveDir = ResolveSaveDir();
            if (saveDir == null) return;

            var projectDir = GleamProjectSource.ProjectDir(saveDir);
            try
            {
                var srcDir = GleamProjectSource.SrcDir(projectDir);
                Directory.CreateDirectory(srcDir);
                Directory.CreateDirectory(Path.Combine(srcDir, "game"));

                CopyIfMissing(
                    Path.Combine(_embeddedDir, "project-template", "gleam.toml"),
                    Path.Combine(projectDir, "gleam.toml"));
                CopyIfMissing(
                    Path.Combine(_embeddedDir, "project-template", "manifest.toml"),
                    Path.Combine(projectDir, "manifest.toml"));
                // The `game` module is the mod's bridge and must stay in lockstep with the
                // embedded copy the runtime compiles from — always refresh the LSP stubs.
                CopyAlways(Path.Combine(_embeddedDir, "game.gleam"), Path.Combine(srcDir, "game.gleam"));
                CopyAlways(Path.Combine(_embeddedDir, "game", "item.gleam"), Path.Combine(srcDir, "game", "item.gleam"));
                CopyAlways(Path.Combine(_embeddedDir, "game_ffi.mjs"), Path.Combine(srcDir, "game_ffi.mjs"));

                if (_projectDir != projectDir)
                {
                    StartWatcher(srcDir);
                    _projectDir = projectDir;
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning($"GleamFarmer: could not scaffold Gleam project at {projectDir}: {ex.Message}");
            }
        }

        /// <summary>Write every open window with a valid module name to `src/&lt;name&gt;.gleam`.</summary>
        public void FlushWindows()
        {
            if (!Enabled || _projectDir == null) return;

            var workspace = MainSim.Inst?.workspace;
            if (workspace?.codeWindows == null) return;

            _flushing = true;
            try
            {
                foreach (var pair in workspace.codeWindows)
                {
                    var name = pair.Key;
                    if (!GleamModuleNames.IsValidModuleName(name) || GleamModuleNames.IsReservedName(name))
                        continue;
                    var text = GetCodeText(pair.Value);
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    var file = GleamProjectSource.ModuleFile(_projectDir, name);
                    Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                    File.WriteAllText(file, text);
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning($"GleamFarmer: could not flush code windows to the Gleam project: {ex.Message}");
            }
            finally
            {
                _flushing = false;
            }
        }

        /// <summary>
        /// Seed open windows from the project files (project is canonical). Called
        /// after the game loads a save. Skips reserved names (e.g. the `game` stub).
        /// </summary>
        public void SeedWindowsFromProject()
        {
            if (!Enabled || _projectDir == null) return;

            var workspace = MainSim.Inst?.workspace;
            if (workspace?.codeWindows == null) return;

            foreach (var module in GleamProjectSource.LoadModules(_projectDir))
            {
                if (!workspace.codeWindows.TryGetValue(module.Name, out var window)) continue;
                SetCodeText(window, module.Code);
            }
        }

        /// <summary>
        /// Apply external `.gleam` edits to open windows. Called from the Unity main
        /// thread each frame (<see cref="Plugin.Update"/>). Own-write suppression
        /// prevents feedback from <see cref="FlushWindows"/>.
        /// </summary>
        public void Pump()
        {
            if (!Enabled || _projectDir == null) return;

            string[] names;
            lock (_lock)
            {
                if (_changed.Count == 0) return;
                names = new string[_changed.Count];
                _changed.CopyTo(names);
                _changed.Clear();
            }

            var workspace = MainSim.Inst?.workspace;
            if (workspace?.codeWindows == null) return;

            foreach (var name in names)
            {
                if (!GleamModuleNames.IsValidModuleName(name) || GleamModuleNames.IsReservedName(name))
                    continue;
                var file = GleamProjectSource.ModuleFile(_projectDir, name);
                if (!File.Exists(file)) continue;
                if (!workspace.codeWindows.TryGetValue(name, out var window)) continue;

                var onDisk = File.ReadAllText(file);
                if (GetCodeText(window) != onDisk)
                    SetCodeText(window, onDisk);
            }
        }

        /// <summary>Resolve the active save's directory using the game's own API.</summary>
        private static string? ResolveSaveDir()
        {
            try
            {
                var saveName = OptionHolder.GetString("activeSave", "Save0");
                return string.IsNullOrEmpty(saveName) ? null : Saver.GetPathOfSaveDirectory(saveName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void StartWatcher(string srcDir)
        {
            try
            {
                _watcher?.Dispose();
                var watcher = new FileSystemWatcher(srcDir)
                {
                    Filter = "*.gleam",
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                    EnableRaisingEvents = true,
                };
                watcher.Changed += OnFileEvent;
                watcher.Created += OnFileEvent;
                watcher.Deleted += OnFileEvent;
                watcher.Renamed += OnRenamed;
                _watcher = watcher;
            }
            catch (Exception ex)
            {
                _log.LogWarning($"GleamFarmer: could not watch the Gleam project: {ex.Message}");
            }
        }

        private void OnFileEvent(object sender, FileSystemEventArgs e) => RecordChange(e.Name);

        private void OnRenamed(object sender, RenamedEventArgs e) => RecordChange(e.Name);

        private void RecordChange(string? fileName)
        {
            if (fileName == null || !fileName.EndsWith(".gleam", StringComparison.OrdinalIgnoreCase))
                return;
            // Skip the LSP stubs we copy in ourselves, and our own flush writes.
            var name = Path.GetFileNameWithoutExtension(fileName);
            if (GleamModuleNames.IsReservedName(name)) return;
            lock (_lock)
            {
                if (_flushing) return;
                _changed.Add(name);
            }
        }

        private void CopyIfMissing(string source, string dest)
        {
            if (File.Exists(dest) || !File.Exists(source)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(source, dest);
        }

        private static void CopyAlways(string source, string dest)
        {
            if (!File.Exists(source)) return;
            if (File.Exists(dest) && File.ReadAllText(dest) == File.ReadAllText(source)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(source, dest, overwrite: true);
        }

        private static string GetCodeText(CodeWindow window)
        {
            var input = HarmonyLib.Traverse.Create(window).Field("codeInput").GetValue();
            if (input == null) return string.Empty;
            var property = input.GetType().GetProperty("text");
            return property?.GetValue(input) as string ?? string.Empty;
        }

        private static void SetCodeText(CodeWindow window, string text)
        {
            var input = HarmonyLib.Traverse.Create(window).Field("codeInput").GetValue();
            if (input == null) return;
            var property = input.GetType().GetProperty("text");
            property?.SetValue(input, text);
        }

        public void Dispose() => _watcher?.Dispose();
    }
}