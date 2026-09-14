using System;
using UnityEngine;

public class OptionSO : ScriptableObject
{
	public OptionUI optionUI;

	public string optionName;

	public string tooltip;

	public string defaultValue;

	public string category = "gameplay";

	public float importance;

	public virtual OptionValueType ValueType => OptionValueType.String;

	public event Action<OptionSO> OnOptionChanged;

	public void TriggerOptionChanged()
	{
		this.OnOptionChanged?.Invoke(this);
	}
}
