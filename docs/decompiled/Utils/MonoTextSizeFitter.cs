using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(TMP_InputField))]
public class MonoTextSizeFitter : MonoBehaviour
{
	[SerializeField]
	private float widthMultiplier = 10f;

	private TMP_InputField codeInputField;

	private RectTransform rectTransform;

	private void Awake()
	{
		codeInputField = GetComponent<TMP_InputField>();
		rectTransform = GetComponent<RectTransform>();
	}

	private void OnEnable()
	{
		((UnityEvent<string>)(object)codeInputField.onValueChanged).AddListener((UnityAction<string>)OnTextChanged);
	}

	private void OnDisable()
	{
		((UnityEvent<string>)(object)codeInputField.onValueChanged).RemoveListener((UnityAction<string>)OnTextChanged);
	}

	private void OnTextChanged(string text)
	{
		if (rectTransform != null)
		{
			Vector2 sizeDelta = rectTransform.sizeDelta;
			sizeDelta.x = (float)text.Length * widthMultiplier;
			rectTransform.sizeDelta = sizeDelta;
		}
	}
}
