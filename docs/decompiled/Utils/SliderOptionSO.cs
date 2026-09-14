using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "ScriptableObjects/SliderOption", order = 1)]
public class SliderOptionSO : OptionSO
{
	public float min;

	public float max = 1f;

	public float stepSize;

	public override OptionValueType ValueType => OptionValueType.Float;
}
