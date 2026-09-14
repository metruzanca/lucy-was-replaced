using System.Collections.Generic;
using UnityEngine;

public class HedgePlant : FarmObject
{
	public bool[] walls;

	public int mazeSize;

	public Vector2Int lowLeftCorner;

	public bool removed;

	public override IEnumerable<(Mesh, Matrix4x4)> GetMeshes()
	{
		for (int i = 0; i < 4; i++)
		{
			if (walls[i] && (i < 2 || pos.x == lowLeftCorner.x || pos.y == lowLeftCorner.y))
			{
				Quaternion q = Quaternion.AngleAxis(90 * i, Vector3.forward);
				yield return (objectSO.meshes[0], Matrix4x4.TRS(positionEnd, q, Vector3.one));
			}
		}
	}

	private void RemoveMaze()
	{
		if (removed)
		{
			return;
		}
		for (int i = 0; i < mazeSize; i++)
		{
			for (int j = 0; j < mazeSize; j++)
			{
				Vector2Int key = new Vector2Int(i, j) + lowLeftCorner;
				if (sim.farm.grid.entities.GetValueOrDefault(key) is HedgePlant { removed: false } hedgePlant)
				{
					hedgePlant.removed = true;
					sim.farm.grid.RemoveEntity(key);
				}
			}
		}
	}

	public override ItemBlock Harvest(out Duration collectionDuration)
	{
		RemoveMaze();
		HarvestEffects();
		return base.Harvest(out collectionDuration);
	}

	public override bool CanDroneMoveToFrom(GridDirection direction)
	{
		return !walls[(int)direction];
	}

	public override bool CanDroneMoveAwayTo(GridDirection direction)
	{
		return !walls[(int)direction];
	}

	public override string ToSerializeString()
	{
		return $"{base.ToSerializeString()},north = {walls[0]},east = {walls[1]},south = {walls[2]},west = {walls[3]}";
	}

	public override void SetValues(Dictionary<string, string> values)
	{
		base.SetValues(values);
		walls = new bool[4];
		if (values.ContainsKey("north") && bool.TryParse(values["north"], out var result))
		{
			walls[0] = result;
		}
		if (values.ContainsKey("east") && bool.TryParse(values["east"], out var result2))
		{
			walls[1] = result2;
		}
		if (values.ContainsKey("south") && bool.TryParse(values["south"], out var result3))
		{
			walls[2] = result3;
		}
		if (values.ContainsKey("west") && bool.TryParse(values["west"], out var result4))
		{
			walls[3] = result4;
		}
	}

	public override void OnFree()
	{
		base.OnFree();
		RemoveMaze();
	}

	public override IPyObject Measure()
	{
		for (int i = lowLeftCorner.x; i < lowLeftCorner.x + mazeSize; i++)
		{
			for (int j = lowLeftCorner.y; j < lowLeftCorner.y + mazeSize; j++)
			{
				if (sim.farm.grid.entities.GetValueOrDefault(new Vector2Int(i, j)) is Treasure)
				{
					return new PyTuple(new List<IPyObject>
					{
						new PyNumber(i),
						new PyNumber(j)
					});
				}
			}
		}
		return PyNone.Instance;
	}
}
