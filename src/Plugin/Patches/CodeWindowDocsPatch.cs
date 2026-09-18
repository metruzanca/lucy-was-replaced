using System.Collections.Generic;
using GleamRuntime;
using HarmonyLib;

namespace GleamFarmer.Patches
{
    /// <summary>
    /// In Gleam mode autocomplete offers the <c>game</c> module: typing <c>game.</c>
    /// completes functions/constants and <c>game.item.</c> completes item members.
    /// Bare Python builtin names are dropped from the base word list (Gleam code never
    /// calls them without the <c>game.</c> prefix).
    /// </summary>
    [HarmonyPatch(typeof(CodeWindow))]
    public static class CodeWindowDocsPatch
    {
        [HarmonyPatch(nameof(CodeWindow.GetWordList))]
        [HarmonyPostfix]
        public static void WordListPostfix(ref List<string> __result)
        {
            if (GleamHost.Instance == null || !GleamHost.Instance.Enabled) return;

            __result.RemoveAll(w => BuiltinFunctions.Functions.ContainsKey(w));
            if (!__result.Contains("game"))
                __result.Add("game");
            __result.Sort();
        }

        [HarmonyPatch(nameof(CodeWindow.GetSubWordList))]
        [HarmonyPrefix]
        public static bool SubWordListPrefix(string domain, ref List<string> __result)
        {
            if (GleamHost.Instance == null || !GleamHost.Instance.Enabled || !GleamDocs.IsLoaded)
                return true;

            if (domain == "game")
            {
                __result = GleamDocs.Members();
                return false;
            }

            if (domain == "item" && !HasUserModule("item"))
            {
                __result = GleamDocs.ItemMembers();
                return false;
            }

            return true;
        }

        private static bool HasUserModule(string name)
        {
            var workspace = MainSim.Inst?.workspace;
            return workspace?.codeWindows != null && workspace.codeWindows.ContainsKey(name);
        }
    }
}