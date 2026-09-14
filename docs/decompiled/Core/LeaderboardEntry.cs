using TMPro;
using UnityEngine;

public class LeaderboardEntry : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI rankText;

	[SerializeField]
	private TextMeshProUGUI playerNameText;

	[SerializeField]
	private TextMeshProUGUI timeText;

	public void Setup(int rank, string playerName, string timeText)
	{
		((TMP_Text)rankText).text = "#" + rank;
		((TMP_Text)playerNameText).text = playerName;
		((TMP_Text)this.timeText).text = timeText;
	}
}
