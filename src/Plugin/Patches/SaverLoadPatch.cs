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
            sync.EnsureScaffold();
            // Project is canonical: apply external `.gleam` edits to the windows first,
            // then write every window back so a fresh save populates the project.
            sync.SeedWindowsFromProject();
            sync.FlushWindows();
        }
    }
}