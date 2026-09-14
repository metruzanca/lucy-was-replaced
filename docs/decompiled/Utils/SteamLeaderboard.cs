using System;
using Steamworks;

public class SteamLeaderboard
{
	private string leaderboardName;

	private int scoreToSet;

	private SteamLeaderboard_t currentLeaderboard;

	private CallResult<LeaderboardFindResult_t> findResult = new CallResult<LeaderboardFindResult_t>((APIDispatchDelegate<LeaderboardFindResult_t>)null);

	private CallResult<LeaderboardScoreUploaded_t> uploadResult = new CallResult<LeaderboardScoreUploaded_t>((APIDispatchDelegate<LeaderboardScoreUploaded_t>)null);

	private CallResult<LeaderboardScoresDownloaded_t> downloadResultPlayer = new CallResult<LeaderboardScoresDownloaded_t>((APIDispatchDelegate<LeaderboardScoresDownloaded_t>)null);

	private CallResult<LeaderboardScoresDownloaded_t> downloadResultTop = new CallResult<LeaderboardScoresDownloaded_t>((APIDispatchDelegate<LeaderboardScoresDownloaded_t>)null);

	private Action<LeaderboardEntryData[]> loadPlayerCallback;

	private Action<LeaderboardEntryData[]> loadTopCallback;

	private SteamLeaderboard(string leaderboardName, int scoreToSet, Action<LeaderboardEntryData[]> loadPlayerCallback, Action<LeaderboardEntryData[]> loadTopCallback)
	{
		this.leaderboardName = leaderboardName;
		this.loadPlayerCallback = loadPlayerCallback;
		this.loadTopCallback = loadTopCallback;
		this.scoreToSet = scoreToSet;
	}

	public static SteamLeaderboard LoadLeaderboard(string leaderboardName, int score, Action<LeaderboardEntryData[]> loadPlayerCallback, Action<LeaderboardEntryData[]> loadTopCallback)
	{
		SteamLeaderboard steamLeaderboard = new SteamLeaderboard(leaderboardName, score, loadPlayerCallback, loadTopCallback);
		steamLeaderboard.Load();
		return steamLeaderboard;
	}

	public void Load()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (SteamManager.Initialized)
		{
			SteamAPICall_t val = SteamUserStats.FindLeaderboard(leaderboardName);
			findResult.Set(val, (APIDispatchDelegate<LeaderboardFindResult_t>)OnLeaderboardFindResult);
		}
	}

	private void OnLeaderboardFindResult(LeaderboardFindResult_t callback, bool failure)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		if (!failure)
		{
			currentLeaderboard = callback.m_hSteamLeaderboard;
			if (scoreToSet != 0)
			{
				SteamAPICall_t val = SteamUserStats.UploadLeaderboardScore(currentLeaderboard, (ELeaderboardUploadScoreMethod)1, scoreToSet, (int[])null, 0);
				uploadResult.Set(val, (APIDispatchDelegate<LeaderboardScoreUploaded_t>)OnLeaderboardUploadResult);
			}
			else
			{
				SteamAPICall_t val2 = SteamUserStats.UploadLeaderboardScore(currentLeaderboard, (ELeaderboardUploadScoreMethod)0, 0, (int[])null, 0);
				uploadResult.Set(val2, (APIDispatchDelegate<LeaderboardScoreUploaded_t>)OnLeaderboardUploadResult);
			}
			SteamAPICall_t val3 = SteamUserStats.DownloadLeaderboardEntries(currentLeaderboard, (ELeaderboardDataRequest)0, 0, 300);
			downloadResultTop.Set(val3, (APIDispatchDelegate<LeaderboardScoresDownloaded_t>)OnTopEntriesLoaded);
		}
	}

	private void OnLeaderboardUploadResult(LeaderboardScoreUploaded_t callback, bool failure)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (!failure)
		{
			SteamAPICall_t val = SteamUserStats.DownloadLeaderboardEntries(currentLeaderboard, (ELeaderboardDataRequest)1, 0, 0);
			downloadResultPlayer.Set(val, (APIDispatchDelegate<LeaderboardScoresDownloaded_t>)OnPlayerEntryLoaded);
		}
	}

	private void OnPlayerEntryLoaded(LeaderboardScoresDownloaded_t callback, bool failure)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		if (!failure)
		{
			loadPlayerCallback(GetEntryData(callback));
		}
	}

	private void OnTopEntriesLoaded(LeaderboardScoresDownloaded_t callback, bool failure)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		if (!failure)
		{
			loadTopCallback(GetEntryData(callback));
		}
	}

	private LeaderboardEntryData[] GetEntryData(LeaderboardScoresDownloaded_t callback)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		LeaderboardEntryData[] array = new LeaderboardEntryData[callback.m_cEntryCount];
		LeaderboardEntry_t val = default(LeaderboardEntry_t);
		for (int i = 0; i < callback.m_cEntryCount; i++)
		{
			SteamUserStats.GetDownloadedLeaderboardEntry(callback.m_hSteamLeaderboardEntries, i, ref val, (int[])null, 0);
			array[i].playerName = SteamFriends.GetFriendPersonaName(val.m_steamIDUser);
			array[i].score = val.m_nScore;
			array[i].rank = val.m_nGlobalRank;
		}
		return array;
	}
}
