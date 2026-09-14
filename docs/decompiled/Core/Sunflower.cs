using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Sunflower : Growable
{
	private int number;

	private bool getBoost;

	protected override double YieldFactor
	{
		get
		{
			Enumerable.Where<FarmObject>((IEnumerable<FarmObject>)sim.farm.grid.entities.Values, (Func<FarmObject, bool>)((FarmObject f) => f is Sunflower));
			if (getBoost)
			{
				return 8.0 * base.YieldFactor;
			}
			return base.YieldFactor;
		}
	}

	public override ItemBlock Harvest(out Duration collectionDuration)
	{
		IEnumerable<FarmObject> enumerable = Enumerable.Where<FarmObject>((IEnumerable<FarmObject>)sim.farm.grid.entities.Values, (Func<FarmObject, bool>)((FarmObject f) => f is Sunflower));
		getBoost = number == Enumerable.Max<FarmObject>(enumerable, (Func<FarmObject, int>)((FarmObject s) => ((Sunflower)s).number)) && Enumerable.Count<FarmObject>(enumerable) >= 10;
		bool hadIncorrectSunflowerHarvest = sim.farm.grid.hadIncorrectSunflowerHarvest;
		sim.farm.grid.hadIncorrectSunflowerHarvest = !getBoost;
		if (hadIncorrectSunflowerHarvest)
		{
			getBoost = false;
		}
		if (getBoost)
		{
			sim.mainSim.PlayEffect(VFXType.stars, base.LocalPosition);
		}
		return base.Harvest(out collectionDuration);
	}

	public override ItemBlock GetYield()
	{
		IEnumerable<FarmObject> enumerable = Enumerable.Where<FarmObject>((IEnumerable<FarmObject>)sim.farm.grid.entities.Values, (Func<FarmObject, bool>)((FarmObject f) => f is Sunflower));
		getBoost = number == Enumerable.Max<FarmObject>(enumerable, (Func<FarmObject, int>)((FarmObject s) => ((Sunflower)s).number)) && Enumerable.Count<FarmObject>(enumerable) >= 10;
		return base.GetYield();
	}

	public override IEnumerable<(Mesh, Matrix4x4)> GetMeshes()
	{
		Matrix4x4 transform = GetTransform();
		yield return (objectSO.meshes[number - 7], transform);
		if (weird)
		{
			yield return (objectSO.gooMeshes[gooIndex], Matrix4x4.Translate(GridManager.CellToLocal(pos)));
		}
	}

	public override void OnFree()
	{
		base.OnFree();
	}

	public override void OnRestart()
	{
		base.OnRestart();
		number = sim.randomSunflower.Next(7, 16);
	}

	public override IPyObject Measure()
	{
		return new PyNumber(number);
	}
}
