using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "ScriptableObjects/CycleOption", order = 1)]
public class CycleOptionSO : OptionSO
{
	public List<string> options;
}
