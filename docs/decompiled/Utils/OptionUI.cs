using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionUI : MonoBehaviour, ITooltipHandler
{
	[SerializeField]
	private TextMeshProUGUI title;

	protected string optionName;

	private OptionSO option;

	public virtual void Setup(OptionSO optionSO)
	{
		optionName = optionSO.optionName;
		((TMP_Text)title).text = optionName;
		option = optionSO;
	}

	public virtual void UpdateValue()
	{
	}

	public TooltipInfo GetTooltipInfo(Action updateTooltipCallback)
	{
		if (string.IsNullOrEmpty(option.tooltip))
		{
			return null;
		}
		return new TooltipInfo(Localizer.Localize(option.tooltip), 0.3f);
	}

	public void TooltipGone()
	{
	}

	private void OnEnable()
	{
		ThemeManager.Inst.OnThemeChanged += OnThemeChanged;
	}

	private void OnDisable()
	{
		ThemeManager.Inst.OnThemeChanged -= OnThemeChanged;
	}

	protected virtual void OnThemeChanged(ColorTheme theme)
	{
		if (TryGetComponent<Image>(out var component))
		{
			component.color = theme.ui.OptionBackgroundColor;
		}
		((Graphic)(object)title).color = theme.ui.OptionTextColor;
	}
}
