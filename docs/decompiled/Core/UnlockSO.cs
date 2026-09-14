using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Unlock", menuName = "ScriptableObjects/Unlock", order = 1)]
public class UnlockSO : ScriptableObject, IPyObject
{
	public string unlockName;

	public string description;

	public string docs;

	public Mesh mesh;

	public string displayCode;

	public List<string> unlocks;

	public string parentUnlock;

	public string unlockedHat;

	public int order;

	public ItemBlock unlockCost;

	public List<ItemBlock> multiUnlockCost;

	public float multiUnlockFactor = 2f;

	public string multiUnlockDescr;

	public MultiUnlockDescrMode multiUnlockDescrMode;

	public float additivePercentStart;

	public float additivePercentFactor = 2f;

	public int maxUnlockLevel = 1;

	public bool enabled = true;

	public bool IsMultiUnlock => maxUnlockLevel > 1;

	public override string ToString()
	{
		return "Unlocks." + CodeUtilities.ToUpperSnake(unlockName);
	}

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		return this;
	}
}
