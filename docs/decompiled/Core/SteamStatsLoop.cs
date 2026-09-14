using Steamworks;
using UnityEngine;

public class SteamStatsLoop : MonoBehaviour
{
	private float statsTime;

	private float richTextTime;

	private void Update()
	{
		if (Time.time - statsTime > 1f)
		{
			statsTime = Time.time;
			if (SteamManager.Initialized)
			{
				SteamUserStats.StoreStats();
			}
		}
		if (Time.time - richTextTime > 60f && Achievements.RichPresenceDisplay != null)
		{
			richTextTime = Time.time;
			if (SteamManager.Initialized)
			{
				SteamFriends.SetRichPresence("steam_display", Achievements.RichPresenceDisplay);
				Achievements.RichPresenceDisplay = null;
				SteamFriends.SetRichPresence("quantity", Achievements.RichPresenceQuantity);
				Achievements.RichPresenceQuantity = null;
			}
		}
	}
}
