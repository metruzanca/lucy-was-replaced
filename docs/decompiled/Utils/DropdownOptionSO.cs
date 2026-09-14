using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "ScriptableObjects/DropdownOption", order = 1)]
public class DropdownOptionSO : OptionSO
{
	public List<string> options;
}
