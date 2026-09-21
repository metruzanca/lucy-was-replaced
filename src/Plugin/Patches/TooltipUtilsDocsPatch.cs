using System;
using System.Collections.Generic;
using GleamRuntime;
using HarmonyLib;
using UnityEngine;

namespace GleamFarmer.Patches
{
    /// <summary>
    /// In Gleam mode hovering a <c>game.*</c> reference shows the bundled Gleam doc, and
    /// the docs-window <c>objects/*</c> / <c>items/*</c> / <c>unlocks/*</c> pages render the
    /// Gleam reference instead of the Python-flavoured tooltips.
    /// </summary>
    [HarmonyPatch(typeof(TooltipUtils))]
    public static class TooltipUtilsDocsPatch
    {
        /// <summary>Game unlock names whose Gleam docs slug differs from the lowercase name.</summary>
        private static readonly Dictionary<string, string> UnlockSlug = new(StringComparer.Ordinal)
        {
            ["auto_unlock"] = "autounlock",
            ["debug_2"] = "debug2",
            ["the_farmers_remains"] = "thefarmersremains",
            ["top_hat"] = "tophat",
        };

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

            // Restore the game's dynamic "Plant Cost" block that the Python tooltip carried.
            var cost = "";
            var farmObject = ResourceManager.GetFarmObject(objectName);
            if (farmObject?.cost != null && !farmObject.cost.IsEmpty())
                cost = "\n\n" + string.Format(Localizer.Localize("object_tooltip_template_cost"), farmObject.objectName);

            __result = NewTip(doc + cost, "game." + TitleCase(objectName), "objects/" + objectName.ToLowerInvariant());
        }

        [HarmonyPatch(nameof(TooltipUtils.UnlockTooltip))]
        [HarmonyPostfix]
        public static void UnlockPostfix(string unlockName, ref TooltipInfo __result)
        {
            if (!Enabled() || __result == null) return;

            var page = UnlockPage(unlockName);
            var body = GleamDocs.Page(page);
            if (string.IsNullOrEmpty(body)) return;

            // Keep the game's dynamic "Unlock Cost" block that the Python tooltip carried.
            var cost = "";
            var unlock = ResourceManager.GetUnlock(unlockName);
            if (unlock != null && MainSim.Inst != null)
            {
                var unlockCost = MainSim.Inst.GetUnlockCost(unlock);
                if (unlockCost != null && !unlockCost.IsEmpty())
                    cost = "\n\n" + string.Format(Localizer.Localize("unlock_tooltip_template_cost"), unlockName);
            }

            var label = GleamDocs.LabelFor(page);
            if (label == "") label = "game.unlock." + TitleCase(page.Substring("unlocks/".Length));
            __result = new TooltipInfo(
                $"## `{label}`\n{CodeUtilities.ApplyCodeTags(body)}{cost}",
                0f, default(Vector3), TooltipInfo.Anchor.Auto, unlock?.docs ?? page);
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

        private static string UnlockPage(string unlockName) =>
            "unlocks/" + (UnlockSlug.TryGetValue(unlockName, out var slug) ? slug : unlockName);
    }
}