using UnityEngine;
using UnityEngine.UI;

public class CycleOptionUI : OptionUI
{
	[SerializeField]
	private ColoredButton button;

	private CycleOptionSO optionSO;

	public override void Setup(OptionSO optionSO)
	{
		base.Setup(optionSO);
		if (!(optionSO is CycleOptionSO))
		{
			Debug.LogError(optionName + "s UI isn't of the right type");
		}
		this.optionSO = (CycleOptionSO)optionSO;
		UpdateValue();
		button.GetComponent<Button>().onClick.AddListener(Clicked);
	}

	public override void UpdateValue()
	{
		button.Text = OptionHolder.GetString(optionSO.optionName);
	}

	public void Clicked()
	{
		string @string = OptionHolder.GetString(optionSO.optionName);
		int b = optionSO.options.IndexOf(@string);
		b = Mathf.Max(0, b);
		int index = (b + 1) % optionSO.options.Count;
		string value = optionSO.options[index];
		OptionHolder.SetOption(optionSO.optionName, value);
	}
}
