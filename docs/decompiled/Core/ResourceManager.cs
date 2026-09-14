using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResourceManager
{
	private static Dictionary<string, FarmObjectSO> farmObjects;

	private static DroneSO drone;

	private static Dictionary<string, HatSO> hats;

	private static Dictionary<string, LeaderboardSO> leaderboards;

	private static Dictionary<string, UnlockSO> unlocks;

	private static ItemSO[] items;

	public static void LoadAll()
	{
		if (farmObjects != null || drone != null || hats != null || leaderboards != null || unlocks != null || items != null)
		{
			return;
		}
		items = Enumerable.ToArray<ItemSO>((IEnumerable<ItemSO>)Enumerable.OrderBy<ItemSO, float>((IEnumerable<ItemSO>)Resources.LoadAll<ItemSO>("Items/"), (Func<ItemSO, float>)((ItemSO x) => x.priority)));
		for (int i = 0; i < items.Length; i++)
		{
			items[i].itemId = i;
		}
		StringIds.SetItemIds(Enumerable.Select<ItemSO, string>((IEnumerable<ItemSO>)items, (Func<ItemSO, string>)((ItemSO x) => x.itemName)));
		farmObjects = new Dictionary<string, FarmObjectSO>();
		FarmObjectSO[] array = Resources.LoadAll<FarmObjectSO>("FarmObjects/");
		foreach (FarmObjectSO farmObjectSO in array)
		{
			farmObjectSO.cost.Deserialize();
			farmObjectSO.dropItemId = StringIds.GetItemId(farmObjectSO.dropItem);
			farmObjects[farmObjectSO.objectName] = farmObjectSO;
		}
		drone = Resources.Load<DroneSO>("FarmObjects/drone");
		hats = new Dictionary<string, HatSO>();
		HatSO[] array2 = Resources.LoadAll<HatSO>("Hats/");
		foreach (HatSO hatSO in array2)
		{
			hats[hatSO.hatName] = hatSO;
		}
		leaderboards = new Dictionary<string, LeaderboardSO>();
		LeaderboardSO[] array3 = Resources.LoadAll<LeaderboardSO>("Leaderboards/");
		foreach (LeaderboardSO leaderboardSO in array3)
		{
			leaderboardSO.startItems.Deserialize();
			leaderboardSO.goalItems.Deserialize();
			leaderboards[leaderboardSO.leaderboardName] = leaderboardSO;
		}
		unlocks = new Dictionary<string, UnlockSO>();
		UnlockSO[] array4 = Resources.LoadAll<UnlockSO>("Unlocks/");
		foreach (UnlockSO unlockSO in array4)
		{
			foreach (ItemBlock item in unlockSO.multiUnlockCost)
			{
				item.Deserialize();
			}
			unlockSO.unlockCost.Deserialize();
			unlocks[unlockSO.unlockName] = unlockSO;
		}
	}

	public static FarmObjectSO GetFarmObject(string name)
	{
		return farmObjects.GetValueOrDefault(name);
	}

	public static DroneSO GetDrone()
	{
		return drone;
	}

	public static HatSO GetHat(string name)
	{
		return hats.GetValueOrDefault(name);
	}

	public static LeaderboardSO GetLeaderboard(string name)
	{
		return leaderboards.GetValueOrDefault(name);
	}

	public static UnlockSO GetUnlock(string name)
	{
		return unlocks.GetValueOrDefault(name);
	}

	public static ItemSO GetItem(int itemId)
	{
		return items[itemId];
	}

	public static Sprite GetSprite(string name)
	{
		return Resources.Load<Sprite>("Sprites/" + name);
	}

	public static IEnumerable<ItemSO> GetAllItems()
	{
		return items;
	}

	public static IEnumerable<FarmObjectSO> GetAllFarmObjects()
	{
		return farmObjects.Values;
	}

	public static IEnumerable<UnlockSO> GetAllUnlocks()
	{
		return unlocks.Values;
	}

	public static IEnumerable<LeaderboardSO> GetAllLeaderboards()
	{
		return leaderboards.Values;
	}

	public static IEnumerable<HatSO> GetAllHats()
	{
		return hats.Values;
	}

	public static IEnumerable<OptionSO> GetAllOptions()
	{
		return Resources.LoadAll<OptionSO>("Options/");
	}
}
