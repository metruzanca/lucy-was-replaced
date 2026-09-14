using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Drone
{
	private enum DroneState
	{
		idle,
		moving,
		action,
		flipping,
		printing,
		petting
	}

	public Vector2Int pos;

	private Simulation sim;

	private DroneSO droneSO;

	public Hat hat;

	private DroneState droneState;

	private Duration animDuration;

	private Duration actionStartTime;

	private Vector3 startPosition;

	private Vector3 endPosition;

	private Quaternion startRotation = Quaternion.identity;

	private Quaternion endRotation = Quaternion.identity;

	private float printWidth;

	private float prevRealTime = -1f;

	private Vector3 prevSpeedMeasurement = Vector3.zero;

	public int DroneId { get; set; }

	public Drone(Simulation sim, Vector2Int pos, int droneId)
	{
		this.sim = sim;
		droneSO = ResourceManager.GetDrone();
		this.pos = pos;
		endPosition = GridManager.CellToLocal(pos);
		droneState = DroneState.idle;
		DroneId = droneId;
		hat = Hat.CreateHat(ResourceManager.GetHat("straw_hat"), sim, this);
		hat.OnEquip(this, null);
	}

	private float ExtendedLerp(float a, float b, float t)
	{
		return a + (b - a) * t;
	}

	private float EaseInOutCubic(float t)
	{
		if (!((double)t < 0.5))
		{
			return 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
		}
		return 4f * t * t * t;
	}

	private float EaseOutCubic(float t)
	{
		return 1f - Mathf.Pow(1f - t, 3f);
	}

	public Matrix4x4 GetTransform()
	{
		float num = (float)((sim.CurrentTime - actionStartTime) / animDuration);
		if (num > 1f)
		{
			droneState = DroneState.idle;
		}
		float x;
		float y;
		float z;
		Quaternion q;
		switch (droneState)
		{
		case DroneState.idle:
			x = endPosition.x;
			y = endPosition.y;
			z = hat.hatSO.droneFlyHeight;
			q = endRotation;
			break;
		case DroneState.moving:
			x = ExtendedLerp(startPosition.x, endPosition.x, num);
			y = ExtendedLerp(startPosition.y, endPosition.y, num);
			z = hat.hatSO.droneFlyHeight;
			q = Quaternion.Slerp(startRotation, endRotation, num);
			break;
		case DroneState.action:
			x = endPosition.x;
			y = endPosition.y;
			z = ExtendedLerp(hat.hatSO.droneFlyHeight, hat.hatSO.droneFlyHeight - 0.4f, droneSO.actionZCurve.Evaluate(num));
			q = endRotation;
			break;
		case DroneState.flipping:
			x = endPosition.x;
			y = endPosition.y;
			z = ExtendedLerp(hat.hatSO.droneFlyHeight, hat.hatSO.droneFlyHeight + 1f, droneSO.flipZCuve.Evaluate(num));
			q = Quaternion.AngleAxis(ExtendedLerp(0f, 360f, EaseOutCubic(num)), Vector3.right);
			break;
		case DroneState.printing:
			x = ExtendedLerp(endPosition.x, endPosition.x + printWidth, droneSO.printXCurve.Evaluate(num));
			y = endPosition.y;
			z = ExtendedLerp(hat.hatSO.droneFlyHeight, hat.hatSO.droneFlyHeight + 1f, droneSO.printZCurve.Evaluate(num));
			q = Quaternion.AngleAxis(ExtendedLerp(0f, 360f, droneSO.printRotationCurve.Evaluate(num)), Vector3.down);
			break;
		case DroneState.petting:
			x = ExtendedLerp(endPosition.x, startPosition.x, droneSO.petMoveXYCurve.Evaluate(num));
			y = ExtendedLerp(endPosition.y, startPosition.y, droneSO.petMoveXYCurve.Evaluate(num));
			z = ExtendedLerp(hat.hatSO.droneFlyHeight, startPosition.z, droneSO.petMoveZCurve.Evaluate(num));
			q = endRotation;
			break;
		default:
			throw new Exception("this case shouldn't be reachable");
		}
		return Matrix4x4.TRS(new Vector3(x, y, z), q, Vector3.one);
	}

	public void ChangeHat(HatSO hatSO, ProgramState programState)
	{
		if (!hatSO.hidden && !sim.farm.IsUnlocked(hatSO.hatName))
		{
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_missing_x_unlock", hatSO));
		}
		Hat obj = hat;
		hat = Hat.CreateHat(hatSO, sim, this);
		obj?.OnUnequip();
		hat.OnEquip(this, programState);
		if (!hat.hatSO.rotateDroneToMove)
		{
			endRotation = Quaternion.identity;
		}
		if (sim.leaderboardType == LeaderboardType.none)
		{
			if (hatSO.hatName != "straw_hat")
			{
				Achievements.UnlockAchievement("EQUIP_A_NEW_HAT");
			}
			if (Enumerable.Count<HatSO>(Enumerable.Distinct<HatSO>(Enumerable.Select<Drone, HatSO>(Enumerable.Where<Drone>((IEnumerable<Drone>)sim.farm.drones, (Func<Drone, bool>)((Drone d) => d != null)), (Func<Drone, HatSO>)((Drone d) => d.hat.hatSO)))) >= 5)
			{
				Achievements.UnlockAchievement("FASHION_SHOW");
			}
		}
	}

	public void DoAFlip()
	{
		if (!hat.hatSO.rotateDroneToMove)
		{
			droneState = DroneState.flipping;
			actionStartTime = sim.CurrentTime;
			animDuration = Duration.FromSeconds(1.0);
			if (sim.leaderboardType == LeaderboardType.none)
			{
				Achievements.IncrementStat("flips", 1);
			}
		}
	}

	public void PetThePiggy()
	{
		if (!hat.hatSO.rotateDroneToMove)
		{
			droneState = DroneState.petting;
			actionStartTime = sim.CurrentTime;
			animDuration = Duration.FromSeconds(1.0);
			endPosition = GetTransform().GetPosition();
			startPosition = MainSim.Inst.pb.Position;
			endRotation = Quaternion.identity;
			startRotation = Quaternion.identity;
			sim.mainSim?.PlayEffect(VFXType.hearts, startPosition);
			if (sim.leaderboardType == LeaderboardType.none)
			{
				Achievements.UnlockAchievement("PET_THE_PIGGY");
			}
		}
	}

	public void PrintToAir(string s)
	{
		if (!hat.hatSO.rotateDroneToMove)
		{
			droneState = DroneState.printing;
			actionStartTime = sim.CurrentTime;
			animDuration = Duration.FromSeconds(1.0);
		}
		if (sim.mainSim != null)
		{
			sim.mainSim?.PlayEffect(VFXType.print_sign, GetTransform().GetPosition(), changeColor: false, default(Color), s);
			printWidth = droneSO.extraPrintWidth + (float)Mathf.Min(droneSO.charactersAtMaxPrintWidth, s.Length) * droneSO.characterPrintWidth;
		}
	}

	public bool Move(GridDirection direction, ProgramState programState, out double ops)
	{
		ops = 1.0;
		Vector2Int newPos = pos + direction.GetDirectionVector();
		Vector2Int b = sim.farm.grid.Wrap(newPos);
		if (!CanMove(direction))
		{
			return false;
		}
		ops = hat.OnMove(pos, newPos, programState);
		if (ops <= 0.0)
		{
			ops = 200.0;
		}
		if (hat.hatSO.rotateDroneToMove)
		{
			startRotation = endRotation;
			endRotation = direction.GetQuaternion();
		}
		float num = Vector2Int.Distance(pos, b);
		pos = b;
		if (num > 8f)
		{
			UpdatePosition(0.0);
		}
		else
		{
			UpdatePosition(ops);
		}
		return true;
	}

	public bool CanMove(GridDirection direction)
	{
		Vector2Int vector2Int = pos + direction.GetDirectionVector();
		Vector2Int vector2Int2 = sim.farm.grid.Wrap(vector2Int);
		if (hat.hatSO.preventWrapping && vector2Int2 != vector2Int)
		{
			return false;
		}
		FarmObject valueOrDefault = sim.farm.grid.entities.GetValueOrDefault(vector2Int2, null);
		FarmObject valueOrDefault2 = sim.farm.grid.entities.GetValueOrDefault(pos, null);
		if ((valueOrDefault != null && !valueOrDefault.CanDroneMoveToFrom(direction.Reverse())) || (valueOrDefault2 != null && !valueOrDefault2.CanDroneMoveAwayTo(direction)))
		{
			return false;
		}
		return true;
	}

	public bool Swap(GridDirection dir, ProgramState state)
	{
		if (sim.farm.grid.Swap(pos, dir))
		{
			sim.mainSim?.PlaySound(SoundEffectType.SwapPlants, pos);
			ActionTween();
			return true;
		}
		Vector2Int vector2Int = pos + dir.GetDirectionVector();
		if (!sim.farm.grid.IsWithinBounds(vector2Int))
		{
			Logger.LogWarning(Localizer.Localize("warning_failed_swap_wrap"), state);
		}
		else
		{
			Logger.LogWarning(string.Format(Localizer.Localize("warning_failed_swap_unswappable"), EntityUnderDrone()), state);
		}
		return false;
	}

	public bool Harvest()
	{
		FarmObject farmObject = EntityUnderDrone();
		if (farmObject == null)
		{
			return false;
		}
		sim.farm.CollectItems(farmObject.Harvest(out var collectionDuration), GetTransform().GetPosition(), collectionDuration);
		ActionTween();
		sim.mainSim?.PlaySound(farmObject.objectSO.harvestSound, pos);
		return true;
	}

	public bool CanHarvest()
	{
		return EntityUnderDrone()?.Harvestable ?? false;
	}

	public bool IsOver(string objName)
	{
		string text = (sim.farm.grid.entities.ContainsKey(pos) ? EntityUnderDrone().objectSO.objectName : "");
		string objectName = GroundUnderDrone().objectSO.objectName;
		if (!(objName == text))
		{
			return objName == objectName;
		}
		return true;
	}

	public void ChangeGround(string ground)
	{
		if (!(EntityUnderDrone() is HedgePlant))
		{
			sim.farm.grid.RemoveEntity(pos, regrowGrass: false);
		}
		if (IsOver(ground))
		{
			sim.farm.grid.SetGround(pos, "grassland");
			if (!(EntityUnderDrone() is HedgePlant))
			{
				sim.farm.grid.SetEntity(pos, "grass");
			}
		}
		else
		{
			sim.farm.grid.SetGround(pos, ground);
		}
		ActionTween();
		sim.mainSim?.PlaySound(SoundEffectType.Till, pos);
	}

	public bool Plant(FarmObjectSO growable, ProgramState state)
	{
		if (sim.farm.grid.entities.ContainsKey(pos) && !EntityUnderDrone().objectSO.canBeOverplanted)
		{
			return false;
		}
		if (!growable.canBePlanted || !growable.placeableOn.Contains(sim.farm.grid.grounds[pos].objectSO.objectName))
		{
			Logger.LogWarning(string.Format(Localizer.Localize("warning_cant_plant_on_ground"), growable, sim.farm.grid.grounds[pos]), state);
			return false;
		}
		sim.farm.AssertUnlocked(growable.objectName);
		int num = Mathf.Max(0, sim.farm.NumUnlocked(growable.yieldUpgradeName) - 1);
		if (!sim.farm.Items.Remove(growable.cost * Mathf.Max(1, 1 << num)))
		{
			Logger.LogWarning(string.Format(Localizer.Localize("warning_missing_seed"), growable), state);
			return false;
		}
		sim.farm.grid.SetEntity(pos, growable.objectName).OnPlanted(this);
		if (sim.mainSim != null)
		{
			ActionTween();
			sim.mainSim?.PlaySound(SoundEffectType.Plant, pos);
			if (sim.leaderboardType == LeaderboardType.none)
			{
				switch (growable.objectName)
				{
				case "bush":
					Achievements.UnlockAchievement("PLANT_BUSH");
					break;
				case "carrot":
					Achievements.UnlockAchievement("PLANT_CARROTS");
					break;
				case "tree":
					Achievements.UnlockAchievement("PLANT_TREE");
					break;
				case "pumpkin":
					Achievements.UnlockAchievement("PLANT_PUMPKIN");
					break;
				case "sunflower":
					Achievements.UnlockAchievement("PLANT_SUNFLOWER");
					break;
				case "cactus":
					Achievements.UnlockAchievement("PLANT_CACTUS");
					break;
				}
			}
		}
		return true;
	}

	public bool Water(int number)
	{
		sim.farm.grid.SetWaterVolume(pos, sim.farm.grid.waterVolume[pos.x, pos.y] + 0.25 * (double)number);
		if (sim.leaderboardType == LeaderboardType.none && Enumerable.All<double>(Enumerable.Cast<double>((IEnumerable)sim.farm.grid.waterVolume), (Func<double, bool>)((double v) => v >= 0.5)))
		{
			Achievements.UnlockAchievement("MUD_FARM");
		}
		if (sim.mainSim != null && sim.mainSim.TimeFactor == 1.0)
		{
			sim.mainSim.PlayEffect(VFXType.water, GetTransform().GetPosition());
			sim.mainSim?.PlaySound(SoundEffectType.UseWater, pos);
		}
		return true;
	}

	public double GetWater()
	{
		return sim.farm.grid.waterVolume[pos.x, pos.y];
	}

	public bool Fertilize(int number)
	{
		Growable growable = GrowableUnderDrone();
		if (growable == null)
		{
			return false;
		}
		growable.Fertilize(number);
		sim.mainSim?.PlaySound(SoundEffectType.UseFertilizer, pos);
		return true;
	}

	public void ResetPos()
	{
		if (pos != Vector2Int.zero)
		{
			pos = Vector2Int.zero;
			UpdatePosition(200.0);
		}
		ChangeHat(ResourceManager.GetHat("straw_hat"), null);
	}

	public Growable GrowableUnderDrone()
	{
		return EntityUnderDrone() as Growable;
	}

	public Ground GroundUnderDrone()
	{
		return (Ground)sim.farm.grid.grounds[pos];
	}

	public FarmObject EntityUnderDrone()
	{
		return sim.farm.grid.entities.GetValueOrDefault(pos);
	}

	private void UpdatePosition(double ops)
	{
		animDuration = sim.GetActionTime(ops);
		startPosition = endPosition;
		endPosition = GridManager.CellToLocal(pos);
		actionStartTime = sim.CurrentTime;
		droneState = DroneState.moving;
	}

	public (float speed, Vector3 position) GetSpeedAndPos(float realTime)
	{
		Vector3 position = GetTransform().GetPosition();
		if (prevRealTime < 0f)
		{
			prevRealTime = realTime;
			return (speed: 0f, position: position);
		}
		float num = realTime - prevRealTime;
		float item = Vector3.Distance(position, prevSpeedMeasurement) / num;
		prevRealTime = realTime;
		prevSpeedMeasurement = position;
		return (speed: item, position: SoundManager.ZoomAdjustedPosition(position));
	}

	private void ActionTween()
	{
		actionStartTime = sim.CurrentTime;
		droneState = DroneState.action;
		animDuration = sim.GetActionTime(200.0);
	}
}
