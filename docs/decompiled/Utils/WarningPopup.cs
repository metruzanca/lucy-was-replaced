using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class WarningPopup : MonoBehaviour
{
	public struct ButtonData
	{
		public string text;

		public UnityAction callback;

		public ButtonData(string text, UnityAction callback)
		{
			this.text = text;
			this.callback = callback;
		}
	}

	[SerializeField]
	private ColoredButton buttonPrefab;

	[SerializeField]
	private Image window;

	[SerializeField]
	private TextMeshProUGUI text;

	[SerializeField]
	private Transform buttons;

	[SerializeField]
	private MonoBehaviour inputBoss;

	public static WarningPopup Inst => UtilsGameObjects.Inst.WarningPopup;

	public void ShowPopup(string text, List<ButtonData> buttonsToAdd)
	{
		((TMP_Text)this.text).text = text;
		foreach (Transform button in buttons)
		{
			Object.Destroy(button.gameObject);
		}
		base.gameObject.SetActive(value: true);
		foreach (ButtonData item in buttonsToAdd)
		{
			ColoredButton coloredButton = Object.Instantiate(buttonPrefab, buttons);
			coloredButton.Text = item.text;
			coloredButton.OnClick.AddListener(item.callback);
		}
		inputBoss.enabled = false;
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
		inputBoss.enabled = true;
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
		if (TryGetComponent<RawImage>(out var component))
		{
			component.color = theme.ui.WarningOverlayColor;
		}
		window.color = theme.ui.WarningBackgroundColor;
		((Graphic)(object)text).color = theme.ui.WarningMessageColor;
	}
}
