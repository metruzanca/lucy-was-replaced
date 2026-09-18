using GleamRuntime;
using HarmonyLib;
using UnityEngine;

namespace GleamFarmer.Patches
{
    /// <summary>
    /// In Gleam mode hovering a <c>game.*</c> reference shows the bundled Gleam doc, and
    /// the docs-window <c>objects/*</c> / <c>items/*</c> pages render the Gleam reference
    /// instead of the Python-flavoured tooltips.
    /// </summary>
    [HarmonyPatch(typeof(TooltipUtils))]
    public static class TooltipUtilsDocsPatch
    {
        [HarmonyPatch(nameof(TooltipUtils.GetWordTooltip))]
        [HarmonyPrefix]
        public static bool WordPrefix(string word, ref TooltipInfo __result)
        {
            if (!Enabled()) return true;

            var hit = GleamDocs.LookupDotted(word);
            if (hit == null) return true;

            var (page, label) = hit.Value;
            var body = GleamDocs.Page(page);
            if (string.IsNullOrEmpty(body)) return true;

            // The docs window has no directions/* handler; link to the module overview.
            var docs = page.StartsWith("directions/", System.StringComparison.Ordinal)
                ? "functions/game"
                : page;
            __result = new TooltipInfo(
                $"## `{label}`\n{CodeUtilities.ApplyCodeTags(body)}",
                0f, default(Vector3), TooltipInfo.Anchor.Auto, docs);
            return false;
        }

        [HarmonyPatch(nameof(TooltipUtils.FarmObjectTooltip))]
        [HarmonyPostfix]
        public static void ObjectPostfix(string objectName, ref TooltipInfo __result)
        {
            if (!Enabled() || __result == null) return;
            var doc = GleamDocs.ObjectDoc(objectName);
            if (string.IsNullOrEmpty(doc)) return;
            __result = NewTip(doc, "game." + TitleCase(objectName), "objects/" + objectName.ToLowerInvariant());
        }

        [HarmonyPatch(nameof(TooltipUtils.ItemTooltip))]
        [HarmonyPostfix]
        public static void ItemPostfix(string itemName, ref TooltipInfo __result)
        {
            if (!Enabled() || __result == null) return;
            var doc = GleamDocs.ItemDoc(itemName);
            if (string.IsNullOrEmpty(doc)) return;
            __result = NewTip(doc, "game.item." + TitleCase(itemName), "items/" + itemName.ToLowerInvariant());
        }

        private static bool Enabled() =>
            GleamHost.Instance != null && GleamHost.Instance.Enabled && GleamDocs.IsLoaded;

        private static TooltipInfo NewTip(string body, string label, string docs) =>
            new($"## `{label}`\n{CodeUtilities.ApplyCodeTags(body)}",
                0f, default(Vector3), TooltipInfo.Anchor.Auto, docs);

        private static string TitleCase(string name) =>
            name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);
    }
}