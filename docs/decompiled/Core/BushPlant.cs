using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BushPlant : Growable
{
	public void GenerateHedgeMaze(int desired_size)
	{
		int num = Mathf.Min(desired_size, sim.farm.grid.WorldSize.x);
		Vector2Int vector2Int = Vector2Int.Max(Vector2Int.zero, pos - new Vector2Int(num / 2, num / 2));
		while (vector2Int.x + num > sim.farm.grid.WorldSize.x)
		{
			vector2Int.x--;
		}
		while (vector2Int.y + num > sim.farm.grid.WorldSize.y)
		{
			vector2Int.y--;
		}
		bool[][] array = new bool[num * num][];
		int num2 = sim.randomMaze.Next(num * num);
		array[num2] = new bool[4] { true, true, true, true };
		List<int> list = new List<int> { num2 };
		bool flag = false;
		List<int> list2 = new List<int>();
		while (list.Count > 0)
		{
			int num3 = list[list.Count - 1];
			int num4 = num3 % num;
			int num5 = num3 / num;
			foreach (GridDirection item in (IEnumerable<GridDirection>)Enumerable.OrderBy<GridDirection, int>((IEnumerable<GridDirection>)(GridDirection[])Enum.GetValues(typeof(GridDirection)), (Func<GridDirection, int>)((GridDirection k) => sim.randomMaze.Next())))
			{
				Vector2Int directionVector = item.GetDirectionVector();
				int num6 = num4 + directionVector.x;
				int num7 = num5 + directionVector.y;
				if (num6 >= 0 && num7 >= 0 && num6 < num && num7 < num && array[num6 + num * num7] == null)
				{
					list.Add(num6 + num * num7);
					array[num6 + num * num7] = new bool[4] { true, true, true, true };
					array[num6 + num * num7][(int)item.Reverse()] = false;
					array[num3][(int)item] = false;
					flag = false;
					break;
				}
			}
			if (list[list.Count - 1] == num3)
			{
				list.RemoveAt(list.Count - 1);
				if (!flag)
				{
					list2.Add(num3);
					flag = true;
				}
			}
		}
		int num8 = list2[sim.randomMaze.Next(list2.Count - 1)];
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				bool flag2 = num8 == i + num * j;
				sim.farm.grid.SetGround(new Vector2Int(i, j) + vector2Int, "grassland");
				HedgePlant hedgePlant = (HedgePlant)sim.farm.grid.SetEntity(new Vector2Int(i, j) + vector2Int, flag2 ? "treasure" : "hedge");
				hedgePlant.walls = array[i + num * j];
				hedgePlant.lowLeftCorner = vector2Int;
				hedgePlant.mazeSize = num;
				if (flag2)
				{
					((Treasure)hedgePlant).ChooseNextPosition();
				}
			}
		}
		if (sim.leaderboardType == LeaderboardType.none)
		{
			Achievements.UnlockAchievement("SPAWN_MAZE");
		}
	}
}
