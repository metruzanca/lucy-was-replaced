using System;
using UnityEngine;

public static class GridDirectionMethods
{
	public static Vector2Int GetDirectionVector(this GridDirection dir)
	{
		return dir switch
		{
			GridDirection.South => Vector2Int.down, 
			GridDirection.East => Vector2Int.right, 
			GridDirection.North => Vector2Int.up, 
			_ => Vector2Int.left, 
		};
	}

	public static GridDirection RotateLeft(this GridDirection dir)
	{
		return dir switch
		{
			GridDirection.South => GridDirection.West, 
			GridDirection.East => GridDirection.South, 
			GridDirection.North => GridDirection.East, 
			_ => GridDirection.North, 
		};
	}

	public static GridDirection RotateRight(this GridDirection dir)
	{
		return dir switch
		{
			GridDirection.South => GridDirection.East, 
			GridDirection.East => GridDirection.North, 
			GridDirection.North => GridDirection.West, 
			_ => GridDirection.South, 
		};
	}

	public static GridDirection Reverse(this GridDirection dir)
	{
		return dir switch
		{
			GridDirection.South => GridDirection.North, 
			GridDirection.East => GridDirection.West, 
			GridDirection.North => GridDirection.South, 
			_ => GridDirection.East, 
		};
	}

	public static float GetAngle(this GridDirection dir)
	{
		return dir switch
		{
			GridDirection.South => 180f, 
			GridDirection.East => 90f, 
			GridDirection.North => 0f, 
			_ => 270f, 
		};
	}

	public static Quaternion GetQuaternion(this GridDirection dir)
	{
		return Quaternion.AngleAxis(dir.GetAngle(), Vector3.forward);
	}

	public static GridDirection RandomGridDirection(System.Random random)
	{
		return (GridDirection)random.Next(0, 4);
	}

	public static GridDirection FromVector(Vector2Int v)
	{
		if (Math.Abs(v.y) >= Math.Abs(v.x))
		{
			if (v.y < 0)
			{
				return GridDirection.South;
			}
			return GridDirection.North;
		}
		if (v.x < 0)
		{
			return GridDirection.West;
		}
		return GridDirection.East;
	}
}
