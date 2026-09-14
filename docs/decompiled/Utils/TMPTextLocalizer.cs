using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPTextLocalizer : MonoBehaviour
{
	private TextMeshProUGUI textComp;

	private string textKey;

	private string localizedText;

	private void Start()
	{
		textComp = GetComponent<TextMeshProUGUI>();
		textKey = ((TMP_Text)textComp).text;
		LocalizeText();
		TMPro_EventManager.TEXT_CHANGED_EVENT.Add((Action<UnityEngine.Object>)OnTextChanged);
		OptionHolder.OnOptionChanged += OnSettingChanged;
	}

	private void OnDestroy()
	{
		TMPro_EventManager.TEXT_CHANGED_EVENT.Remove((Action<UnityEngine.Object>)OnTextChanged);
		OptionHolder.OnOptionChanged -= OnSettingChanged;
	}

	public void LocalizeText()
	{
		if (((TMP_Text)textComp).text != localizedText)
		{
			textKey = ((TMP_Text)textComp).text;
		}
		localizedText = Localizer.Localize(textKey);
		if (((TMP_Text)textComp).text != localizedText)
		{
			((TMP_Text)textComp).text = localizedText;
			((TMP_Text)textComp).ForceMeshUpdate(false, false);
		}
	}

	private void OnTextChanged(UnityEngine.Object obj)
	{
		if (obj == (UnityEngine.Object)(object)textComp)
		{
			LocalizeText();
		}
	}

	private void OnSettingChanged(string setting)
	{
		if (setting == "language")
		{
			LocalizeText();
		}
	}
}
