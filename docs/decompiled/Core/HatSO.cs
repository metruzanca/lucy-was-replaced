using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Hat", menuName = "ScriptableObjects/Hat", order = 3)]
public class HatSO : ScriptableObject, IPyObject
{
	public string className;

	public string hatName;

	public Mesh hatMesh;

	public bool isGolden;

	public bool preventWrapping;

	public bool rotateDroneToMove;

	public float droneFlyHeight;

	public bool hidden;

	public AudioClip sound1;

	public AudioClip sound2;

	public override string ToString()
	{
		return "Hats." + CodeUtilities.ToUpperSnake(hatName);
	}

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		return this;
	}
}
