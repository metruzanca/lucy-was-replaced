using HarmonyLib;

namespace GleamFarmer
{
    /// <summary>
    /// After the game opens the code windows from `.py` files, seed them from the
    /// on-disk Gleam project so externally-edited `.gleam` files (the project is
    /// canonical) take effect in-game.
    /// </summary>
    [HarmonyPatch(typeof(Saver), nameof(Saver.Load))]
    public static class SaverLoadPatch
    {
        [HarmonyPostfix]
        public static void OnLoad()
        {
            var sync = GleamHost.Instance?.ProjectSync;
            if (sync == null || !sync.Enabled) return;
            var createdFresh = sync.EnsureScaffold();
            // Project is canonical. On a fresh project (first load after enabling the
            // feature) populate it from the game's windows; on later loads apply the
            // project back to the windows (opening/closing/renaming as needed). No
            // unconditional flush — re-writing every window would resurrect `.gleam`
            // files the player deleted externally.
            if (createdFresh)
                sync.FlushWindows();
            else
                sync.SeedWindowsFromProject();
        }
    }
}