using System.Collections.Generic;
using UnityEngine;

public class Apple : Growable
{
	public Vector2Int nextPos;

	private bool hasNextPos;

	public override bool Harvestable => false;

	public override ItemBlock Harvest(out Duration collectionDuration)
	{
		collectionDuration = default(Duration);
		return ItemBlock.CreateEmpty();
	}

	public override IPyObject Measure()
	{
		if (hasNextPos)
		{
			return new PyTuple(new List<IPyObject>
			{
				new PyNumber(nextPos.x),
				new PyNumber(nextPos.y)
			});
		}
		return new PyNone();
	}

	public void ChooseTarget()
	{
		if (sim.farm.grid.WorldSize.y > 1)
		{
			GridManager grid = sim.farm.grid;
			FarmObject valueOrDefault;
			do
			{
				nextPos = new Vector2Int(sim.randomSnake.Next(sim.farm.grid.WorldSize.x), sim.randomSnake.Next(0, sim.farm.grid.WorldSize.y));
				valueOrDefault = grid.entities.GetValueOrDefault(nextPos);
			}
			while (valueOrDefault is Dinosaur || valueOrDefault is Apple);
			hasNextPos = true;
		}
	}
}
