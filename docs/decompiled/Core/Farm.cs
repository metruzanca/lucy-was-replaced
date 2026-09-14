using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Farm
{
	private const double FILL_INTERVAL = 10.0;

	private const double USE_POWER_INTERVAL = 0.2;

	public GridManager grid;

	public List<Drone> drones = new List<Drone>();

	public int mainDroneId;

	public Simulation sim;

	public int droneGeneration;

	private readonly ItemBlock _items;

	public static List<string> startUnlocks = new List<string> { "grass", "soil", "harvest", "pass", "do_a_flip", "pet_the_piggy", "grassland", "hay", "straw_hat", "tap" };

	private Dictionary<string, int> unlocks = new Dictionary<string, int>();

	public static HashSet<string> allKeyWords = new HashSet<string>
	{
		"def", "while", "for", "if", "else", "elif", "and", "or", "not", "True",
		"False", "None", "Entities", "Grounds", "Items", "Unlocks", "Leaderboards", "Hats", "North", "South",
		"West", "East", "pass", "break", "continue", "return", "global", "import", "from"
	};

	public HashSet<string> unlockedKeyWords = new HashSet<string>();

	private Dictionary<string, UnlockSO> unlockedIn;

	public ItemBlock Items => _items;

	public double UsedPower { get; set; }

	public int NumSpeedUpgrades { get; private set; }

	public Farm(Simulation sim, IEnumerable<string> unlocks, ItemBlock itemBlock, List<SFO> loadedGrounds = null, List<SFO> loadedEntities = null, bool resetUnlocks = false)
	{
		this.sim = sim;
		sim.farm = this;
		drones.Add(new Drone(sim, Vector2Int.zero, 0));
		grid = new GridManager(this, loadedGrounds, loadedEntities);
		_items = itemBlock;
		foreach (string startUnlock in startUnlocks)
		{
			Unlock(startUnlock, 1);
		}
		foreach (string unlock3 in unlocks)
		{
			int num = unlock3.LastIndexOf('_');
			string text;
			if (num != -1 && int.TryParse(unlock3.Substring(num + 1), out var result) && !unlock3.StartsWith("debug"))
			{
				text = unlock3.Substring(0, num);
				if (resetUnlocks)
				{
					UnlockSO unlock = ResourceManager.GetUnlock(text);
					if (unlock != null)
					{
						result = (int)Math.Clamp(Math.Log(result + 1, 2.0), 1.0, unlock.maxUnlockLevel);
					}
				}
				Unlock(text, result);
			}
			else
			{
				text = unlock3;
				result = 1;
			}
			if (ResourceManager.GetUnlock(text) != null)
			{
				UnlockSO unlock2 = ResourceManager.GetUnlock(text);
				UnlockAllIn(unlock2);
			}
			Unlock(text, result);
		}
		sim.StartTimer(ReceiveWater, Duration.FromSeconds(20.0 / (double)(1 << NumUnlocked("watering"))));
		sim.StartTimer(ReceiveFertilizer, Duration.FromSeconds(20.0 / (double)(1 << NumUnlocked("fertilizer"))));
		sim.StartTimer(UsePower, Duration.FromSeconds(0.2));
	}

	public void CollectItems(ItemBlock items, Vector3 localPos, Duration collectionDuration = default(Duration))
	{
		if (items == null || items.IsEmpty())
		{
			return;
		}
		if (Items.GetNumber(StringIds.PowerItemId) == 0.0 && items.GetNumber(StringIds.PowerItemId) > 0.0 && sim.SpeedFactor == MaxSpeedFactor())
		{
			UsedPower = 0.0;
			Items.Add(items);
			sim.ChangeExecutionSpeed(MaxSpeedFactor());
		}
		else
		{
			Items.Add(items);
		}
		if (!(sim.mainSim != null))
		{
			return;
		}
		if (sim.leaderboardType == LeaderboardType.none)
		{
			for (int i = 0; i < items.items.Length; i++)
			{
				if (items.items[i] > 0.0)
				{
					ItemSO item = ResourceManager.GetItem(i);
					if (item != null && item.trackStats)
					{
						Achievements.CollectItem(item.itemId, items.items[i], sim.CurrentTime, collectionDuration);
					}
				}
			}
		}
		if (sim.mainSim.TimeFactor == 1.0)
		{
			sim.mainSim.CollectItemEffect(Enumerable.First<int>(items.ItemIds()), localPos);
		}
	}

	public void CollectItem(int itemId, double n, Vector3 localPos, Duration collectionDuration = default(Duration))
	{
		CollectItems(new ItemBlock(itemId, n), localPos, collectionDuration);
	}

	public ItemBlock GetUnlockCost(UnlockSO unlockSO, int numUnlocked = -1)
	{
		int num = ((numUnlocked >= 0) ? numUnlocked : NumUnlocked(unlockSO.unlockName));
		if (num >= unlockSO.maxUnlockLevel)
		{
			return ItemBlock.CreateEmpty();
		}
		if (unlockSO.IsMultiUnlock)
		{
			int count = unlockSO.multiUnlockCost.Count;
			if (num > 0)
			{
				if (count < num)
				{
					ItemBlock itemBlock = unlockSO.multiUnlockCost[count - 1] * Mathf.Pow(unlockSO.multiUnlockFactor, num - count);
					foreach (int item in itemBlock.ItemIds())
					{
						itemBlock.items[item] = Helper.RoundToNDecimalDigits(itemBlock.items[item], 3);
					}
					itemBlock.TruncateNumbers();
					return itemBlock;
				}
				return unlockSO.multiUnlockCost[num - 1];
			}
		}
		return unlockSO.unlockCost;
	}

	public List<string> SerializeUnlocks()
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, int> unlock in unlocks)
		{
			if (unlock.Value > 1)
			{
				list.Add($"{unlock.Key}_{unlock.Value}");
			}
			else
			{
				list.Add(unlock.Key);
			}
		}
		return list;
	}

	public UnlockSO GetUnlockOf(string name)
	{
		if (unlockedIn == null)
		{
			unlockedIn = new Dictionary<string, UnlockSO>();
			foreach (UnlockSO allUnlock in ResourceManager.GetAllUnlocks())
			{
				unlockedIn[allUnlock.unlockName] = allUnlock;
				foreach (string unlock in allUnlock.unlocks)
				{
					unlockedIn[unlock] = allUnlock;
				}
			}
		}
		return unlockedIn.GetValueOrDefault(name);
	}

	public bool IsUnlocked(string s)
	{
		return NumUnlocked(s.ToLower()) > 0;
	}

	public void AssertUnlocked(string dependency, int wordStart = -1, int wordEnd = -1)
	{
		if (IsUnlocked(dependency))
		{
			return;
		}
		string text = dependency.ToLower();
		UnlockSO unlockSO = null;
		foreach (UnlockSO allUnlock in ResourceManager.GetAllUnlocks())
		{
			if (allUnlock.unlocks.Contains(dependency) || allUnlock.unlocks.Contains(text) || allUnlock.unlockName == text)
			{
				unlockSO = allUnlock;
				break;
			}
		}
		if (unlockSO != null)
		{
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_missing_x_unlock", unlockSO), wordStart, wordEnd);
		}
		throw new ExecuteException(Localizer.Localize("error_missing_unlock"), wordStart, wordEnd);
	}

	public bool UnlockOrUpgrade(UnlockSO unlockSO, bool requireParent = true)
	{
		if ((requireParent && !string.IsNullOrEmpty(unlockSO.parentUnlock) && !IsUnlocked(unlockSO.parentUnlock)) || NumUnlocked(unlockSO) >= unlockSO.maxUnlockLevel || (sim.singleDrone && (unlockSO.unlockName == "megafarm" || unlockSO.unlockName == "expand")))
		{
			return false;
		}
		ItemBlock unlockCost = GetUnlockCost(unlockSO);
		if (unlockCost == null)
		{
			return false;
		}
		if (!Items.Remove(unlockCost))
		{
			return false;
		}
		Unlock(unlockSO.unlockName, NumUnlocked(unlockSO) + 1);
		UnlockAllIn(unlockSO);
		return true;
	}

	public void Unlock(string s, int level)
	{
		string text = s.ToLower();
		int num = NumUnlocked(text);
		if (num == level)
		{
			return;
		}
		unlocks[text] = level;
		if (allKeyWords.Contains(text))
		{
			unlockedKeyWords.Add(text);
		}
		if (allKeyWords.Contains(CodeUtilities.ToUpperSnake(text)))
		{
			unlockedKeyWords.Add(CodeUtilities.ToUpperSnake(text));
		}
		if (text == "expand")
		{
			grid.GenerateWorld(num > level);
			if (sim.leaderboardType == LeaderboardType.none)
			{
				Achievements.UnlockAchievement("EXPAND");
				if (level >= 9)
				{
					Achievements.UnlockAchievement("BIG_FARM");
				}
			}
		}
		else if (text == "speed")
		{
			double num2 = MaxSpeedFactor();
			NumSpeedUpgrades = level;
			if (sim.SpeedFactor == num2)
			{
				sim.ChangeExecutionSpeed(MaxSpeedFactor());
			}
		}
	}

	public void UnlockAllIn(UnlockSO unlockSO)
	{
		foreach (string unlock in unlockSO.unlocks)
		{
			if (char.IsDigit(unlock[0]))
			{
				if (int.Parse(unlock[0].ToString()) <= NumUnlocked(unlockSO.unlockName))
				{
					Unlock(unlock.Substring(1), 1);
				}
			}
			else
			{
				Unlock(unlock, 1);
			}
		}
	}

	public void UnlockAll(IEnumerable<string> unlocks)
	{
		foreach (string unlock in unlocks)
		{
			Unlock(unlock, 1);
		}
	}

	public bool UnlockHat(HatSO hat)
	{
		if (IsUnlocked(hat.hatName))
		{
			return false;
		}
		Unlock(hat.hatName, 1);
		return true;
	}

	public Dictionary<string, int> GetUnlocks()
	{
		return new Dictionary<string, int>(unlocks);
	}

	public int NumUnlocked(UnlockSO unlockSO)
	{
		return unlocks.GetValueOrDefault(unlockSO.unlockName, 0);
	}

	public int NumUnlocked(string unlock)
	{
		return unlocks.GetValueOrDefault(unlock, 0);
	}

	public void ResetUnlocksToMax()
	{
		foreach (UnlockSO allUnlock in ResourceManager.GetAllUnlocks())
		{
			if (allUnlock.IsMultiUnlock && NumUnlocked(allUnlock) > allUnlock.maxUnlockLevel)
			{
				Unlock(allUnlock.unlockName, allUnlock.maxUnlockLevel);
			}
		}
	}

	public double MaxSpeedFactor()
	{
		double num = Math.Pow(1.5, NumSpeedUpgrades);
		if (Items.GetNumber(StringIds.PowerItemId) > 0.0)
		{
			num *= 2.0;
		}
		return num;
	}

	private void UsePower()
	{
		if (sim.farm.Items.GetNumber(StringIds.PowerItemId) > 0.0 && !sim.farm.Items.Remove(StringIds.PowerItemId, UsedPower))
		{
			sim.farm.Items.SetNumber(StringIds.PowerItemId, 0.0);
			if (sim.SpeedFactor > sim.farm.MaxSpeedFactor())
			{
				sim.ChangeExecutionSpeed(sim.farm.MaxSpeedFactor());
			}
		}
		UsedPower = 0.0;
		sim.StartTimer(UsePower, Duration.FromSeconds(0.2));
	}

	private void ReceiveWater()
	{
		if (NumUnlocked("watering") > 0)
		{
			Items.AddItem(StringIds.WaterItemId, sim.mainSim.harvestFactor);
		}
		sim.StartTimer(ReceiveWater, Duration.FromSeconds(20.0 / (double)(1 << NumUnlocked("watering"))));
	}

	private void ReceiveFertilizer()
	{
		if (NumUnlocked("fertilizer") > 0)
		{
			Items.AddItem(StringIds.FertilizerItemId, sim.mainSim.harvestFactor);
		}
		sim.StartTimer(ReceiveFertilizer, Duration.FromSeconds(20.0 / (double)(1 << NumUnlocked("fertilizer"))));
	}

	public int AddDrone(int droneId)
	{
		droneGeneration++;
		for (int i = 0; i < drones.Count; i++)
		{
			if (drones[i] == null)
			{
				drones[i] = new Drone(sim, drones[droneId].pos, i);
				return i;
			}
		}
		if (drones.Count >= Helper.NumDrones(NumUnlocked("megafarm")))
		{
			throw new ExecuteException(Localizer.Localize("error_max_drones_reached"));
		}
		Drone item = new Drone(sim, drones[droneId].pos, drones.Count);
		drones.Add(item);
		return drones.Count - 1;
	}

	public void RemoveSpawnedDrones()
	{
		for (int num = drones.Count - 1; num >= 0; num--)
		{
			if (num != mainDroneId)
			{
				if (drones[num] != null)
				{
					drones[num].hat.OnUnequip();
				}
				drones.RemoveAt(num);
			}
		}
		mainDroneId = 0;
		drones[0].DroneId = 0;
	}

	public void RemoveDrone(int droneId)
	{
		if (droneId == mainDroneId)
		{
			for (int i = 0; i < drones.Count; i++)
			{
				if (drones[i] != null && i != mainDroneId)
				{
					mainDroneId = i;
					break;
				}
			}
		}
		drones[droneId].hat.OnUnequip();
		drones[droneId] = null;
	}
}
