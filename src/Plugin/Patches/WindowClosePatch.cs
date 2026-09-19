using HarmonyLib;

namespace GleamFarmer
{
    /// <summary>
    /// Keep the on-disk Gleam project in sync with in-game window lifecycle:
    ///
    /// - <see cref="Window.Close"/> removes the `.gleam` file for the closed code
    ///   window, so the project mirrors the game (the game itself deletes the stale
    ///   `.py` for closed windows on the next save).
    /// - <see cref="Workspace.RenameWindow"/> renames the `.gleam` file, so the
    ///   project mirrors a window renamed in-game.
    ///
    /// The reverse direction (create/delete/rename in the editor → game) is handled
    /// by <see cref="GleamProjectSync.Pump"/>.
    /// </summary>
    [HarmonyPatch(typeof(Window), nameof(Window.Close))]
    public static class WindowClosePatch
    {
        [HarmonyPostfix]
        public static void OnClose(Window __instance)
        {
            if (!__instance.TryGetComponent<CodeWindow>(out var codeWindow)) return;
            var sync = GleamHost.Instance?.ProjectSync;
            if (sync == null || !sync.Enabled) return;
            sync.DeleteModuleFile(codeWindow.fileName);
        }
    }

    [HarmonyPatch(typeof(Workspace), nameof(Workspace.RenameWindow))]
    public static class WindowRenamePatch
    {
        [HarmonyPostfix]
        public static void OnRename(Window window, string newWindowName)
        {
            if (!window.TryGetComponent<CodeWindow>(out var codeWindow)) return;
            var sync = GleamHost.Instance?.ProjectSync;
            if (sync == null || !sync.Enabled) return;
            // CodeWindow.Rename updates `fileName` AFTER RenameWindow, so the postfix
            // still sees the old name here — exactly what RenameModuleFile needs.
            sync.RenameModuleFile(codeWindow.fileName, newWindowName);
        }
    }

    /// <summary>
    /// Opening a code window in-game is an explicit re-add: clear the
    /// externally-deleted flag so a subsequent flush re-creates its `.gleam`.
    /// </summary>
    [HarmonyPatch(typeof(Workspace), nameof(Workspace.OpenCodeWindow))]
    public static class WindowOpenPatch
    {
        [HarmonyPostfix]
        public static void OnOpen(string fileName)
        {
            var sync = GleamHost.Instance?.ProjectSync;
            if (sync == null || !sync.Enabled) return;
            sync.OnWindowOpenedInGame(fileName);
        }
    }
}