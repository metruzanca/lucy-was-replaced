using System;
using TMPro;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
	private const double MAX_SPEEDUP = 256.0;

	[SerializeField]
	private GameObject overlay;

	[SerializeField]
	private TextMeshProUGUI timeText;

	[SerializeField]
	private GameObject leftButton;

	[SerializeField]
	private GameObject rightButton;

	[SerializeField]
	private GameObject leaderboardScreen;

	[SerializeField]
	private Leaderboard leaderboard;

	[SerializeField]
	private TextMeshProUGUI finishTimeText;

	[SerializeField]
	private GameObject runCancelled;

	[SerializeField]
	private TextMeshProUGUI leaderboardTitle;

	[SerializeField]
	private TextMeshProUGUI averageText;

	public bool IsRunning => overlay.activeInHierarchy;

	public bool IsLeaderBoardScreenOpen => leaderboardScreen.activeInHierarchy;

	private void Update()
	{
		if (IsRunning)
		{
			((TMP_Text)timeText).text = StringFromTimeSpan(MainSim.Inst.GetCurrentTime().ToTimeSpan());
			if (MainSim.Inst.numLeaderboardRuns > 0)
			{
				int numLeaderboardRuns = MainSim.Inst.numLeaderboardRuns;
				TimeSpan timeSpan = MainSim.Inst.totalLeaderboardTime.ToTimeSpan();
				double num = timeSpan / TimeSpan.FromHours(2.0) * 100.0;
				((TMP_Text)averageText).text = string.Format(Localizer.Localize("average_text"), numLeaderboardRuns, StringFromTimeSpan(timeSpan / numLeaderboardRuns), num);
			}
			else
			{
				((TMP_Text)averageText).text = string.Format(Localizer.Localize("average_text2"), 0, "");
			}
		}
	}

	public void StartLeaderboardRun(bool showAverageText)
	{
		overlay.SetActive(value: true);
		((Component)(object)averageText).gameObject.SetActive(showAverageText);
	}

	public void RemoveOverlay()
	{
		overlay.SetActive(value: false);
	}

	public void StopLeaderboardRun(bool finished, string leaderboardName, string steamLeaderboardName, TimeSpan timeSpan)
	{
		overlay.SetActive(value: false);
		MainSim.Inst.workspace.gameObject.SetActive(value: false);
		leaderboardScreen.SetActive(value: true);
		((TMP_Text)leaderboardTitle).text = CodeUtilities.ToUpperSnake(leaderboardName);
		((TMP_Text)finishTimeText).text = StringFromTimeSpan(timeSpan);
		int score = (int)timeSpan.TotalMilliseconds;
		if (!finished || timeSpan.TotalMilliseconds > 2147483647.0)
		{
			runCancelled.SetActive(value: true);
			score = 0;
		}
		else
		{
			runCancelled.SetActive(value: false);
		}
		leaderboard.FillLeaderboard(steamLeaderboardName, score);
		if (finished)
		{
			Achievements.UnlockAchievement("COMPETITIVE_FARMING");
			if (leaderboardName == "fastest_reset")
			{
				Achievements.UnlockAchievement("FULL_AUTOMATION");
			}
		}
	}

	public void SpeedUp()
	{
		if (MainSim.Inst.TimeFactor <= 128.0)
		{
			MainSim.Inst.TimeFactor *= 2.0;
		}
	}

	public void SlowDown()
	{
		if (MainSim.Inst.TimeFactor >= 2.0)
		{
			MainSim.Inst.TimeFactor /= 2.0;
		}
		rightButton.SetActive(MainSim.Inst.TimeFactor <= 128.0);
		leftButton.SetActive(MainSim.Inst.TimeFactor >= 2.0);
	}

	public void OkPressed()
	{
		MainSim.Inst.workspace.gameObject.SetActive(value: true);
		leaderboardScreen.SetActive(value: false);
		MainSim.Inst.RestoreMainSim();
	}

	public static string StringFromTimeSpan(TimeSpan t)
	{
		return string.Format("{0}{1}", ((int)t.TotalHours > 0) ? ((int)t.TotalHours + ":") : "", t.ToString("mm\\:ss\\.fff"));
	}
}
