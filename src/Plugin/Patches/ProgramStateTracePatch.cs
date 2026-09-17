using HarmonyLib;

namespace GleamFarmer.Patches
{
    /// <summary>
    /// `ProgramState.GetTrace()` dereferences `currentExecutingNode.boxedParams.codeWindow`
    /// to render the offending line. Our bridge calls game verbs (plant/swap/…) with a bare
    /// `ProgramState` whose `currentExecutingNode` is null; when a verb hits a *warning* path
    /// (e.g. "can't plant on this ground", "missing seed"), the game's Logger.LogWarning →
    /// GetTrace() would throw a NullReferenceException. Return a plain trace instead.
    /// The game's own states always have a currentExecutingNode while executing, so this only
    /// affects our bridged calls.
    /// </summary>
    [HarmonyPatch(typeof(ProgramState), nameof(ProgramState.GetTrace))]
    public static class ProgramStateTracePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ProgramState __instance, ref string __result)
        {
            if (__instance.CurrentExecutingNode == null)
            {
                __result = "(gleam)";
                return false; // skip the original, which would null-ref on the source window
            }
            return true;
        }
    }
}