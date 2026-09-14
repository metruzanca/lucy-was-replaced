using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpoilerWarning : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI text;

	[SerializeField]
	private Image background;

	public void SetText(string text)
	{
		((TMP_Text)this.text).text = text;
		ColorTheme theme = ThemeManager.Inst.Theme;
		background.color = theme.docs.SpoilerColor;
		((Graphic)(object)this.text).color = theme.docs.SpoilerTextColor;
	}

	public void OnClicked()
	{
		Object.Destroy(base.gameObject);
	}
}
