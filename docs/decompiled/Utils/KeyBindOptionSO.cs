using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "ScriptableObjects/KeyBindOption", order = 1)]
public class KeyBindOptionSO : OptionSO
{
	public bool canBeMouseButton;

	public override OptionValueType ValueType => OptionValueType.KeyCombination;
}
