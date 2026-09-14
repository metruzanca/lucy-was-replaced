using HarmonyLib;

namespace GleamFarmer.Patches
{
    /// <summary>
    /// Intercepts the game's Run/Execute button. In Gleam mode the window's code
    /// is compiled with the embedded Gleam compiler and run on the JS runtime,
    /// instead of the game's built-in Python interpreter.
    /// </summary>
    [HarmonyPatch(typeof(CodeWindow), nameof(CodeWindow.PressExecuteOrStop))]
    public static class CodeWindowRunPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(CodeWindow __instance)
        {
            if (GleamHost.Instance == null || !GleamHost.Instance.Enabled)
                return true; // run the game's original behaviour

            GleamHost.Instance.Run(__instance);
            return false; // always skip the Python interpreter in Gleam mode
        }
    }
}