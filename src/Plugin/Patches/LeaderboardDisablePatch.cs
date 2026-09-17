using HarmonyLib;

namespace GleamFarmer.Patches
{
    /// <summary>
    /// The game author requires all mods to disable the official leaderboards:
    /// modded executions must never be uploaded to (or read from) Steam.
    /// `SteamLeaderboard.LoadLeaderboard` is the single choke point every
    /// leaderboard interaction flows through (uploads + downloads), so no-op it.
    /// </summary>
    [HarmonyPatch(typeof(SteamLeaderboard), nameof(SteamLeaderboard.LoadLeaderboard))]
    public static class LeaderboardDisablePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref SteamLeaderboard? __result)
        {
            __result = null;
            return false; // skip the original: no Steam find/upload/download
        }
    }
}