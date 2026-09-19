using GleamRuntime;
using HarmonyLib;

namespace GleamFarmer.Patches
{
    /// <summary>
    /// In Gleam mode the function docs ("code_tooltip_*" — the in-game docs-window pages
    /// and hover tooltips) come from the bundled Gleam reference instead of the game's
    /// Python descriptions. Python-only builtins (range, len, min, …) keep the game's
    /// text since they have no <c>game.*</c> counterpart.
    /// </summary>
    [HarmonyPatch(typeof(Localizer), nameof(Localizer.Localize))]
    public static class LocalizerDocsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref string __result, string key)
        {
            if (GleamHost.Instance == null || !GleamHost.Instance.Enabled || !GleamDocs.IsLoaded)
                return;

            if (key.StartsWith("code_tooltip_", System.StringComparison.Ordinal))
            {
                var name = key.Substring("code_tooltip_".Length);
                var doc = name == "game" ? GleamDocs.Overview : GleamDocs.FunctionDoc(name);
                if (!string.IsNullOrEmpty(doc))
                {
                    __result = CodeUtilities.ApplyCodeTags(doc);
                    return;
                }
            }
            else if (key == "table_of_contents_section_builtins")
            {
                __result = "Game API";
            }
            else if (key == "table_of_contents_section_programming")
            {
                __result = "Gleam Programming";
            }
        }
    }
}