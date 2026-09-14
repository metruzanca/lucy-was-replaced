using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

public static class Achievements
{
	private struct ItemAddition
	{
		public int itemId;

		public double number;

		public Duration time;

		public ItemAddition(int itemId, double number, Duration time)
		{
			this.itemId = itemId;
			this.number = number;
			this.time = time;
		}
	}

	private static readonly Duration COLLECTIION_TIME_THRESHOLD = Duration.FromSeconds(1.0);

	private static readonly Duration MEASURE_INTERVAL = Duration.FromSeconds(60.0);

	private const int STATS_VERSION = 3;

	private static ItemBlock best;

	private static ItemBlock sum;

	private static ItemBlock total_stats;

	private static Deque<ItemAddition> window = new Deque<ItemAddition>();

	private static HashSet<string> unlockedAchievements = new HashSet<string>();

	private static HashSet<int> unlockedItemHats = new HashSet<int>();

	private static int richPresenceItemId = -1;

	private static object lockObject = new object();

	public static volatile bool enabled = true;

	public static string RichPresenceDisplay { get; set; } = null;

	public static string RichPresenceQuantity { get; set; } = null;

	public static void LoadStats()
	{
		lock (lockObject)
		{
			window.Clear();
			best = ItemBlock.CreateEmpty();
			sum = ItemBlock.CreateEmpty();
			total_stats = ItemBlock.CreateEmpty();
			if (!SteamManager.Initialized || !best.IsEmpty())
			{
				return;
			}
			SteamUserStats.RequestCurrentStats();
			int num = default(int);
			SteamUserStats.GetStat("version", ref num);
			if (num != 3)
			{
				SteamUserStats.ResetAllStats(false);
				SteamUserStats.SetStat("version", 3);
				SteamUserStats.StoreStats();
			}
			ItemSO[] array = Resources.LoadAll<ItemSO>("Items/");
			int num2 = default(int);
			int num3 = default(int);
			foreach (ItemSO itemSO in array)
			{
				if (itemSO.trackStats)
				{
					SteamUserStats.GetStat(itemSO.itemName, ref num2);
					best.AddItem(itemSO.itemId, num2);
					UnlockGoldenHats(itemSO.itemName, num2);
					SteamUserStats.GetStat("total_" + itemSO.itemName, ref num3);
					total_stats.AddItem(itemSO.itemId, num3);
					UnlockFarmHats(itemSO.itemId);
				}
			}
			SteamFriends.SetRichPresence("steam_display", "#default_status");
		}
	}

	public static ItemBlock GetBest()
	{
		lock (lockObject)
		{
			return new ItemBlock(best);
		}
	}

	public static ItemBlock GetSum()
	{
		lock (lockObject)
		{
			return new ItemBlock(sum);
		}
	}

	public static void CollectItem(int itemId, double number, Duration currentTime, Duration collectionDuration)
	{
		if (!enabled)
		{
			return;
		}
		lock (lockObject)
		{
			double n = number;
			if (collectionDuration > MEASURE_INTERVAL)
			{
				number = ScaleNumberToNewDuration(number, collectionDuration, MEASURE_INTERVAL);
				collectionDuration = MEASURE_INTERVAL;
			}
			if (collectionDuration > COLLECTIION_TIME_THRESHOLD)
			{
				int num = (int)(collectionDuration / COLLECTIION_TIME_THRESHOLD);
				double number2 = number / (double)num;
				Duration duration = currentTime;
				for (int i = 0; i < num; i++)
				{
					ItemAddition item = new ItemAddition(itemId, number2, duration);
					duration -= COLLECTIION_TIME_THRESHOLD;
					int j;
					for (j = 0; j < window.Count && window[j].time > duration; j++)
					{
					}
					window.Insert(j, item);
				}
			}
			else
			{
				window.Enqueue(new ItemAddition(itemId, number, currentTime));
			}
			sum.AddItem(itemId, number);
			UpdateSum(currentTime);
			double number3 = sum.GetNumber(itemId);
			if (number3 > best.GetNumber(itemId))
			{
				best.AddItem(itemId, number3 - best.GetNumber(itemId));
				string itemName = StringIds.GetItemName(itemId);
				if (SteamManager.Initialized)
				{
					SteamUserStats.SetStat(itemName, (int)number3);
				}
				UnlockGoldenHats(itemName, number3);
			}
			total_stats.AddItem(itemId, n);
			if (SteamManager.Initialized)
			{
				string itemName2 = StringIds.GetItemName(itemId);
				SteamUserStats.SetStat("total_" + itemName2, (int)total_stats.GetNumber(itemId));
			}
			UnlockFarmHats(itemId);
			UpdateRichPresence(itemId);
		}
	}

	public static void UnlockGoldenHats(string itemName, double newTotal)
	{
		switch (itemName)
		{
		case "carrot":
			if (newTotal >= 200000000.0)
			{
				MainSim.Inst.UnlockHat(ResourceManager.GetHat("golden_carrot_hat"));
			}
			break;
		case "cactus":
			if (newTotal >= 20000000.0)
			{
				MainSim.Inst.UnlockHat(ResourceManager.GetHat("golden_cactus_hat"));
			}
			break;
		case "gold":
			if (newTotal >= 2000000.0)
			{
				MainSim.Inst.UnlockHat(ResourceManager.GetHat("golden_gold_hat"));
			}
			break;
		case "wood":
			if (newTotal >= 1000000000.0)
			{
				MainSim.Inst.UnlockHat(ResourceManager.GetHat("golden_tree_hat"));
			}
			break;
		case "pumpkin":
			if (newTotal >= 20000000.0)
			{
				MainSim.Inst.UnlockHat(ResourceManager.GetHat("golden_pumpkin_hat"));
			}
			break;
		case "power":
			if (newTotal >= 12000.0)
			{
				MainSim.Inst.UnlockHat(ResourceManager.GetHat("golden_sunflower_hat"));
			}
			break;
		}
	}

	public static void UnlockFarmHats(int itemId)
	{
		if (!(total_stats.GetNumber(itemId) < 1000.0) && !unlockedItemHats.Contains(itemId))
		{
			unlockedItemHats.Add(itemId);
			switch (StringIds.GetItemName(itemId))
			{
			case "carrot":
				MainSim.Inst.UnlockHat(ResourceManager.GetHat("carrot_hat"));
				break;
			case "cactus":
				MainSim.Inst.UnlockHat(ResourceManager.GetHat("cactus_hat"));
				break;
			case "gold":
				MainSim.Inst.UnlockHat(ResourceManager.GetHat("gold_hat"));
				break;
			case "wood":
				MainSim.Inst.UnlockHat(ResourceManager.GetHat("tree_hat"));
				break;
			case "pumpkin":
				MainSim.Inst.UnlockHat(ResourceManager.GetHat("pumpkin_hat"));
				break;
			case "power":
				MainSim.Inst.UnlockHat(ResourceManager.GetHat("sunflower_hat"));
				break;
			}
		}
	}

	public static void IncrementStat(string statName, int increment)
	{
		if (!enabled)
		{
			return;
		}
		lock (lockObject)
		{
			if (SteamManager.Initialized)
			{
				int num = default(int);
				SteamUserStats.GetStat(statName, ref num);
				num += increment;
				SteamUserStats.SetStat(statName, num);
			}
		}
	}

	private static double ScaleNumberToNewDuration(double number, Duration oldDuration, Duration newDuration)
	{
		return Math.Max(0.0, number * (newDuration / oldDuration));
	}

	public static void UpdateSum(Duration currentTime)
	{
		lock (lockObject)
		{
			while (window.Count > 0 && currentTime - window.Peek().time > MEASURE_INTERVAL)
			{
				ItemAddition itemAddition = window.Dequeue();
				if (!sum.RemoveItem(itemAddition.itemId, itemAddition.number))
				{
					sum.SetNumber(itemAddition.itemId, 0.0);
				}
			}
		}
	}

	public static void UnlockAchievement(string achievement)
	{
		if (!enabled)
		{
			return;
		}
		lock (lockObject)
		{
			if (!unlockedAchievements.Contains(achievement))
			{
				unlockedAchievements.Add(achievement);
				switch (achievement)
				{
				case "CAUSE_A_RUNTIME_ERROR":
					MainSim.Inst.UnlockHat(ResourceManager.GetHat("traffic_cone"));
					break;
				case "STACK_OVERFLOW":
					MainSim.Inst.UnlockHat(ResourceManager.GetHat("traffic_cone_stack"));
					break;
				case "HIGHER_ORDER_PROGRAMMING":
					MainSim.Inst.UnlockHat(ResourceManager.GetHat("wizard_hat"));
					break;
				}
				if (SteamManager.Initialized)
				{
					SteamUserStats.SetAchievement(achievement);
					SteamUserStats.StoreStats();
				}
			}
		}
	}

	public static void UpdateRichPresence(int itemId)
	{
		if (SteamManager.Initialized && (richPresenceItemId < 0 || !(sum.GetNumber(richPresenceItemId) > sum.GetNumber(itemId))))
		{
			string itemName = StringIds.GetItemName(itemId);
			RichPresenceDisplay = "#farming_" + itemName;
			RichPresenceQuantity = ((long)sum.GetNumber(itemId)).ToString();
			richPresenceItemId = itemId;
		}
	}
}
