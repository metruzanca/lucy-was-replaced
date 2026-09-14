using System;
using System.Collections.Generic;
using UnityEngine;

public class FarmObject
{
	public FarmObjectSO objectSO;

	public Vector2Int pos;

	private GridDirection orientation;

	protected Quaternion rotationStart;

	protected Quaternion rotationEnd = Quaternion.identity;

	protected Duration rotationDuration;

	protected Duration rotationEndTime;

	protected Vector3 positionStart;

	protected Vector3 positionEnd = Vector3.zero;

	protected Duration moveDuration;

	protected Duration moveEndTime;

	protected Vector3 scaleStart;

	protected Vector3 scaleEnd = Vector3.one;

	protected Duration scaleDuration;

	protected Duration scaleEndTime;

	[NonSerialized]
	public Simulation sim;

	public GridDirection Orientation
	{
		get
		{
			return orientation;
		}
		set
		{
			orientation = value;
			rotationEnd = orientation.GetQuaternion();
		}
	}

	public virtual bool Harvestable => true;

	public Vector3 LocalPosition
	{
		get
		{
			if (moveEndTime > sim.CurrentTime)
			{
				float t = 1f - (float)((moveEndTime - sim.CurrentTime) / moveDuration);
				return Vector3.Lerp(positionStart, positionEnd, t);
			}
			return positionEnd;
		}
		set
		{
			positionEnd = value;
		}
	}

	protected virtual double YieldFactor => Math.Max(1 << sim.farm.NumUnlocked(objectSO.yieldUpgradeName) - 1, 1) * MainSim.Inst.harvestFactor;

	public void AnimateMove(Vector3 destination, Duration duration)
	{
		positionStart = LocalPosition;
		positionEnd = destination;
		moveDuration = duration;
		moveEndTime = sim.CurrentTime + duration;
	}

	public void AnimateRotation(GridDirection destination, Duration duration)
	{
		rotationStart = rotationEnd;
		Orientation = destination;
		rotationDuration = duration;
		rotationEndTime = sim.CurrentTime + duration;
	}

	public Matrix4x4 GetTransform()
	{
		Vector3 vector = positionEnd;
		Vector3 s = scaleEnd;
		if (moveEndTime > sim.CurrentTime)
		{
			float t = 1f - (float)((moveEndTime - sim.CurrentTime) / moveDuration);
			vector = Vector3.Lerp(positionStart, positionEnd, t);
		}
		if (scaleEndTime > sim.CurrentTime)
		{
			float t2 = 1f - (float)((scaleEndTime - sim.CurrentTime) / scaleDuration);
			s = Vector3.Lerp(scaleStart, scaleEnd, t2);
		}
		Quaternion q;
		if (rotationEndTime > sim.CurrentTime)
		{
			float t3 = 1f - (float)((rotationEndTime - sim.CurrentTime) / rotationDuration);
			q = Quaternion.Slerp(rotationStart, rotationEnd, t3);
		}
		else
		{
			q = rotationEnd;
		}
		if (s.x == 0f || s.y == 0f || s.z == 0f)
		{
			s = Vector3.zero;
		}
		return Matrix4x4.TRS(vector, q, s);
	}

	public virtual IEnumerable<(Mesh, Matrix4x4)> GetMeshes()
	{
		yield return (objectSO.meshes[0], GetTransform());
	}

	public virtual ItemBlock GetYield()
	{
		return null;
	}

	public virtual ItemBlock Harvest(out Duration collectionDuration)
	{
		collectionDuration = Duration.FromSeconds(0.0);
		return null;
	}

	public virtual void OnFree()
	{
	}

	public virtual void OnRestart()
	{
		if (objectSO.randomRotation)
		{
			Orientation = GridDirectionMethods.RandomGridDirection(sim.randomVarious);
		}
		else
		{
			Orientation = GridDirection.South;
		}
	}

	public virtual void OnPlanted(Drone drone)
	{
	}

	public virtual void UpdateFarmObject()
	{
	}

	public virtual IPyObject Measure()
	{
		return null;
	}

	public void UpdateNeighbors()
	{
		FarmObject[] neighbors = sim.farm.grid.GetNeighbors(pos);
		for (int i = 0; i < neighbors.Length; i++)
		{
			neighbors[i]?.UpdateFarmObject();
		}
	}

	public virtual bool CanDroneMoveToFrom(GridDirection direction)
	{
		return true;
	}

	public virtual bool CanDroneMoveAwayTo(GridDirection direction)
	{
		return true;
	}

	public virtual string ToSerializeString()
	{
		return "type = " + objectSO.objectName;
	}

	public virtual void SetValues(Dictionary<string, string> values)
	{
	}

	public virtual void OnSwapped()
	{
	}

	public override string ToString()
	{
		return "Entities." + CodeUtilities.ToUpperSnake(objectSO.objectName);
	}

	public void HarvestEffects(bool addStars = false)
	{
		if (sim.mainSim != null && sim.mainSim.TimeFactor == 1.0)
		{
			Vector3 localPosition = LocalPosition;
			sim.mainSim.PlayEffect(VFXType.dust, localPosition);
			sim.mainSim.PlayEffect(VFXType.hit, localPosition, changeColor: true, objectSO.color);
			if (addStars)
			{
				sim.mainSim.PlayEffect(VFXType.stars, localPosition);
			}
		}
	}
}
