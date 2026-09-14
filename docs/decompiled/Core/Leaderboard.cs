using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Leaderboard : MonoBehaviour
{
	[SerializeField]
	private LeaderboardEntry leaderboardEntryPrefab;

	[SerializeField]
	private Transform container;

	[SerializeField]
	private TextMeshProUGUI highScoreText;

	[SerializeField]
	private TextMeshProUGUI rankText;

	private List<LeaderboardEntry> leaderboardEntries = new List<LeaderboardEntry>();

	public void FillLeaderboard(string leaderboardName, int score = 0)
	{
		SteamLeaderboard.LoadLeaderboard(leaderboardName, score, OnPlayerLoaded, OnEntriesLoaded);
		((TMP_Text)highScoreText).text = "-";
		((TMP_Text)rankText).text = "-";
		container.gameObject.SetActive(value: false);
	}

	private void OnPlayerLoaded(LeaderboardEntryData[] data)
	{
		if (data.Length != 0)
		{
			if ((UnityEngine.Object)(object)highScoreText != null)
			{
				((TMP_Text)highScoreText).text = LeaderboardManager.StringFromTimeSpan(TimeSpan.FromMilliseconds(data[0].score));
			}
			if ((UnityEngine.Object)(object)rankText != null)
			{
				((TMP_Text)rankText).text = "#" + data[0].rank;
			}
			if (data[0].rank == 1)
			{
				MainSim.Inst.UnlockHat(ResourceManager.GetHat("gold_trophy_hat"));
			}
			else if (data[0].rank == 2)
			{
				MainSim.Inst.UnlockHat(ResourceManager.GetHat("silver_trophy_hat"));
			}
			else if (data[0].rank == 3)
			{
				MainSim.Inst.UnlockHat(ResourceManager.GetHat("wood_trophy_hat"));
			}
		}
	}

	private void OnEntriesLoaded(LeaderboardEntryData[] data)
	{
		container.gameObject.SetActive(value: true);
		for (int i = 0; i < data.Length; i++)
		{
			if (leaderboardEntries.Count - 1 < i)
			{
				leaderboardEntries.Add(UnityEngine.Object.Instantiate(leaderboardEntryPrefab, container));
			}
			leaderboardEntries[i].Setup(data[i].rank, data[i].playerName, LeaderboardManager.StringFromTimeSpan(TimeSpan.FromMilliseconds(data[i].score)));
		}
		while (leaderboardEntries.Count > data.Length)
		{
			UnityEngine.Object.Destroy(leaderboardEntries[leaderboardEntries.Count - 1].gameObject);
			leaderboardEntries.RemoveAt(leaderboardEntries.Count - 1);
		}
	}
}
