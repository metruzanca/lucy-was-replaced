using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Cactus : Growable
{
	private int number;

	private bool isPopped;

	private Duration plantTime;

	protected override double YieldFactor => base.YieldFactor;

	public override ItemBlock Harvest(out Duration collectionDuration)
	{
		collectionDuration = sim.CurrentTime - plantTime;
		if (!Harvestable)
		{
			HarvestEffects();
			sim.farm.grid.RemoveEntity(pos);
			return ItemBlock.CreateEmpty();
		}
		List<Cactus> list = new List<Cactus>();
		int numWeird = 0;
		Pop(list, ref numWeird);
		int count = list.Count;
		foreach (Cactus item in list)
		{
			sim.farm.grid.RemoveEntity(item.pos);
		}
		return GetDrops(count, numWeird);
	}

	protected override void OnFullyGrown()
	{
		base.OnFullyGrown();
		sim.farm.grid.cactusNumbers[pos.x, pos.y] = -1;
		CheckWrongOrderAchievement();
	}

	public override void OnSwapped()
	{
		base.OnSwapped();
		CheckWrongOrderAchievement();
	}

	private void CheckWrongOrderAchievement()
	{
		if (sim.leaderboardType == LeaderboardType.none && sim.farm.grid.entities.Count == sim.farm.grid.WorldSize.x * sim.farm.grid.WorldSize.y && IsReverseSorted() && Enumerable.All<Cactus>(Enumerable.OfType<Cactus>((IEnumerable)sim.farm.grid.entities.Values), (Func<Cactus, bool>)((Cactus c) => c.IsReverseSorted())) && Enumerable.Count<Cactus>(Enumerable.OfType<Cactus>((IEnumerable)sim.farm.grid.entities.Values)) == sim.farm.grid.WorldSize.x * sim.farm.grid.WorldSize.y)
		{
			Achievements.UnlockAchievement("WRONG_ORDER");
		}
	}

	public override ItemBlock GetYield()
	{
		CountSorted(out var count, out var numWeird);
		return GetDrops(count, numWeird);
	}

	private ItemBlock GetDrops(int numPopped, int numWeird)
	{
		double num = YieldFactor * (double)numPopped;
		double num2 = Math.Floor(0.5 * (double)numWeird * num);
		double n = (double)numPopped * num - num2;
		ItemBlock itemBlock = new ItemBlock(StringIds.CactusItemId, n);
		if (num2 > 0.0)
		{
			itemBlock.AddItem(StringIds.WeirdSubstanceItemId, num2);
		}
		return itemBlock;
	}

	private void Pop(List<Cactus> popped, ref int numWeird)
	{
		if (isPopped || !Harvestable)
		{
			return;
		}
		isPopped = true;
		popped.Add(this);
		if (weird)
		{
			numWeird++;
		}
		HarvestEffects();
		FarmObject[] neighbors = sim.farm.grid.GetNeighbors(pos);
		if (Enumerable.All<FarmObject>((IEnumerable<FarmObject>)neighbors, (Func<FarmObject, bool>)((FarmObject f) => !(f is Cactus cactus) || cactus.IsSorted())))
		{
			FarmObject[] array = neighbors;
			for (int i = 0; i < array.Length; i++)
			{
				(array[i] as Cactus)?.Pop(popped, ref numWeird);
			}
		}
	}

	private void CountSorted(out int count, out int numWeird)
	{
		if (!IsSorted())
		{
			count = 0;
			numWeird = 0;
			return;
		}
		numWeird = 0;
		HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>();
		List<Vector2Int> list = new List<Vector2Int> { pos };
		while (list.Count > 0)
		{
			Vector2Int vector2Int = list[list.Count - 1];
			list.RemoveAt(list.Count - 1);
			if (!hashSet.Contains(vector2Int) && sim.farm.grid.entities.TryGetValue(vector2Int, out var value) && value is Cactus { Harvestable: not false } cactus && cactus.IsSorted())
			{
				hashSet.Add(vector2Int);
				if (cactus.weird)
				{
					numWeird++;
				}
				Vector2Int[] array = sim.farm.grid.NeighborPositions(vector2Int);
				foreach (Vector2Int item in array)
				{
					list.Add(item);
				}
			}
		}
		count = hashSet.Count;
	}

	private bool IsSorted()
	{
		FarmObject[] neighbors = sim.farm.grid.GetNeighbors(pos);
		if (Enumerable.All<FarmObject>(Enumerable.Take<FarmObject>((IEnumerable<FarmObject>)neighbors, 2), (Func<FarmObject, bool>)((FarmObject f) => !(f is Cactus cactus) || (cactus.Harvestable && cactus.number >= number))))
		{
			return Enumerable.All<FarmObject>(Enumerable.Skip<FarmObject>((IEnumerable<FarmObject>)neighbors, 2), (Func<FarmObject, bool>)((FarmObject f) => !(f is Cactus cactus2) || (cactus2.Harvestable && cactus2.number <= number)));
		}
		return false;
	}

	private bool IsReverseSorted()
	{
		FarmObject[] neighbors = sim.farm.grid.GetNeighbors(pos);
		if (Enumerable.All<FarmObject>(Enumerable.Take<FarmObject>((IEnumerable<FarmObject>)neighbors, 2), (Func<FarmObject, bool>)((FarmObject f) => !(f is Cactus cactus) || (cactus.Harvestable && cactus.number <= number))))
		{
			return Enumerable.All<FarmObject>(Enumerable.Skip<FarmObject>((IEnumerable<FarmObject>)neighbors, 2), (Func<FarmObject, bool>)((FarmObject f) => !(f is Cactus cactus2) || (cactus2.Harvestable && cactus2.number >= number)));
		}
		return false;
	}

	public override IPyObject Measure()
	{
		return new PyNumber(number);
	}

	public override IEnumerable<(Mesh, Matrix4x4)> GetMeshes()
	{
		Matrix4x4 transform = GetTransform();
		yield return (objectSO.meshes[(!Harvestable || IsSorted()) ? number : (10 + number)], transform);
		if (weird)
		{
			yield return (objectSO.gooMeshes[gooIndex], Matrix4x4.Translate(GridManager.CellToLocal(pos)));
		}
	}

	public override void OnRestart()
	{
		base.OnRestart();
		if (sim.farm.grid.cactusNumbers[pos.x, pos.y] == -1)
		{
			sim.farm.grid.cactusNumbers[pos.x, pos.y] = sim.randomCactus.Next(10);
		}
		number = sim.farm.grid.cactusNumbers[pos.x, pos.y];
		isPopped = false;
		plantTime = sim.CurrentTime;
	}
}
