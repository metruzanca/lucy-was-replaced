using HarmonyLib;

namespace GleamFarmer.Patches
{
    /// <summary>
    /// Intercepts the game's step-by-step button. In Gleam mode the button toggles the
    /// runtime's step-through gate: enter step mode (or start a run in step mode) when not
    /// stepping, advance one line when already stepping.
    /// </summary>
    [HarmonyPatch(typeof(CodeWindow), nameof(CodeWindow.PressStepByStepButton))]
    public static class CodeWindowStepPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(CodeWindow __instance)
        {
            if (GleamHost.Instance == null || !GleamHost.Instance.Enabled)
                return true; // run the game's original behaviour

            GleamHost.Instance.OnStepPressed(__instance);
            return false; // always handle step mode ourselves in Gleam mode
        }
    }
}