using System.Collections.Generic;
using UnityEngine;

public class FarmRenderer : MonoBehaviour
{
	[SerializeField]
	private Material material;

	[SerializeField]
	private Material propellerMaterial;

	[SerializeField]
	private Material golddenHatMaterial;

	[Header("Drone")]
	[SerializeField]
	private Mesh droneMesh;

	[SerializeField]
	private Mesh propellerMesh;

	[SerializeField]
	private Mesh hoverMesh;

	[SerializeField]
	private Mesh droneHighlightArrowMesh;

	[SerializeField]
	private Vector3 propellerOffset1;

	[SerializeField]
	private Vector3 propellerOffset2;

	[SerializeField]
	private Vector3 propellerOffset3;

	[SerializeField]
	private Vector3 propellerOffset4;

	[SerializeField]
	private bool renderDrones = true;

	[SerializeField]
	private bool renderFarm = true;

	private Dictionary<Mesh, List<Matrix4x4>> farmMeshes = new Dictionary<Mesh, List<Matrix4x4>>();

	private Dictionary<Mesh, List<Matrix4x4>> droneMeshes = new Dictionary<Mesh, List<Matrix4x4>>();

	private Dictionary<Mesh, List<Matrix4x4>> goldenHatMeshes = new Dictionary<Mesh, List<Matrix4x4>>();

	private List<Matrix4x4> propellerMeshes = new List<Matrix4x4>();

	private void Update()
	{
		Transform sceneScaler = MainSim.Inst.sceneScaler;
		foreach (Mesh key in farmMeshes.Keys)
		{
			farmMeshes[key].Clear();
		}
		foreach (Mesh key2 in droneMeshes.Keys)
		{
			droneMeshes[key2].Clear();
		}
		foreach (Mesh key3 in goldenHatMeshes.Keys)
		{
			goldenHatMeshes[key3].Clear();
		}
		propellerMeshes.Clear();
		if (!droneMeshes.ContainsKey(droneMesh))
		{
			droneMeshes[droneMesh] = new List<Matrix4x4>();
		}
		foreach (var droneMesh in MainSim.Inst.GetDroneMeshes())
		{
			Matrix4x4 matrix4x = sceneScaler.localToWorldMatrix * droneMesh.Item3;
			if (!IsMatrixValid(matrix4x))
			{
				continue;
			}
			Quaternion q = Quaternion.Euler(0f, 180f, 0f);
			droneMeshes[this.droneMesh].Add(matrix4x);
			if (droneMesh.Item1.isGolden)
			{
				if (!goldenHatMeshes.ContainsKey(droneMesh.Item1.hatMesh))
				{
					goldenHatMeshes[droneMesh.Item1.hatMesh] = new List<Matrix4x4>();
				}
				goldenHatMeshes[droneMesh.Item1.hatMesh].Add(matrix4x);
			}
			else
			{
				if (!droneMeshes.ContainsKey(droneMesh.Item1.hatMesh))
				{
					droneMeshes[droneMesh.Item1.hatMesh] = new List<Matrix4x4>();
				}
				droneMeshes[droneMesh.Item1.hatMesh].Add(matrix4x);
			}
			if (droneMesh.highlight)
			{
				droneMeshes[droneHighlightArrowMesh] = new List<Matrix4x4> { matrix4x };
			}
			Matrix4x4 item = matrix4x * Matrix4x4.Translate(propellerOffset1) * Matrix4x4.Rotate(q);
			propellerMeshes.Add(item);
			Matrix4x4 item2 = matrix4x * Matrix4x4.Translate(propellerOffset2);
			propellerMeshes.Add(item2);
			Matrix4x4 item3 = matrix4x * Matrix4x4.Translate(propellerOffset3) * Matrix4x4.Rotate(q);
			propellerMeshes.Add(item3);
			Matrix4x4 item4 = matrix4x * Matrix4x4.Translate(propellerOffset4);
			propellerMeshes.Add(item4);
		}
		foreach (var farmMesh in MainSim.Inst.GetFarmMeshes())
		{
			if (!farmMeshes.ContainsKey(farmMesh.Item1))
			{
				farmMeshes[farmMesh.Item1] = new List<Matrix4x4>();
			}
			farmMeshes[farmMesh.Item1].Add(sceneScaler.localToWorldMatrix * farmMesh.Item2);
		}
		if (renderFarm)
		{
			foreach (Mesh key4 in farmMeshes.Keys)
			{
				for (int i = 0; i < farmMeshes[key4].Count; i += 1023)
				{
					int count = Mathf.Min(1023, farmMeshes[key4].Count - i);
					Graphics.DrawMeshInstanced(key4, 0, material, farmMeshes[key4].GetRange(i, count));
				}
			}
		}
		if (renderDrones)
		{
			foreach (Mesh key5 in droneMeshes.Keys)
			{
				for (int j = 0; j < droneMeshes[key5].Count; j += 1023)
				{
					int count2 = Mathf.Min(1023, droneMeshes[key5].Count - j);
					Graphics.DrawMeshInstanced(key5, 0, material, droneMeshes[key5].GetRange(j, count2));
				}
			}
			foreach (Mesh key6 in goldenHatMeshes.Keys)
			{
				for (int k = 0; k < goldenHatMeshes[key6].Count; k += 1023)
				{
					int count3 = Mathf.Min(1023, goldenHatMeshes[key6].Count - k);
					Graphics.DrawMeshInstanced(key6, 0, golddenHatMaterial, goldenHatMeshes[key6].GetRange(k, count3));
				}
			}
			for (int l = 0; l < propellerMeshes.Count; l += 1023)
			{
				int count4 = Mathf.Min(1023, propellerMeshes.Count - l);
				Graphics.DrawMeshInstanced(propellerMesh, 0, propellerMaterial, propellerMeshes.GetRange(l, count4));
			}
		}
		if (MainSim.Inst.hoveredCell.x >= 0 && MainSim.Inst.hoveredCell.y >= 0)
		{
			Graphics.DrawMesh(hoverMesh, sceneScaler.localToWorldMatrix * Matrix4x4.Translate(new Vector3(-MainSim.Inst.hoveredCell.x, MainSim.Inst.hoveredCell.y, 0f)), material, 0);
		}
	}

	private bool IsMatrixValid(Matrix4x4 m)
	{
		for (int i = 0; i < 16; i++)
		{
			float f = m[i];
			if (float.IsNaN(f) || float.IsInfinity(f))
			{
				return false;
			}
		}
		return true;
	}
}
