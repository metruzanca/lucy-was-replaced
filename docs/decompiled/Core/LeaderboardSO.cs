using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Leaderboard", menuName = "ScriptableObjects/Leaderboard", order = 1)]
public class LeaderboardSO : ScriptableObject, IPyObject
{
	public string leaderboardName;

	public string steamLeaderboardName;

	public LeaderboardType leaderboardType;

	public ItemBlock startItems;

	public bool everythingUnlocked;

	public bool singleDrone;

	public ItemBlock goalItems;

	public override string ToString()
	{
		return "Leaderboards." + CodeUtilities.ToUpperSnake(leaderboardName);
	}

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		return this;
	}
}
