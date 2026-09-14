using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ErrorMessage : MonoBehaviour
{
	[SerializeField]
	private float codeBoxMargin;

	[SerializeField]
	private float errorBoxHeight;

	[SerializeField]
	private Image background;

	[SerializeField]
	private Image messageBorder;

	[SerializeField]
	private RectTransform codeBorder;

	[SerializeField]
	private TextMeshProUGUI errorText;

	[SerializeField]
	private TextMeshProUGUI errorTitle;

	private string error;

	private Vector3 left;

	private Vector3 right;

	private bool updateOnEnable;

	public bool IsShowing()
	{
		return base.gameObject.activeInHierarchy;
	}

	public void ShowError(string error, Vector3 left, Vector3 right)
	{
		this.error = error;
		this.left = left;
		this.right = right;
		if (base.gameObject.activeInHierarchy)
		{
			UpdateErrorMessage();
			return;
		}
		updateOnEnable = true;
		base.gameObject.SetActive(value: true);
	}

	private void OnEnable()
	{
		if (updateOnEnable)
		{
			updateOnEnable = false;
			UpdateErrorMessage();
		}
	}

	private void UpdateErrorMessage()
	{
		RectTransform rectTransform = (RectTransform)base.transform;
		rectTransform.position = (left + right) / 2f;
		codeBorder.sizeDelta = new Vector2((rectTransform.InverseTransformPoint(right) - rectTransform.InverseTransformPoint(left)).x + codeBoxMargin, codeBorder.sizeDelta.y);
		((TMP_Text)errorText).text = Localizer.Localize(error);
		rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, ((TMP_Text)errorText).preferredHeight + errorBoxHeight);
		ColorTheme theme = ThemeManager.Inst.Theme;
		background.color = theme.code.ErrorBackgroundColor;
		messageBorder.color = theme.code.ErrorBorderColor;
		((Graphic)(object)errorText).color = theme.code.ErrorMessageColor;
		((Graphic)(object)errorTitle).color = theme.code.ErrorTitleColor;
		if (codeBorder.TryGetComponent<Image>(out var component))
		{
			component.color = theme.code.ErrorBorderColor;
		}
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
	}
}
