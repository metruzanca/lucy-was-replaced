using HarmonyLib;

namespace GleamFarmer.Patches
{
    /// <summary>
    /// In Gleam mode the editor holds Gleam source, which the game's Python parser
    /// cannot read. The game calls <c>CodeWindow.Parse()</c> in several places
    /// (on commit, undo, window focus), which would set a bogus parse error like
    /// "invalid file import" for Gleam `import` statements. Skip it entirely.
    /// </summary>
    [HarmonyPatch(typeof(CodeWindow), nameof(CodeWindow.Parse))]
    public static class CodeWindowParsePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(CodeWindow __instance, ref Node __result)
        {
            if (GleamHost.Instance == null || !GleamHost.Instance.Enabled)
                return true; // normal Python mode

            var fields = Traverse.Create(__instance);
            fields.Field("parseException").SetValue(null);
            fields.Field("errorString").SetValue(null);
            fields.Field("cachedProgram").SetValue(null);
            __result = null!;

            // Dismiss any stale Python error panel.
            __instance.CloseError();
            return false;
        }
    }
}