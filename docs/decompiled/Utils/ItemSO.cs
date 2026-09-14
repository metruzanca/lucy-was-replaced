using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "ScriptableObjects/Item", order = 1)]
public class ItemSO : ScriptableObject, IPyObject
{
	public string itemName;

	public string description;

	public string docs;

	public Mesh mesh;

	public bool trackStats;

	public bool enabled = true;

	public float priority;

	[NonSerialized]
	public int itemId;

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		return this;
	}

	public override string ToString()
	{
		return "Items." + CodeUtilities.ToUpperSnake(itemName);
	}
}
