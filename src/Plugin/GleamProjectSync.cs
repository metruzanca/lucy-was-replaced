using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using BepInEx.Logging;
using GleamRuntime;
using UnityEngine;

namespace GleamFarmer
{
    /// <summary>
    /// Maintains the on-disk Gleam project (`&lt;save&gt;/gleam-project/`) that mirrors the
    /// game's code windows, so players can edit `.gleam` files in an external editor
    /// with full Gleam LSP support. The project and the windows stay in sync in both
    /// directions, including file creation/deletion/renaming:
    ///
    /// 1. Windows → files: <see cref="FlushWindows"/> writes every open window with a
    ///    valid module name to `src/&lt;name&gt;.gleam` (called on Run and on game save).
    /// 2. Files → windows: a <see cref="FileSystemWatcher"/> records external `.gleam`
    ///    edits; <see cref="Pump"/> (called every frame on the main thread) pushes them
    ///    into the matching open window, opens a new window for created files, closes
    ///    the window for deleted files, and renames the window on a file rename.
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
        private readonly Dictionary<string, ChangeKind> _changed = new(StringComparer.Ordinal);
        private readonly List<(string Old, string New)> _renames = new();
        private readonly HashSet<string> _deletedExternally = new(StringComparer.Ordinal);
        private string? _projectDir;
        private volatile bool _flushing;

        private enum ChangeKind
        {
            /// <summary>File created or modified (content should be pushed into a window).</summary>
            Upsert,
            /// <summary>File deleted (the matching window should be closed).</summary>
            Delete,
        }

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
        /// pointed at it. Idempotent; safe to call from any thread. Returns true
        /// when the project was created fresh (no src/ dir existed yet), which the
        /// caller can use to decide whether to populate it from the game's windows.
        /// </summary>
        public bool EnsureScaffold()
        {
            if (!Enabled) return false;

            var saveDir = ResolveSaveDir();
            if (saveDir == null) return false;

            var projectDir = GleamProjectSource.ProjectDir(saveDir);
            try
            {
                var srcDir = GleamProjectSource.SrcDir(projectDir);
                var createdFresh = !Directory.Exists(srcDir);
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
                return createdFresh;
            }
            catch (Exception ex)
            {
                _log.LogWarning($"GleamFarmer: could not scaffold Gleam project at {projectDir}: {ex.Message}");
                return false;
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
                    // A file the player deleted externally must not be resurrected by a
                    // flush, even if its window is still open (e.g. mid-execution, where
                    // Pump deliberately skips closing it).
                    if (_deletedExternally.Contains(name)) continue;
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
        /// after the game loads a save. Files that exist in the project but have no
        /// window yet get one opened; existing windows get the file's content.
        /// Skips reserved names (e.g. the `game` stub).
        ///
        /// Once the mirror is established (the project has at least one user module),
        /// windows whose `.gleam` file is missing are closed too, so a file deleted
        /// externally stays deleted (the game would otherwise re-open it from its
        /// `.py` save and our flush would resurrect the `.gleam`). A fresh save with
        /// no `.gleam` files yet is left untouched so its windows survive the first
        /// load.
        /// </summary>
        public void SeedWindowsFromProject()
        {
            if (!Enabled || _projectDir == null) return;

            var workspace = MainSim.Inst?.workspace;
            if (workspace?.codeWindows == null) return;

            var modules = GleamProjectSource.LoadModules(_projectDir);
            foreach (var module in modules)
            {
                if (workspace.codeWindows.TryGetValue(module.Name, out var window))
                    SetCodeText(window, module.Code);
                else
                    OpenWindow(workspace, module.Name, module.Code);
            }

            // Only prune missing files once the mirror is established.
            if (modules.Count == 0) return;
            foreach (var pair in workspace.codeWindows)
            {
                var name = pair.Key;
                if (!IsTrackedModule(name)) continue;
                if (!File.Exists(GleamProjectSource.ModuleFile(_projectDir, name)))
                    CloseWindow(pair.Value);
            }
        }

        /// <summary>
        /// Apply external `.gleam` edits to the game. Called from the Unity main
        /// thread each frame (<see cref="Plugin.Update"/>). Own-write suppression
        /// prevents feedback from <see cref="FlushWindows"/>.
        /// </summary>
        public void Pump()
        {
            if (!Enabled || _projectDir == null) return;

            List<(string Old, string New)> renames;
            Dictionary<string, ChangeKind> changes;
            lock (_lock)
            {
                if (_changed.Count == 0 && _renames.Count == 0) return;
                renames = new List<(string, string)>(_renames);
                _renames.Clear();
                changes = new Dictionary<string, ChangeKind>(_changed);
                _changed.Clear();
            }

            var workspace = MainSim.Inst?.workspace;
            if (workspace?.codeWindows == null) return;

            // Renames first: retitle the window, then reconcile content below.
            foreach (var (oldName, newName) in renames)
            {
                if (!IsTrackedModule(oldName) || !IsTrackedModule(newName)) continue;
                if (!workspace.codeWindows.TryGetValue(oldName, out var window)) continue;
                try
                {
                    window.Rename(newName);
                    var renamedFile = GleamProjectSource.ModuleFile(_projectDir, newName);
                    if (File.Exists(renamedFile))
                        SetCodeText(window, File.ReadAllText(renamedFile));
                }
                catch (Exception ex)
                {
                    _log.LogWarning($"GleamFarmer: could not rename window {oldName} -> {newName}: {ex.Message}");
                }
            }

            foreach (var change in changes)
            {
                var name = change.Key;
                var kind = change.Value;
                if (!IsTrackedModule(name)) continue;
                var file = GleamProjectSource.ModuleFile(_projectDir, name);
                var windowExists = workspace.codeWindows.TryGetValue(name, out var window);

                switch (kind)
                {
                    case ChangeKind.Upsert when File.Exists(file):
                        _deletedExternally.Remove(name);
                        var onDisk = File.ReadAllText(file);
                        if (windowExists)
                        {
                            if (GetCodeText(window) != onDisk)
                                SetCodeText(window, onDisk);
                        }
                        else
                        {
                            // Created externally: open a new code window.
                            OpenWindow(workspace, name, onDisk);
                        }
                        break;
                    case ChangeKind.Delete when windowExists:
                        lock (_lock)
                        {
                            _deletedExternally.Add(name);
                        }
                        CloseWindow(window);
                        break;
                }
            }
        }

        private static bool IsTrackedModule(string name) =>
            GleamModuleNames.IsValidModuleName(name) && !GleamModuleNames.IsReservedName(name);

        private void OpenWindow(Workspace workspace, string name, string code)
        {
            try
            {
                // Place near the view center, like the game's "new window" button.
                workspace.OpenCodeWindow(name, code, -workspace.container.anchoredPosition);
            }
            catch (Exception ex)
            {
                _log.LogWarning($"GleamFarmer: could not open window for {name}.gleam: {ex.Message}");
            }
        }

        private void CloseWindow(CodeWindow window)
        {
            // Match the game's own guard: never close a window mid-execution.
            if (window.isExecuting) return;
            try
            {
                window.GetComponent<Window>().Close();
            }
            catch (Exception ex)
            {
                _log.LogWarning($"GleamFarmer: could not close window {window.fileName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a module's file after its window is closed in-game, so the
        /// project mirrors the game (closing a window removes the module).
        /// </summary>
        public void DeleteModuleFile(string windowName)
        {
            if (!Enabled || _projectDir == null) return;
            if (!GleamModuleNames.IsValidModuleName(windowName) || GleamModuleNames.IsReservedName(windowName))
                return;
            var file = GleamProjectSource.ModuleFile(_projectDir, windowName);
            if (!File.Exists(file)) return;
            try
            {
                File.Delete(file);
            }
            catch (Exception ex)
            {
                _log.LogWarning($"GleamFarmer: could not delete {windowName}.gleam: {ex.Message}");
            }
        }

        /// <summary>
        /// A code window was opened in-game (new window or via the game's own
        /// open): clear any externally-deleted flag so the module's `.gleam` can
        /// be re-created on the next flush.
        /// </summary>
        public void OnWindowOpenedInGame(string windowName)
        {
            if (!Enabled) return;
            if (!GleamModuleNames.IsValidModuleName(windowName) || GleamModuleNames.IsReservedName(windowName))
                return;
            lock (_lock)
            {
                _deletedExternally.Remove(windowName);
            }
        }

        /// <summary>
        /// Rename a module's file when its window is renamed in-game, so the
        /// project mirrors the game (an orphaned old file would otherwise
        /// resurrect the old window on the next load).
        /// </summary>
        public void RenameModuleFile(string oldName, string newName)
        {
            if (!Enabled || _projectDir == null) return;
            if (!GleamModuleNames.IsValidModuleName(oldName) || GleamModuleNames.IsReservedName(oldName) ||
                !GleamModuleNames.IsValidModuleName(newName) || GleamModuleNames.IsReservedName(newName))
                return;
            var oldFile = GleamProjectSource.ModuleFile(_projectDir, oldName);
            var newFile = GleamProjectSource.ModuleFile(_projectDir, newName);
            if (!File.Exists(oldFile)) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newFile)!);
                if (File.Exists(newFile)) File.Delete(newFile);
                File.Move(oldFile, newFile);
            }
            catch (Exception ex)
            {
                _log.LogWarning($"GleamFarmer: could not rename {oldName}.gleam -> {newName}.gleam: {ex.Message}");
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

        private void OnFileEvent(object sender, FileSystemEventArgs e)
        {
            var kind = e.ChangeType == WatcherChangeTypes.Deleted ? ChangeKind.Delete : ChangeKind.Upsert;
            RecordChange(e.Name, kind);
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            var oldName = FileNameToModule(e.OldName);
            var newName = FileNameToModule(e.Name);
            if (oldName == null || newName == null) return;
            lock (_lock)
            {
                if (_flushing) return;
                _renames.Add((oldName, newName));
            }
        }

        private void RecordChange(string? fileName, ChangeKind kind)
        {
            var name = FileNameToModule(fileName);
            if (name == null || GleamModuleNames.IsReservedName(name)) return;
            lock (_lock)
            {
                if (_flushing) return;
                _changed[name] = kind;
            }
        }

        private static string? FileNameToModule(string? fileName)
        {
            if (fileName == null || !fileName.EndsWith(".gleam", StringComparison.OrdinalIgnoreCase))
                return null;
            return Path.GetFileNameWithoutExtension(fileName);
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