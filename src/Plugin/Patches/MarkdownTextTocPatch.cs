using System.Text;
using GleamRuntime;
using HarmonyLib;

namespace GleamFarmer.Patches
{
    /// <summary>
    /// In Gleam mode the docs-window table of contents lists the <c>game.*</c> library
    /// instead of the game's Python builtins/Entities/Items/Grounds. Entries keep the
    /// game's unlock gating (same keys), and the "builtins" section gains a leading
    /// overview entry.
    /// </summary>
    [HarmonyPatch(typeof(MarkdownText))]
    public static class MarkdownTextTocPatch
    {
        [HarmonyPatch(nameof(MarkdownText.GenerateBuiltinsTOC))]
        [HarmonyPostfix]
        public static void Builtins(ref string __result)
        {
            if (!Enabled()) return;
            __result = Toc(GleamDocs.Builtins(), includeOverview: true);
        }

        [HarmonyPatch(nameof(MarkdownText.GenerateEntitiesTOC))]
        [HarmonyPostfix]
        public static void Entities(ref string __result)
        {
            if (!Enabled()) return;
            __result = Toc(GleamDocs.Entities(), includeOverview: false);
        }

        [HarmonyPatch(nameof(MarkdownText.GenerateGroundsTOC))]
        [HarmonyPostfix]
        public static void Grounds(ref string __result)
        {
            if (!Enabled()) return;
            __result = Toc(GleamDocs.Grounds(), includeOverview: false);
        }

        [HarmonyPatch(nameof(MarkdownText.GenerateItemsTOC))]
        [HarmonyPostfix]
        public static void Items(ref string __result)
        {
            if (!Enabled()) return;
            __result = Toc(GleamDocs.Items(), includeOverview: false);
        }

        private static bool Enabled() =>
            GleamHost.Instance != null && GleamHost.Instance.Enabled && GleamDocs.IsLoaded;

        private static string Toc(
            System.Collections.Generic.List<(string Label, string Page, string Gate)> entries,
            bool includeOverview)
        {
            var sb = new StringBuilder();
            foreach (var (label, page, gate) in entries)
            {
                var link = page == "game" ? "functions/game" : page;
                if (gate.Length == 0)
                    sb.Append($"[{label}]({link})      ");
                else
                    sb.Append($"<unlock={gate}>[{label}]({link})      </unlock>");
            }
            return sb.ToString();
        }
    }
}