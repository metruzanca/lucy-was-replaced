using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Trade", menuName = "ScriptableObjects/Trade", order = 1)]
public class TradeSO : ScriptableObject, IPyObject
{
	public string itemName;

	public string descr;

	public ItemBlock input;

	public string unlockToScaleWith;

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		return this;
	}
}
