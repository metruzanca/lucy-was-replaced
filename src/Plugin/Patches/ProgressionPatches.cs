using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace GleamFarmer.Patches
{
    /// <summary>
    /// Re-scopes progression for Gleam: language/tooling unlocks are auto-granted
    /// (Gleam syntax is never gated) and hidden from the research tree, leaving the
    /// world/crop economy as the progression.
    /// </summary>
    public static class ProgressionPatches
    {
        // Python-only SYNTAX keywords. Used to detect language unlocks data-drivenly.
        // (Farm.allKeyWords also contains constants like North/True/Entities, which
        // world unlocks legitimately introduce - matching on those would hide crops.)
        private static readonly HashSet<string> SyntaxKeywords = new()
        {
            "def", "while", "for", "if", "else", "elif", "and", "or", "not",
            "break", "continue", "pass", "return", "global", "import", "from",
        };

        private static readonly HashSet<string> LanguageOrToolingNames = new()
        {
            // Language / syntax unlocks (Python-oriented; Gleam equivalents always work).
            "loops", "functions", "operators", "variables", "lists", "dictionaries",
            "import", "if", "else", "elif", "for", "range",
            // Tooling unlocks: soft gates for sensors/helpers we expose via `game`.
            "senses", "utilities", "costs", "timing", "debug", "debug_2",
            "simulation", "auto_unlock", "can_harvest", "get_water", "measure",
        };

        private static bool _loggedTree = false;

        internal static bool IsGleamMode => GleamHost.Instance?.Enabled == true;

        /// <summary>
        /// Language/tooling UnlockSOs: curated names plus any unlock that directly
        /// gates a Python syntax keyword.
        /// </summary>
        internal static List<UnlockSO> GetLanguageUnlocks()
        {
            var result = new List<UnlockSO>();
            foreach (var so in ResourceManager.GetAllUnlocks())
            {
                var name = so.unlockName?.ToLowerInvariant() ?? string.Empty;
                var gatesKeyword = so.unlocks != null && so.unlocks.Any(SyntaxKeywords.Contains);
                if (LanguageOrToolingNames.Contains(name) || gatesKeyword)
                    result.Add(so);
            }
            return result;
        }

        internal static void LogTreeOnce()
        {
            if (_loggedTree) return;
            _loggedTree = true;

            var language = GetLanguageUnlocks();
            var names = new HashSet<string>(language.Select(s => s.unlockName));
            var parentsOfVisible = language
                .Where(so => ResourceManager.GetAllUnlocks().Any(w => !names.Contains(w.unlockName) && w.parentUnlock == so.unlockName))
                .Select(so => so.unlockName)
                .ToArray();

            GleamHost.LogSource.LogInfo(
                "[GleamFarmer] progression: hiding " + string.Join(", ", language.Select(s => s.unlockName)) +
                (parentsOfVisible.Length > 0
                    ? " | WARNING: hidden unlocks still referenced as parent by visible nodes: " + string.Join(", ", parentsOfVisible)
                    : " | no visible node references a hidden unlock as its parent"));
        }
    }

    /// <summary>Auto-grant language/tooling unlocks whenever a sim is set up (Gleam mode).</summary>
    [HarmonyPatch(typeof(MainSim), nameof(MainSim.SetupSim))]
    public static class AutoGrantUnlocksPatch
    {
        [HarmonyPostfix]
        public static void Postfix(MainSim __instance)
        {
            if (!ProgressionPatches.IsGleamMode) return;
            if (__instance.sim?.farm == null) return;

            lock (__instance.lockSimulation)
            {
                foreach (var so in ProgressionPatches.GetLanguageUnlocks())
                {
                    __instance.sim.farm.UnlockAllIn(so);
                    __instance.sim.farm.Unlock(so.unlockName, 1);
                }
            }
        }
    }

    /// <summary>
    /// Hide language/tooling unlocks from the research tree in Gleam mode.
    /// The `enabled` flag is re-derived on every Setup, so it self-heals when
    /// Gleam mode is toggled off.
    /// </summary>
    [HarmonyPatch(typeof(ResearchMenu), nameof(ResearchMenu.Setup))]
    public static class ResearchTreePatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            var gleam = ProgressionPatches.IsGleamMode;
            foreach (var so in ProgressionPatches.GetLanguageUnlocks())
                so.enabled = !gleam;

            if (gleam) ProgressionPatches.LogTreeOnce();
        }
    }
}