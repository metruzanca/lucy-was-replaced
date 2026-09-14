using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SliderOptionUI : OptionUI
{
	[SerializeField]
	private Slider slider;

	[SerializeField]
	private Image sliderBackground;

	[SerializeField]
	private TMP_InputField inputField;

	[SerializeField]
	private bool changeOnlyOnRelease;

	private SliderOptionSO optionSO;

	private float prevValue;

	private string formatString;

	public override void Setup(OptionSO optionSO)
	{
		base.Setup(optionSO);
		if (!(optionSO is SliderOptionSO))
		{
			Debug.LogError(optionName + "s UI isn't of the right type");
		}
		this.optionSO = (SliderOptionSO)optionSO;
		slider.minValue = this.optionSO.min;
		slider.maxValue = this.optionSO.max;
		formatString = "N" + Mathf.Log10(1f / this.optionSO.stepSize);
		UpdateValue();
		slider.onValueChanged.AddListener(OnSliderValueChanged);
		((UnityEvent<string>)(object)inputField.onEndEdit).AddListener((UnityAction<string>)OnInputFieldEndEdit);
	}

	public override void UpdateValue()
	{
		float @float = OptionHolder.GetFloat(optionName);
		if (slider.value != @float)
		{
			slider.value = @float;
		}
		inputField.text = @float.ToString(formatString, CultureInfo.InvariantCulture);
		prevValue = slider.value;
	}

	private void OnSliderValueChanged(float v)
	{
		if (!changeOnlyOnRelease)
		{
			ChangeValue(slider.value);
		}
	}

	public void OnSliderReleased()
	{
		if (changeOnlyOnRelease)
		{
			ChangeValue(slider.value);
		}
	}

	private void OnInputFieldEndEdit(string s)
	{
		if (float.TryParse(inputField.text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result))
		{
			ChangeValue(result);
		}
		else
		{
			inputField.text = prevValue.ToString(formatString, CultureInfo.InvariantCulture);
		}
	}

	private void ChangeValue(float newValue)
	{
		float num = Mathf.Clamp(Mathf.Round(newValue / optionSO.stepSize) * optionSO.stepSize, optionSO.min, optionSO.max);
		if (prevValue != num)
		{
			OptionHolder.SetOption(optionName, num);
		}
		prevValue = num;
	}

	protected override void OnThemeChanged(ColorTheme theme)
	{
		base.OnThemeChanged(theme);
		theme.ui.text.ApplyTo(inputField);
		slider.colors = theme.ui.slider.ToColorBlock();
		sliderBackground.color = theme.ui.SliderBackgroundColor;
		if (slider.fillRect.TryGetComponent<Image>(out var component))
		{
			component.color = theme.ui.slider.NormalColor;
		}
	}
}
