using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class OptionMenu : MonoBehaviour
{
	[SerializeField]
	private ColoredButton coloredButtonPrefab;

	[SerializeField]
	private Image background;

	[SerializeField]
	private RectTransform optionContainer;

	[SerializeField]
	private RectTransform tabsContainer;

	private IEnumerable<OptionSO> allOptions;

	private Dictionary<string, ColoredButton> categoryButtons;

	private string currentCategory;

	private Dictionary<string, OptionUI> optionUIs = new Dictionary<string, OptionUI>();

	public void Setup()
	{
		if (categoryButtons != null)
		{
			return;
		}
		allOptions = (IEnumerable<OptionSO>)Enumerable.OrderByDescending<OptionSO, float>(ResourceManager.GetAllOptions(), (Func<OptionSO, float>)((OptionSO o) => o.importance));
		if (Enumerable.Count<OptionSO>(allOptions) == 0)
		{
			return;
		}
		IEnumerable<string> enumerable = Enumerable.Distinct<string>(Enumerable.Select<OptionSO, string>(allOptions, (Func<OptionSO, string>)((OptionSO o) => o.category)));
		currentCategory = Enumerable.First<string>(enumerable);
		categoryButtons = new Dictionary<string, ColoredButton>();
		foreach (string category in enumerable)
		{
			ColoredButton coloredButton = UnityEngine.Object.Instantiate(coloredButtonPrefab, tabsContainer);
			coloredButton.Text = category;
			coloredButton.GetComponent<Button>().onClick.AddListener(delegate
			{
				ChangeCategory(category);
			});
			categoryButtons[category] = coloredButton;
		}
		ChangeCategory(currentCategory);
		OptionHolder.OnOptionChanged += OnOptionChanged;
	}

	private void ChangeCategory(string newCategory)
	{
		categoryButtons[currentCategory].Interactable = true;
		currentCategory = newCategory;
		categoryButtons[newCategory].Interactable = false;
		for (int num = optionContainer.childCount - 1; num >= 0; num--)
		{
			UnityEngine.Object.Destroy(optionContainer.GetChild(num).gameObject);
		}
		optionUIs.Clear();
		foreach (OptionSO item in Enumerable.Where<OptionSO>(allOptions, (Func<OptionSO, bool>)((OptionSO o) => o.category == currentCategory)))
		{
			OptionUI optionUI = UnityEngine.Object.Instantiate(item.optionUI, optionContainer);
			optionUI.Setup(item);
			optionUIs[item.name] = optionUI;
		}
	}

	private void OnOptionChanged(string optionName)
	{
		if (optionUIs.TryGetValue(optionName, out var value))
		{
			value.UpdateValue();
		}
	}

	private void OnEnable()
	{
		ThemeManager.Inst.OnThemeChanged += OnThemeChanged;
	}

	private void OnDisable()
	{
		ThemeManager.Inst.OnThemeChanged -= OnThemeChanged;
	}

	private void OnThemeChanged(ColorTheme theme)
	{
		background.color = theme.ui.OptionsMenuColor;
	}
}
