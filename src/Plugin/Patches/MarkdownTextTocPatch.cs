using System.Text;
using GleamRuntime;
using HarmonyLib;

namespace GleamFarmer.Patches
{
    /// <summary>
    /// In Gleam mode the docs-window table of contents lists the <c>game.*</c> library
    /// instead of the game's Python builtins/Entities/Items/Grounds. Entries keep the
    /// game's unlock gating (same keys), and the "builtins" section gains a leading
    /// overview entry. The home page's "Programming" section (Python scripting links)
    /// is replaced with the Gleam primer + stdlib pages; its heading is renamed to
    /// "Gleam Programming" via the Localizer patch. A small "Feedback" section with
    /// GitHub links (Discussions first, Issues for bugs) follows it — the game opens
    /// external <c>https</c> links in the system browser.
    /// </summary>
    [HarmonyPatch(typeof(MarkdownText))]
    public static class MarkdownTextTocPatch
    {
        [HarmonyPatch(nameof(MarkdownText.Setup))]
        [HarmonyPrefix]
        public static void HomePrefix(ref string text)
        {
            if (!Enabled() || text == null) return;
            // Only the docs-window home/TOC page carries the builtins TOC placeholder.
            if (text.IndexOf("builtinsTOC", System.StringComparison.Ordinal) < 0) return;

            // Replace the base game's "Programming" section content (Python scripting
            // links) with the Gleam primer list + stdlib pages. The section heading is
            // renamed to "Gleam Programming" by LocalizerDocsPatch.
            const string marker = "## {{@table_of_contents_section_programming}}";
            var heading = text.IndexOf(marker, System.StringComparison.Ordinal);
            if (heading < 0) return;
            var contentStart = text.IndexOf('\n', heading);
            if (contentStart < 0) return;
            contentStart++; // past the heading's newline

            var contentEnd = text.IndexOf("\n## ", contentStart, System.StringComparison.Ordinal);
            if (contentEnd < 0) contentEnd = text.Length;

            var gleam = "\n" + GleamDocs.PrimerToc() + "\n" + GleamDocs.StdlibToc()
                + "\n\n## Feedback\n\n"
                + "[GitHub Discussions](https://github.com/metruzanca/tfwr-gleam/discussions) — feedback, ideas, questions\n\n"
                + "[GitHub Issues](https://github.com/metruzanca/tfwr-gleam/issues) — report a bug\n\n";
            text = text.Substring(0, contentStart) + gleam + text.Substring(contentEnd);
        }

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