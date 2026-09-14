using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DinosaurHat : Hat
{
	private LinkedList<Dinosaur> tail = new LinkedList<Dinosaur>();

	public Apple target;

	private int ops = 400;

	private Duration equipTime;

	public override void OnEquip(Drone drone, ProgramState programState)
	{
		base.OnEquip(drone, programState);
		equipTime = base.sim.CurrentTime;
		if (base.sim.leaderboardType == LeaderboardType.none)
		{
			Achievements.UnlockAchievement("EQUIP_DINO_HAT");
		}
		if (!base.sim.farm.grid.entities.ContainsKey(drone.pos) || drone.EntityUnderDrone().objectSO.canBeOverplanted)
		{
			if (Enumerable.Count<Drone>(Enumerable.Where<Drone>((IEnumerable<Drone>)base.sim.farm.drones, (Func<Drone, bool>)((Drone d) => d?.hat.hatSO.hatName == "dinosaur_hat"))) > 1)
			{
				drone.hat = Hat.CreateHat(ResourceManager.GetHat("straw_hat"), base.sim, drone);
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_dino_hat_already_used"));
			}
			int num = Mathf.Max(0, base.sim.farm.NumUnlocked("dinosaurs") - 1);
			if (base.sim.farm.Items.Remove(ResourceManager.GetFarmObject("apple").cost * Mathf.Max(1, 1 << num)))
			{
				target = (Apple)base.sim.farm.grid.SetEntity(drone.pos, "apple");
				target.ChooseTarget();
			}
			else
			{
				Logger.LogWarning(CodeUtilities.LocalizeAndFormat("warning_missing_seed", ResourceManager.GetFarmObject("apple")), programState);
			}
		}
	}

	public override void OnUnequip()
	{
		base.OnUnequip();
		if (tail.Count > 0)
		{
			base.sim.mainSim?.PlaySound(SoundEffectType.DinosaurDie, base.drone.pos);
			base.sim.farm.CollectItem(StringIds.BoneItemId, BoneCount(), tail.First.Value.LocalPosition, base.sim.CurrentTime - equipTime);
		}
		foreach (Dinosaur item in tail)
		{
			item.HarvestEffects();
			item.isInSnake = false;
			base.sim.farm.grid.RemoveEntity(item.pos);
		}
		if (target != null)
		{
			base.sim.farm.grid.RemoveEntity(target.pos);
		}
		tail.Clear();
	}

	public double BoneCount()
	{
		int num = Enumerable.Count<KeyValuePair<Vector2Int, FarmObject>>((IEnumerable<KeyValuePair<Vector2Int, FarmObject>>)base.sim.farm.grid.entities, (Func<KeyValuePair<Vector2Int, FarmObject>, bool>)((KeyValuePair<Vector2Int, FarmObject> e) => e.Value is Dinosaur));
		int num2 = Mathf.Max(0, base.sim.farm.NumUnlocked("dinosaurs") - 1);
		return Math.Floor(Math.Pow(num, 2.0)) * (double)(1 << num2);
	}

	public override int OnMove(Vector2Int oldPos, Vector2Int newPos, ProgramState programState)
	{
		base.OnMove(oldPos, newPos, programState);
		bool flag = false;
		int num = base.sim.farm.grid.WorldSize.x * base.sim.farm.grid.WorldSize.y - 2;
		Apple apple = target;
		if (apple != null && apple.pos == oldPos && base.sim.farm.grid.entities.GetValueOrDefault(oldPos) == target)
		{
			int num2 = Mathf.Max(0, base.sim.farm.NumUnlocked("dinosaurs") - 1);
			flag = true;
			if (tail.Count < num && (!base.sim.farm.grid.entities.TryGetValue(target.nextPos, out var value) || value.objectSO.canBeOverplanted) && base.sim.farm.Items.Remove(target.objectSO.cost * (1 << num2)))
			{
				base.sim.mainSim?.PlaySound(SoundEffectType.DinosaurEatApple, newPos);
				base.sim.mainSim?.PlayEffect(VFXType.hit, target.LocalPosition, changeColor: true, target.objectSO.color);
				target = (Apple)base.sim.farm.grid.SetEntity(target.nextPos, "apple");
				target.ChooseTarget();
				ops -= (int)Math.Floor((double)ops * 0.03);
			}
			else
			{
				if (!base.sim.farm.Items.Contains(target.objectSO.cost * (1 << num2)))
				{
					Logger.LogWarning(CodeUtilities.LocalizeAndFormat("warning_missing_seed", ResourceManager.GetFarmObject("apple")), programState);
				}
				target = null;
			}
		}
		if (flag || tail.Count > 0)
		{
			Dinosaur dinosaur = (Dinosaur)base.sim.farm.grid.SetEntity(oldPos, "dinosaur");
			dinosaur.drone = base.drone;
			if (tail.Count > 0)
			{
				dinosaur.Orientation = GridDirectionMethods.FromVector(oldPos - tail.First.Value.pos);
				if (!flag)
				{
					tail.Last.Value.isInSnake = false;
					base.sim.farm.grid.RemoveEntity(tail.Last.Value.pos);
					tail.RemoveLast();
				}
			}
			else
			{
				dinosaur.Orientation = GridDirectionMethods.FromVector(newPos - oldPos);
			}
			tail.AddFirst(dinosaur);
			if (tail.Count == num && base.sim.leaderboardType == LeaderboardType.none)
			{
				Achievements.UnlockAchievement("LONG_DINOSAUR");
			}
			else if (tail.Count >= 1000 && base.sim.leaderboardType == LeaderboardType.none)
			{
				Achievements.UnlockAchievement("SIZE_MATTERS");
			}
			if (tail.Count < num && tail.Count > 1 && base.sim.farm.grid.entities.GetValueOrDefault(newPos) != target)
			{
				tail.Last.Value.canMoveTo = true;
			}
			if (!flag && tail.Count > 1)
			{
				tail.Last.Value.LastTailAnim(ops);
				tail.First.Value.FirstTailAnim(ops);
			}
			else if (tail.Count == 1)
			{
				tail.First.Value.DoubleTailAnim();
			}
			else
			{
				tail.First.Value.FirstTailAnim(ops);
			}
		}
		while (true)
		{
			Apple apple2 = target;
			if (apple2 == null || !(apple2.nextPos == oldPos) || tail.Count >= num)
			{
				break;
			}
			target.ChooseTarget();
		}
		return ops;
	}
}
