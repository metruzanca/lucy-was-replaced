using HarmonyLib;
using UnityEngine;

namespace GleamFarmer.Patches
{
    /// <summary>Boilerplate placed in every newly created code window.</summary>
    internal static class NewWindowBoilerplate
    {
        internal const string Code = "import game\n\npub fn main() {\n  game.do_a_flip()\n}\n";
    }

    /// <summary>
    /// Pre-fill the window created by the game's "new file" button with Gleam
    /// boilerplate instead of leaving it empty.
    /// </summary>
    [HarmonyPatch(typeof(Workspace), nameof(Workspace.AddNewWindow))]
    public static class NewWindowButtonPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Workspace __instance)
        {
            if (GleamHost.Instance == null || !GleamHost.Instance.Enabled)
                return true; // Python mode: normal empty window

            __instance.OpenCodeWindow(
                __instance.GenerateFileName("f"),
                NewWindowBoilerplate.Code,
                -__instance.container.anchoredPosition + __instance.spawnWindowOffset);
            if (__instance.codeWindows.Count >= 20)
                Achievements.UnlockAchievement("CHAOS");
            return false;
        }
    }

    /// <summary>
    /// A brand-new save starts with a "main" window created empty by
    /// <see cref="Saver.Load"/>; give it the same boilerplate. Existing saves are
    /// untouched (only empty windows are filled).
    /// </summary>
    [HarmonyPatch(typeof(Saver), nameof(Saver.Load))]
    public static class SaverLoadBoilerplatePatch
    {
        [HarmonyPostfix]
        public static void FillMain()
        {
            if (GleamHost.Instance == null || !GleamHost.Instance.Enabled)
                return; // Python mode

            var workspace = MainSim.Inst?.workspace;
            if (workspace?.codeWindows == null) return;
            if (!workspace.codeWindows.TryGetValue("main", out var window)) return;
            if (!string.IsNullOrWhiteSpace(window.CodeInput.text)) return;
            window.CodeInput.text = NewWindowBoilerplate.Code;
        }
    }
}