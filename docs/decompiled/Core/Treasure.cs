using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Treasure : HedgePlant
{
	private const int MAX_TREASURE_FACTOR = 301;

	private int treasureFactor = 1;

	private Vector2Int nextPos;

	private Duration startTime;

	private bool repositioning;

	public override ItemBlock Harvest(out Duration collectionDuration)
	{
		base.Harvest(out collectionDuration);
		if (sim.leaderboardType == LeaderboardType.none && mazeSize == sim.farm.grid.WorldSize.x)
		{
			Achievements.UnlockAchievement("TREASURE_HUNTER");
		}
		collectionDuration = Duration.FromSeconds(0.0);
		double n = objectSO.dropAmount * (double)mazeSize * (double)mazeSize * YieldFactor;
		return new ItemBlock(objectSO.dropItemId, n);
	}

	public override ItemBlock GetYield()
	{
		double n = objectSO.dropAmount * (double)mazeSize * (double)mazeSize * YieldFactor;
		return new ItemBlock(objectSO.dropItemId, n);
	}

	public override IEnumerable<(Mesh, Matrix4x4)> GetMeshes()
	{
		return Enumerable.Append<(Mesh, Matrix4x4)>(base.GetMeshes(), (objectSO.meshes[1], GetTransform()));
	}

	public bool RepositionTreasure(int desired_size, out bool useActionTicks)
	{
		if (mazeSize <= 1 || desired_size < mazeSize || treasureFactor >= 301)
		{
			useActionTicks = false;
			return false;
		}
		if (repositioning)
		{
			useActionTicks = true;
			return false;
		}
		repositioning = true;
		double n = objectSO.dropAmount * (double)mazeSize * (double)mazeSize * YieldFactor;
		sim.farm.CollectItems(new ItemBlock(objectSO.dropItemId, n), GetTransform().GetPosition(), Duration.FromSeconds(0.0));
		sim.mainSim?.PlaySound(SoundEffectType.HarvestTreasureChest, pos);
		Vector3 localPosition = base.LocalPosition;
		sim.mainSim?.PlayEffect(VFXType.hit, localPosition, changeColor: true, objectSO.color);
		sim.mainSim?.PlayEffect(VFXType.dust, localPosition);
		sim.StartTimer(CompleteRepositioning, sim.OpDuration * 200.0);
		useActionTicks = true;
		return true;
	}

	private void CompleteRepositioning()
	{
		if (!repositioning || removed)
		{
			return;
		}
		repositioning = false;
		HedgePlant hedgePlant = sim.farm.grid.entities[nextPos] as HedgePlant;
		hedgePlant.removed = true;
		Treasure treasure = (Treasure)sim.farm.grid.SetEntity(nextPos, "treasure");
		treasure.walls = hedgePlant.walls;
		treasure.treasureFactor = treasureFactor + 1;
		treasure.startTime = startTime;
		treasure.mazeSize = mazeSize;
		treasure.lowLeftCorner = lowLeftCorner;
		treasure.ChooseNextPosition();
		if (sim.leaderboardType == LeaderboardType.none && treasure.treasureFactor >= 300)
		{
			Achievements.UnlockAchievement("RECYCLING");
		}
		removed = true;
		HedgePlant obj = (HedgePlant)sim.farm.grid.SetEntity(pos, "hedge");
		obj.walls = walls;
		obj.mazeSize = mazeSize;
		obj.lowLeftCorner = lowLeftCorner;
		for (int i = lowLeftCorner.x; i < lowLeftCorner.x + mazeSize; i++)
		{
			for (int j = lowLeftCorner.y; j < lowLeftCorner.y + mazeSize; j++)
			{
				if ((i + j) % 2 == 1)
				{
					continue;
				}
				Vector2Int vector2Int = new Vector2Int(i, j);
				foreach (GridDirection item in Enumerable.Select<int, GridDirection>(Enumerable.Range(0, 4), (Func<int, GridDirection>)((int d) => (GridDirection)d)))
				{
					Vector2Int key = vector2Int + item.GetDirectionVector();
					if (!(sim.randomMaze.NextDouble() > 0.002) && key.x >= lowLeftCorner.x && key.y >= lowLeftCorner.y && key.x < lowLeftCorner.x + mazeSize && key.y < lowLeftCorner.y + mazeSize)
					{
						((HedgePlant)sim.farm.grid.entities[vector2Int]).walls[(int)item] = false;
						((HedgePlant)sim.farm.grid.entities[key]).walls[(int)item.Reverse()] = false;
					}
				}
			}
		}
	}

	public override void OnRestart()
	{
		base.OnRestart();
		treasureFactor = 1;
		startTime = sim.CurrentTime;
		repositioning = false;
	}

	public override IPyObject Measure()
	{
		return new PyTuple(new List<IPyObject>
		{
			new PyNumber(pos.x),
			new PyNumber(pos.y)
		});
	}

	public override string ToSerializeString()
	{
		return $"{base.ToSerializeString()},treasureFactor = {treasureFactor},nextX = {nextPos.x},nextY = {nextPos.y},lifetime = {sim.CurrentTime - startTime}";
	}

	public override void SetValues(Dictionary<string, string> values)
	{
		base.SetValues(values);
		if (values.ContainsKey("treasureFactor") && int.TryParse(values["treasureFactor"], out var result))
		{
			treasureFactor = result;
		}
		if (values.ContainsKey("nextX") && int.TryParse(values["nextX"], out var result2))
		{
			nextPos.x = result2;
		}
		if (values.ContainsKey("nextY") && int.TryParse(values["nextY"], out var result3))
		{
			nextPos.x = result3;
		}
		if (values.ContainsKey("lifetime") && double.TryParse(values["lifetime"], out var result4))
		{
			startTime = sim.CurrentTime - Duration.FromSeconds(result4);
		}
	}

	public void ChooseNextPosition()
	{
		if (mazeSize > 1)
		{
			do
			{
				nextPos = new Vector2Int(sim.randomMaze.Next(mazeSize), sim.randomMaze.Next(mazeSize)) + lowLeftCorner;
			}
			while (nextPos == pos);
		}
	}
}
