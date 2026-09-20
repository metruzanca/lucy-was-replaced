using System;
using System.Collections.Generic;
using GleamRuntime;
using HarmonyLib;

namespace GleamFarmer.Patches
{
/// <summary>
        /// In Gleam mode autocomplete offers the <c>game</c> module and the Gleam stdlib
        /// modules: typing <c>game.</c> completes functions/constants, <c>game.item.</c>
        /// completes item members, and <c>int.</c> / <c>list.</c> / … complete stdlib
        /// functions. The game's Python keywords (and <c>__name__</c>) are dropped from the
        /// base word list.
        /// </summary>
        [HarmonyPatch(typeof(CodeWindow))]
        public static class CodeWindowDocsPatch
        {
            [HarmonyPatch(nameof(CodeWindow.GetWordList))]
            [HarmonyPostfix]
            public static void WordListPostfix(ref List<string> __result)
            {
                if (GleamHost.Instance == null || !GleamHost.Instance.Enabled) return;

                __result.RemoveAll(w =>
                    BuiltinFunctions.Functions.ContainsKey(w)
                    || Farm.allKeyWords.Contains(w)
                    || w == "__name__");
                if (!__result.Contains("game"))
                    __result.Add("game");
                foreach (var module in GleamStdlibDocs.CuratedModules)
                    if (!__result.Contains(module))
                        __result.Add(module);
                if (!__result.Contains("unlock"))
                    __result.Add("unlock");
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

                if (domain == "unlock" && !HasUserModule("unlock"))
                {
                    __result = GleamDocs.UnlockMembers();
                    return false;
                }

                if (Array.IndexOf(GleamStdlibDocs.CuratedModules, domain) >= 0 && !HasUserModule(domain))
                {
                    __result = GleamDocs.StdlibMembers(domain);
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