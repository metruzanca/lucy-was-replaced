using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PumpkinController
{
	private const int greenOffset = 5;

	private GridManager gm;

	private Dictionary<Vector2Int, Pumpkin> pumpkins = new Dictionary<Vector2Int, Pumpkin>();

	private Dictionary<Pumpkin, RectInt> groups = new Dictionary<Pumpkin, RectInt>();

	public PumpkinController(GridManager gm)
	{
		this.gm = gm;
	}

	public void AddPumpkin(Pumpkin p)
	{
		Vector2Int pos = p.pos;
		pumpkins[pos] = p;
		MergeWithOthers(pos);
		RectInt r = groups[p];
		foreach (Vector2Int item in IterPositions(r))
		{
			bool[] array = new bool[4]
			{
				item.y < r.yMax,
				item.x < r.xMax,
				item.y > r.yMin,
				item.x > r.xMin
			};
			int num;
			GridDirection orientation;
			switch ((array[0] ? 1 : 0) + (array[1] ? 1 : 0) + (array[2] ? 1 : 0) + (array[3] ? 1 : 0))
			{
			case 4:
				num = 5;
				orientation = GridDirection.North;
				break;
			case 3:
				num = 4;
				orientation = ((!array[0]) ? GridDirection.South : ((!array[3]) ? GridDirection.East : (array[2] ? GridDirection.West : GridDirection.North)));
				break;
			case 2:
				if (array[0] && array[2])
				{
					num = 2;
					orientation = GridDirection.North;
				}
				else if (array[1] && array[3])
				{
					num = 2;
					orientation = GridDirection.East;
				}
				else
				{
					num = 3;
					orientation = ((!array[0]) ? ((!array[3]) ? GridDirection.East : GridDirection.South) : (array[3] ? GridDirection.West : GridDirection.North));
				}
				break;
			case 1:
				num = 1;
				orientation = ((!array[0]) ? (array[3] ? GridDirection.West : ((!array[2]) ? GridDirection.East : GridDirection.South)) : GridDirection.North);
				break;
			default:
				num = 0;
				orientation = GridDirection.North;
				break;
			}
			if (num != 0 && item == (r.max + Vector2Int.one) / 2)
			{
				num += 5;
			}
			pumpkins[item].SetMesh(num, orientation);
		}
	}

	public Vector2Int RemovePumpkin(Pumpkin p, out int numWeird)
	{
		numWeird = 0;
		if (!groups.ContainsKey(p))
		{
			gm.RemoveEntity(p.pos);
			return Vector2Int.zero;
		}
		Vector2Int size = groups[p].size;
		foreach (Vector2Int item in IterPositions(groups[p]))
		{
			groups.Remove(pumpkins[item]);
			Pumpkin pumpkin = gm.entities.GetValueOrDefault(item) as Pumpkin;
			if (pumpkin != null && pumpkin.weird)
			{
				numWeird++;
			}
			pumpkin?.HarvestEffects();
			gm.RemoveEntity(item);
			pumpkins.Remove(item);
		}
		return size + Vector2Int.one;
	}

	public Vector2Int GetSize(Pumpkin p, out int numWeird)
	{
		numWeird = 0;
		if (!groups.ContainsKey(p))
		{
			return Vector2Int.zero;
		}
		Vector2Int size = groups[p].size;
		foreach (Vector2Int item in IterPositions(groups[p]))
		{
			if (gm.entities.GetValueOrDefault(item) is Pumpkin { weird: not false })
			{
				numWeird++;
			}
		}
		return size + Vector2Int.one;
	}

	private void MergeWithOthers(Vector2Int pos)
	{
		int y = gm.farm.grid.WorldSize.y;
		int[] array = new int[y * y];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (pumpkins.ContainsKey(new Vector2Int(i % y, i / y)) ? 1 : 0);
		}
		bool flag = true;
		int num = 1;
		while (flag)
		{
			num++;
			flag = false;
			for (int j = 0; j < array.Length; j++)
			{
				int num2 = j % y;
				int num3 = j / y;
				if (num2 + 1 < y && num3 + 1 < y && array[j] == num - 1 && array[num2 + 1 + y * num3] == num - 1 && array[num2 + y * (num3 + 1)] == num - 1 && array[num2 + 1 + y * (num3 + 1)] == num - 1)
				{
					array[j] = num;
					flag = true;
				}
			}
		}
		num--;
		RectInt rectInt = LargestSquare(array, y, num, pos);
		Pumpkin pumpkin = pumpkins[Enumerable.First<Vector2Int>(IterPositions(rectInt))];
		foreach (Vector2Int item in IterPositions(rectInt))
		{
			groups[pumpkins[item]] = rectInt;
			pumpkins[item].mysteriousNumber = pumpkin.mysteriousNumber;
		}
	}

	private void PrintDP(int[] dp, int n)
	{
		string text = "";
		for (int i = 0; i < n; i++)
		{
			for (int j = 0; j < n; j++)
			{
				text += dp[j + n * i];
			}
			text += "\n";
		}
		Debug.Log(text);
	}

	private RectInt LargestSquare(int[] dp, int n, int squareSize, Vector2Int pos)
	{
		while (squareSize > 1)
		{
			for (int i = 0; i < dp.Length; i++)
			{
				RectInt rectInt = new RectInt(new Vector2Int(i % n, i / n), new Vector2Int(squareSize - 1, squareSize - 1));
				if (dp[i] >= squareSize && IsInRect(rectInt, pos) && !HasOverlaps(rectInt))
				{
					return rectInt;
				}
			}
			squareSize--;
		}
		return new RectInt(pos, Vector2Int.zero);
	}

	private bool HasOverlaps(RectInt r)
	{
		foreach (Vector2Int item in IterPositions(r))
		{
			if (groups.ContainsKey(pumpkins[item]))
			{
				RectInt rectInt = groups[pumpkins[item]];
				if (rectInt.max.x > r.max.x || rectInt.max.y > r.max.y || rectInt.min.x < r.min.x || rectInt.min.y < r.min.y)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool IsInRect(RectInt r, Vector2Int pos)
	{
		if (pos.x >= r.xMin && pos.y >= r.yMin && pos.x <= r.xMax)
		{
			return pos.y <= r.yMax;
		}
		return false;
	}

	private IEnumerable<Vector2Int> IterPositions(RectInt r)
	{
		int xMax = r.xMax;
		int yMax = r.yMax;
		for (int i = r.xMin; i <= xMax; i++)
		{
			for (int j = r.yMin; j <= yMax; j++)
			{
				yield return new Vector2Int(i, j);
			}
		}
	}
}
