using System;
using System.Collections.Generic;
using UnityEngine;

public class Pumpkin : Growable
{
	private const double BREAK_PROBABILITY = 0.2;

	private int meshIndex;

	public double mysteriousNumber;

	public override IEnumerable<(Mesh, Matrix4x4)> GetMeshes()
	{
		Matrix4x4 transform = GetTransform();
		yield return (objectSO.meshes[meshIndex], transform);
		if (weird)
		{
			yield return (objectSO.gooMeshes[gooIndex], transform);
		}
	}

	protected override void OnFullyGrown()
	{
		base.OnFullyGrown();
		if (sim.randomPumpkin.NextDouble() <= 0.2)
		{
			sim.farm.grid.SetEntity(pos, "dead_pumpkin");
		}
		else
		{
			sim.farm.grid.pumpkinController.AddPumpkin(this);
		}
	}

	public override ItemBlock GetYield()
	{
		int numWeird;
		Vector2Int size = sim.farm.grid.pumpkinController.GetSize(this, out numWeird);
		return GetDrops(size, numWeird);
	}

	public override ItemBlock Harvest(out Duration collectionDuration)
	{
		HarvestEffects();
		int numWeird;
		Vector2Int size = sim.farm.grid.pumpkinController.RemovePumpkin(this, out numWeird);
		if (size.x == 32 && sim.leaderboardType == LeaderboardType.none)
		{
			Achievements.UnlockAchievement("GIANT_PUMPKIN");
		}
		collectionDuration = Duration.FromSeconds(0.0);
		return GetDrops(size, numWeird);
	}

	private ItemBlock GetDrops(Vector2Int size, int numWeird)
	{
		double num = (double)Mathf.Min(size.x, 6) * YieldFactor;
		double num2 = Math.Floor((double)numWeird * 0.5 * num);
		double n = (double)(size.x * size.x) * num - num2;
		ItemBlock itemBlock = new ItemBlock(objectSO.dropItemId, n);
		if (num2 > 0.0)
		{
			itemBlock.AddItem(StringIds.WeirdSubstanceItemId, num2);
		}
		return itemBlock;
	}

	public void SetMesh(int m, GridDirection orientation)
	{
		meshIndex = m;
		base.Orientation = orientation;
	}

	public override void OnRestart()
	{
		base.OnRestart();
		meshIndex = 0;
		mysteriousNumber = Helper.JustSha256It(sim.randomPumpkin);
	}

	public override void OnFree()
	{
		base.OnFree();
		sim.farm.grid.pumpkinController.RemovePumpkin(this, out var _);
	}

	public override IPyObject Measure()
	{
		return new PyNumber(mysteriousNumber);
	}
}
