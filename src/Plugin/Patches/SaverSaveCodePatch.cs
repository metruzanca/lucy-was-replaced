using HarmonyLib;

namespace GleamFarmer
{
    /// <summary>
    /// Keep the on-disk Gleam project in sync with the game's code windows: when
    /// the game saves the code windows (Saver.SaveCode → WriteCodeFiles), flush
    /// them into the `gleam-project/src/*.gleam` files so an external editor/LSP
    /// sees the latest in-game edits.
    /// </summary>
    [HarmonyPatch(typeof(Saver), nameof(Saver.SaveCode))]
    public static class SaverSaveCodePatch
    {
        [HarmonyPostfix]
        public static void OnSaveCode()
        {
            var sync = GleamHost.Instance?.ProjectSync;
            if (sync == null || !sync.Enabled) return;
            sync.EnsureScaffold();
            sync.FlushWindows();
        }
    }
}