using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class GridManager
{
	private const double MAX_WATER_VOLUME = 1.0;

	private const double WATER_DECAY_INTERVAL = 0.1;

	private const double WATER_DECAY_PROBABILITY = 0.1;

	private const double WATER_DECAY_FACTOR = 0.99;

	public Farm farm;

	public PumpkinController pumpkinController;

	private static Dictionary<string, Type> farmObjectTypes;

	private int sizeLimit;

	public Dictionary<Vector2Int, FarmObject> entities = new Dictionary<Vector2Int, FarmObject>();

	public Dictionary<Vector2Int, FarmObject> grounds = new Dictionary<Vector2Int, FarmObject>();

	public double[,] waterVolume;

	public int[,] cactusNumbers;

	public bool hadIncorrectSunflowerHarvest;

	public Vector2Int WorldSize
	{
		get
		{
			int num = Helper.WorldSizeScale(farm.NumUnlocked("expand"));
			if (sizeLimit > 0 && sizeLimit < num)
			{
				return new Vector2Int(sizeLimit, sizeLimit);
			}
			if (num != 2)
			{
				return new Vector2Int(num, num);
			}
			return new Vector2Int(1, 3);
		}
	}

	public int SizeLimit
	{
		get
		{
			return sizeLimit;
		}
		set
		{
			if (value > 2)
			{
				if (value < WorldSize.y)
				{
					sizeLimit = value;
					GenerateWorld(shrinkFarm: true);
				}
				else if (value > WorldSize.y)
				{
					sizeLimit = value;
					GenerateWorld();
				}
			}
			else if (sizeLimit > 0)
			{
				sizeLimit = 0;
				GenerateWorld();
			}
		}
	}

	public Vector3 midPoint => (CellToLocal(WorldSize) + CellToLocal(Vector2Int.zero) + Vector3.left) / 2f;

	public GridManager(Farm farm, List<SFO> loadedGrounds = null, List<SFO> loadedEntities = null)
	{
		this.farm = farm;
		farm.grid = this;
		pumpkinController = new PumpkinController(this);
		GenerateWorld(shrinkFarm: true);
		if (loadedGrounds != null && loadedGrounds.Count > 0)
		{
			ClearGrid(spawnGrass: false);
			foreach (SFO loadedGround in loadedGrounds)
			{
				LoadFromString(loadedGround.pos, loadedGround.data, isGround: true);
			}
		}
		if (loadedEntities != null)
		{
			foreach (SFO loadedEntity in loadedEntities)
			{
				LoadFromString(loadedEntity.pos, loadedEntity.data, isGround: false);
			}
		}
		farm.sim.StartTimer(WaterDecay, Duration.FromSeconds(0.1));
	}

	public void SetWaterVolume(Vector2Int pos, double volume)
	{
		double num = waterVolume[pos.x, pos.y];
		waterVolume[pos.x, pos.y] = Math.Clamp(volume, 0.0, 1.0);
		if (waterVolume[pos.x, pos.y] != num && entities.TryGetValue(pos, out var value) && value is Growable growable)
		{
			growable.UpdateFarmObject();
		}
	}

	private void WaterDecay()
	{
		for (int i = 0; i < waterVolume.GetLength(0); i++)
		{
			for (int j = 0; j < waterVolume.GetLength(1); j++)
			{
				Vector2Int pos = new Vector2Int(i, j);
				if (farm.sim.randomVarious.NextDouble() < 0.1)
				{
					SetWaterVolume(pos, waterVolume[i, j] * 0.99);
				}
			}
		}
		farm.sim.StartTimer(WaterDecay, Duration.FromSeconds(0.1));
	}

	public void ClearGrid(bool spawnGrass = true)
	{
		Vector2Int worldSize = WorldSize;
		for (int i = 0; i < worldSize.x; i++)
		{
			for (int j = 0; j < worldSize.y; j++)
			{
				Vector2Int vector2Int = new Vector2Int(i, j);
				if (!grounds.ContainsKey(vector2Int) || grounds[vector2Int].objectSO.objectName != "grassland")
				{
					SetGround(vector2Int, "grassland");
				}
				if (spawnGrass)
				{
					SetEntity(vector2Int, "grass");
				}
				else
				{
					RemoveEntity(vector2Int, regrowGrass: false);
				}
			}
		}
	}

	public void GenerateWorld(bool shrinkFarm = false)
	{
		if (shrinkFarm)
		{
			Vector2Int[] array = Enumerable.ToArray<Vector2Int>((IEnumerable<Vector2Int>)grounds.Keys);
			foreach (Vector2Int vector2Int in array)
			{
				if (!IsWithinBounds(vector2Int))
				{
					Free(grounds[vector2Int]);
					grounds.Remove(vector2Int);
					if (entities.ContainsKey(vector2Int))
					{
						RemoveEntity(vector2Int, regrowGrass: false);
					}
				}
			}
		}
		Vector2Int worldSize = WorldSize;
		double[,] array2 = new double[WorldSize.x, WorldSize.y];
		for (int j = 0; j < worldSize.x; j++)
		{
			for (int k = 0; k < worldSize.y; k++)
			{
				if (waterVolume == null || j >= waterVolume.GetLength(0) || k >= waterVolume.GetLength(1))
				{
					array2[j, k] = 0.0;
				}
				else
				{
					array2[j, k] = waterVolume[j, k];
				}
			}
		}
		waterVolume = array2;
		int[,] array3 = new int[WorldSize.x, WorldSize.y];
		for (int l = 0; l < worldSize.x; l++)
		{
			for (int m = 0; m < worldSize.y; m++)
			{
				if (cactusNumbers == null || l >= cactusNumbers.GetLength(0) || m >= cactusNumbers.GetLength(1))
				{
					array3[l, m] = -1;
				}
				else
				{
					array3[l, m] = cactusNumbers[l, m];
				}
			}
		}
		cactusNumbers = array3;
		ClearGrid();
	}

	public bool Swap(Vector2Int pos, GridDirection dir)
	{
		FarmObject valueOrDefault = farm.grid.entities.GetValueOrDefault(pos);
		Vector2Int vector2Int = pos + dir.GetDirectionVector();
		if (!IsWithinBounds(vector2Int))
		{
			return false;
		}
		FarmObject valueOrDefault2 = farm.grid.entities.GetValueOrDefault(vector2Int);
		if ((valueOrDefault != null && !valueOrDefault.objectSO.canBeSwapped) || (valueOrDefault2 != null && !valueOrDefault2.objectSO.canBeSwapped))
		{
			return false;
		}
		if (farm.sim.mainSim != null && farm.sim.mainSim.TimeFactor == 1.0)
		{
			farm.sim.mainSim.PlayEffect(VFXType.light_dust, CellToLocal(pos));
			farm.sim.mainSim.PlayEffect(VFXType.light_dust, CellToLocal(vector2Int));
		}
		int num = cactusNumbers[pos.x, pos.y];
		cactusNumbers[pos.x, pos.y] = cactusNumbers[vector2Int.x, vector2Int.y];
		cactusNumbers[vector2Int.x, vector2Int.y] = num;
		if (valueOrDefault != null)
		{
			farm.grid.entities[vector2Int] = valueOrDefault;
			valueOrDefault.AnimateMove(CellToLocal(vector2Int), Duration.FromSeconds(0.1));
			valueOrDefault.pos = vector2Int;
			valueOrDefault.UpdateFarmObject();
			valueOrDefault.UpdateNeighbors();
			valueOrDefault.OnSwapped();
		}
		else
		{
			farm.grid.entities.Remove(vector2Int);
		}
		if (valueOrDefault2 != null)
		{
			farm.grid.entities[pos] = valueOrDefault2;
			valueOrDefault2.AnimateMove(CellToLocal(pos), Duration.FromSeconds(0.1));
			valueOrDefault2.pos = pos;
			valueOrDefault2.UpdateFarmObject();
			valueOrDefault2.UpdateNeighbors();
			valueOrDefault2.OnSwapped();
		}
		else
		{
			farm.grid.entities.Remove(pos);
		}
		return true;
	}

	public FarmObject SetGround(Vector2Int pos, string newGround)
	{
		if (grounds.GetValueOrDefault(pos) != null)
		{
			Free(grounds[pos]);
		}
		return SetFarmObject(pos, newGround, isGround: true);
	}

	public FarmObject SetEntity(Vector2Int pos, string newObject)
	{
		if (entities.GetValueOrDefault(pos) != null)
		{
			Free(entities[pos]);
		}
		return SetFarmObject(pos, newObject, isGround: false);
	}

	public void RemoveEntity(Vector2Int pos, bool regrowGrass = true)
	{
		if (entities.ContainsKey(pos))
		{
			Free(entities[pos]);
			if (regrowGrass && grounds[pos].objectSO.objectName == "grassland")
			{
				SetEntity(pos, "grass");
			}
		}
	}

	public Vector2Int[] NeighborPositions(Vector2Int pos)
	{
		return new Vector2Int[4]
		{
			pos + new Vector2Int(0, 1),
			pos + new Vector2Int(1, 0),
			pos + new Vector2Int(0, -1),
			pos + new Vector2Int(-1, 0)
		};
	}

	public FarmObject[] GetNeighbors(Vector2Int pos)
	{
		FarmObject[] array = new FarmObject[4];
		entities.TryGetValue(pos + new Vector2Int(0, 1), out array[0]);
		entities.TryGetValue(pos + new Vector2Int(1, 0), out array[1]);
		entities.TryGetValue(pos + new Vector2Int(0, -1), out array[2]);
		entities.TryGetValue(pos + new Vector2Int(-1, 0), out array[3]);
		return array;
	}

	public Vector2Int Wrap(Vector2Int pos)
	{
		return new Vector2Int((pos.x + WorldSize.x) % WorldSize.x, (pos.y + WorldSize.y) % WorldSize.y);
	}

	public bool IsWithinBounds(Vector2Int pos)
	{
		if (pos.x >= 0 && pos.y >= 0 && pos.x < WorldSize.x)
		{
			return pos.y < WorldSize.y;
		}
		return false;
	}

	public static Vector3 CellToLocal(Vector2Int pos)
	{
		return new Vector3(-pos.x, pos.y, 0f);
	}

	public static Vector2Int LocalToCell(Vector3 localPos)
	{
		return new Vector2Int(Mathf.RoundToInt(0f - localPos.x), Mathf.RoundToInt(localPos.y));
	}

	private void LoadFromString(Vector2Int pos, string s, bool isGround)
	{
		if (string.IsNullOrEmpty(s) || pos.x >= WorldSize.x || pos.y >= WorldSize.y)
		{
			return;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		string[] array = s.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(" = ");
			if (array2.Length == 2)
			{
				dictionary[array2[0]] = array2[1];
			}
		}
		if (dictionary.ContainsKey("type"))
		{
			if (isGround)
			{
				SetGround(pos, dictionary["type"]);
				grounds[pos].SetValues(dictionary);
			}
			else
			{
				SetEntity(pos, dictionary["type"]);
				entities[pos].SetValues(dictionary);
			}
		}
	}

	private FarmObject CreateFarmObject(FarmObjectSO objectSO)
	{
		if (farmObjectTypes == null)
		{
			farmObjectTypes = Enumerable.ToDictionary<Type, string, Type>(Enumerable.Where<Type>((IEnumerable<Type>)typeof(FarmObject).Assembly.GetTypes(), (Func<Type, bool>)((Type type) => type.IsSubclassOf(typeof(FarmObject)) || type == typeof(FarmObject))), (Func<Type, string>)((Type t) => t.Name), (Func<Type, Type>)((Type t) => t));
		}
		return (FarmObject)Activator.CreateInstance(farmObjectTypes[objectSO.className]);
	}

	private FarmObject SetFarmObject(Vector2Int pos, string objName, bool isGround)
	{
		FarmObjectSO farmObject = ResourceManager.GetFarmObject(objName);
		FarmObject farmObject2 = CreateFarmObject(farmObject);
		farmObject2.objectSO = farmObject;
		farmObject2.pos = pos;
		farmObject2.LocalPosition = CellToLocal(pos);
		farmObject2.sim = farm.sim;
		if (isGround)
		{
			grounds[pos] = farmObject2;
		}
		else
		{
			entities[pos] = farmObject2;
		}
		farmObject2.OnRestart();
		if (farm.sim.mainSim != null)
		{
			farm.sim.mainSim.dirty = true;
		}
		return farmObject2;
	}

	public void Free(FarmObject obj)
	{
		if (entities.GetValueOrDefault(obj.pos) == obj)
		{
			entities.Remove(obj.pos);
		}
		obj.OnFree();
	}

	public void PrintGrid()
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < WorldSize.y; i++)
		{
			for (int j = 0; j < WorldSize.x; j++)
			{
				if (j != 0)
				{
					stringBuilder.Append(" ");
				}
				if (entities.TryGetValue(new Vector2Int(j, i), out var value))
				{
					stringBuilder.Append(Enumerable.First<char>((IEnumerable<char>)value.objectSO.objectName));
				}
				else
				{
					stringBuilder.Append("-");
				}
			}
			stringBuilder.Append("\n");
		}
		Debug.Log(stringBuilder.ToString());
	}
}
