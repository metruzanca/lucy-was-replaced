using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DropdownOptionUI : OptionUI
{
	[SerializeField]
	private TMP_Dropdown dropdown;

	[SerializeField]
	private Image dropdownArrow;

	[SerializeField]
	private Image dropdownBackground;

	[SerializeField]
	private TMP_Text dropdownItemText;

	[SerializeField]
	private Image dropdownItemCheckmark;

	private DropdownOptionSO optionSO;

	private bool isRefreshingOptions;

	public override void Setup(OptionSO optionSO)
	{
		base.Setup(optionSO);
		if (!(optionSO is DropdownOptionSO dropdownOptionSO))
		{
			Debug.LogError(optionName + "s UI isn't of the right type");
			return;
		}
		this.optionSO = dropdownOptionSO;
		RefreshOptions(optionSO);
		((UnityEvent<int>)(object)dropdown.onValueChanged).AddListener((UnityAction<int>)delegate
		{
			Changed();
		});
		optionSO.OnOptionChanged += RefreshOptions;
	}

	private void RefreshOptions(OptionSO optionSO)
	{
		isRefreshingOptions = true;
		dropdown.ClearOptions();
		dropdown.AddOptions(this.optionSO.options);
		UpdateValue();
		isRefreshingOptions = false;
	}

	public override void UpdateValue()
	{
		dropdown.value = optionSO.options.IndexOf(OptionHolder.GetString(optionSO.optionName));
	}

	public void Changed()
	{
		if (!isRefreshingOptions)
		{
			string value = optionSO.options[dropdown.value];
			if (string.IsNullOrEmpty(value))
			{
				string @string = OptionHolder.GetString(optionSO.optionName);
				int b = optionSO.options.IndexOf(@string);
				b = Mathf.Max(0, b);
				dropdown.value = b;
			}
			else
			{
				OptionHolder.SetOption(optionSO.optionName, value);
			}
		}
	}

	protected override void OnThemeChanged(ColorTheme theme)
	{
		base.OnThemeChanged(theme);
		((Selectable)(object)dropdown).colors = theme.ui.button.ToColorBlock();
		((Graphic)(object)dropdown.captionText).color = theme.ui.ButtonTextColor;
		Toggle componentInChildren = dropdown.template.GetComponentInChildren<Toggle>(includeInactive: true);
		if (componentInChildren != null)
		{
			componentInChildren.colors = theme.ui.dropdown_item.ToColorBlock();
		}
		if (dropdown.template.TryGetComponent<Image>(out var component))
		{
			component.color = theme.ui.dropdown_item.NormalColor;
		}
		if (dropdown.template.TryGetComponent<ScrollRect>(out var component2) && component2.verticalScrollbar.handleRect.TryGetComponent<Image>(out var component3))
		{
			component3.color = theme.ui.DropdownScrollbarColor;
		}
		if (dropdownArrow != null)
		{
			dropdownArrow.color = theme.ui.ButtonTextColor;
		}
		if (dropdownBackground != null)
		{
			dropdownBackground.color = theme.ui.DropdownBackgroundColor;
		}
		if ((Object)(object)dropdownItemText != null)
		{
			((Graphic)(object)dropdownItemText).color = theme.ui.DropdownTextColor;
		}
		if (dropdownItemCheckmark != null)
		{
			dropdownItemCheckmark.color = theme.ui.DropdownTextColor;
		}
	}

	private void OnDestroy()
	{
		if (optionSO != null)
		{
			optionSO.OnOptionChanged -= RefreshOptions;
			optionSO = null;
		}
	}
}
