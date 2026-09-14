using System;
using System.Collections.Generic;
using UnityEngine;

public class Soil : Ground
{
	public override IEnumerable<(Mesh, Matrix4x4)> GetMeshes()
	{
		double num = sim.farm.grid.waterVolume[pos.x, pos.y];
		int index = ((num != 1.0) ? ((int)Math.Floor(num * (double)objectSO.meshes.Count)) : (objectSO.meshes.Count - 1));
		yield return (objectSO.meshes[index], GetTransform());
	}
}
