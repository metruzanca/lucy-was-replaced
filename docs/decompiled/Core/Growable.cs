using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class Growable : FarmObject
{
	private bool isGrown;

	private Duration growTime;

	private double prevGrowTimeFactor = 1.0;

	private Duration growStart;

	private double grownPercent;

	private float randomFactor;

	public bool canBePlanted = true;

	public List<string> growsOn;

	private FarmObjectSO companionType;

	private Vector2Int companionPos;

	public bool weird;

	protected int gooIndex;

	private Simulation.Timer growTimer;

	public Duration GrowTime => growTime;

	public double GrownPercent
	{
		get
		{
			if (!isGrown)
			{
				return grownPercent + (sim.CurrentTime - growStart) / growTime;
			}
			return 1.0;
		}
	}

	public override bool Harvestable => isGrown;

	public float MeanGrowTime => objectSO.meanGrowTime;

	protected virtual double GrowTimeFactor
	{
		get
		{
			double num = 1.0;
			if (sim.farm.grid.grounds[pos] is Ground)
			{
				num += sim.farm.grid.waterVolume[pos.x, pos.y] * 4.0;
			}
			return 1.0 / num;
		}
	}

	protected override double YieldFactor => base.YieldFactor * (double)((!HasCompanion()) ? 1 : (5 << sim.farm.NumUnlocked("polyculture")));

	public override IEnumerable<(Mesh, Matrix4x4)> GetMeshes()
	{
		foreach (var mesh in base.GetMeshes())
		{
			yield return mesh;
		}
		if (weird)
		{
			yield return (objectSO.gooMeshes[gooIndex], Matrix4x4.Translate(GridManager.CellToLocal(pos)));
		}
	}

	public void Grow()
	{
		isGrown = true;
		scaleEnd = Vector3.one;
		OnFullyGrown();
		UpdateNeighbors();
	}

	public override void UpdateFarmObject()
	{
		base.UpdateFarmObject();
		if (prevGrowTimeFactor != GrowTimeFactor)
		{
			TryScheduleGrow();
		}
	}

	private void TryScheduleGrow()
	{
		if (isGrown)
		{
			return;
		}
		StopGrowing();
		growTime = GetGrowTime();
		growStart = sim.CurrentTime;
		Duration duration = growTime * (1.0 - grownPercent);
		if (duration.nanoseconds == 0L)
		{
			Grow();
			return;
		}
		growTimer = sim.StartTimer(Grow, duration);
		float num = (float)grownPercent * 0.9f + 0.1f;
		if (objectSO.verticalGrowth)
		{
			scaleStart = new Vector3(1f, 1f, num);
		}
		else
		{
			scaleStart = Vector3.one * num;
		}
		scaleEnd = Vector3.one;
		scaleDuration = duration;
		scaleEndTime = sim.CurrentTime + duration;
	}

	private Duration GetGrowTime()
	{
		prevGrowTimeFactor = GrowTimeFactor;
		return Duration.FromSeconds(prevGrowTimeFactor * (double)objectSO.meanGrowTime * (double)randomFactor);
	}

	protected virtual void OnFullyGrown()
	{
	}

	public void StopGrowing()
	{
		if (!isGrown)
		{
			if (growTimer != null)
			{
				growTimer.stopped = true;
			}
			grownPercent += (sim.CurrentTime - growStart) / growTime;
		}
	}

	public override ItemBlock GetYield()
	{
		ItemBlock itemBlock = ItemBlock.CreateEmpty();
		if (objectSO.dropAmount <= 0.0)
		{
			return itemBlock;
		}
		double num = objectSO.dropAmount * YieldFactor;
		if (weird)
		{
			num *= 0.5;
			itemBlock.AddItem(StringIds.WeirdSubstanceItemId, Math.Truncate(num));
			num = Math.Ceiling(num);
		}
		itemBlock.AddItem(objectSO.dropItemId, num);
		return itemBlock;
	}

	public override ItemBlock Harvest(out Duration collectionDuration)
	{
		base.Harvest(out collectionDuration);
		ItemBlock result = (Harvestable ? GetYield() : ItemBlock.CreateEmpty());
		sim.farm.grid.RemoveEntity(pos);
		HarvestEffects(HasCompanion());
		return result;
	}

	public void Fertilize(int number)
	{
		weird = true;
		if (!isGrown)
		{
			StopGrowing();
			grownPercent = Math.Min(grownPercent + Duration.FromSeconds(2 * number) / GetGrowTime(), 1.0);
			TryScheduleGrow();
		}
	}

	public void ToggleWeird()
	{
		weird = !weird;
		gooIndex = sim.randomVarious.Next(objectSO.gooMeshes.Count - 1);
		bool flag = !weird;
		FarmObject[] neighbors = sim.farm.grid.GetNeighbors(pos);
		for (int i = 0; i < neighbors.Length; i++)
		{
			if (neighbors[i] is Growable growable)
			{
				growable.weird = !growable.weird;
				growable.gooIndex = sim.randomVarious.Next(growable.objectSO.gooMeshes.Count - 1);
				flag = flag || !growable.weird;
			}
		}
		if (sim.leaderboardType == LeaderboardType.none && flag)
		{
			Achievements.UnlockAchievement("HEALER");
		}
	}

	private bool HasCompanion()
	{
		if (!objectSO.canHaveCompanion || !sim.farm.grid.entities.ContainsKey(companionPos))
		{
			return false;
		}
		if (sim.farm.grid.entities[companionPos].objectSO.objectName == companionType.objectName)
		{
			return true;
		}
		return false;
	}

	public IPyObject GetCompanion()
	{
		if (!objectSO.canHaveCompanion)
		{
			return new PyNone();
		}
		return new PyTuple(new List<IPyObject>
		{
			companionType,
			new PyTuple(new List<IPyObject>
			{
				new PyNumber(companionPos.x),
				new PyNumber(companionPos.y)
			})
		});
	}

	private void ChooseCompanion()
	{
		int num = 3;
		do
		{
			companionPos = new Vector2Int(sim.randomPoly.Next(-num, num + 1), sim.randomPoly.Next(-num, num + 1));
		}
		while ((sim.farm.grid.Wrap(pos + companionPos) == pos || Mathf.Abs(companionPos.x) + Mathf.Abs(companionPos.y) > num) && sim.farm.grid.WorldSize.y != 1);
		companionPos = sim.farm.grid.Wrap(pos + companionPos);
		do
		{
			companionType = sim.randomPoly.Next(4) switch
			{
				2 => ResourceManager.GetFarmObject("carrot"), 
				1 => ResourceManager.GetFarmObject("bush"), 
				0 => ResourceManager.GetFarmObject("grass"), 
				_ => ResourceManager.GetFarmObject("tree"), 
			};
		}
		while (companionType.objectName == objectSO.objectName);
	}

	public override void OnFree()
	{
		base.OnFree();
		StopGrowing();
	}

	public override void OnRestart()
	{
		base.OnRestart();
		gooIndex = sim.randomVarious.Next(objectSO.gooMeshes.Count - 1);
		if (objectSO.canHaveCompanion)
		{
			ChooseCompanion();
		}
		isGrown = false;
		growStart = sim.CurrentTime;
		grownPercent = 0.0;
		randomFactor = (float)sim.randomVarious.NextDouble() * objectSO.growTimeDeviationPercent * 2f + 1f - objectSO.growTimeDeviationPercent;
		growTime = GetGrowTime();
		weird = false;
		TryScheduleGrow();
	}

	public override void SetValues(Dictionary<string, string> values)
	{
		base.SetValues(values);
		StopGrowing();
		if (values.ContainsKey("grown percent") && double.TryParse(values["grown percent"], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result))
		{
			grownPercent = result;
		}
		TryScheduleGrow();
	}

	public override string ToSerializeString()
	{
		return $"{base.ToSerializeString()},grown percent = {grownPercent + (sim.CurrentTime - growStart) / growTime}";
	}
}
