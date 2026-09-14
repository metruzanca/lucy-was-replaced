using HarmonyLib;

namespace GleamFarmer.Patches
{
    /// <summary>
    /// In Gleam mode the editor is highlighted with Gleam syntax instead of the
    /// game's Python regex highlighter.
    /// </summary>
    [HarmonyPatch(typeof(CodeUtilities))]
    [HarmonyPatch(nameof(CodeUtilities.SyntaxColor2))]
    public static class CodeUtilitiesColorPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(
            ref string __result,
            string code,
            ColorTheme colorTheme,
            string searchWord = "",
            int searchIndex = -1)
        {
            if (GleamHost.Instance == null || !GleamHost.Instance.Enabled)
                return true;

            __result = GleamHighlighter.Color(code, colorTheme, searchWord, searchIndex);
            return false;
        }
    }
}