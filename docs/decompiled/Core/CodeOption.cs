using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CodeOption : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, ITooltipHandler
{
	private int index;

	private CodeCompleter completer;

	public TextMeshProUGUI text;

	[SerializeField]
	private Image highlightImage;

	[SerializeField]
	private Transform tooltipPosition;

	public void SetUp(int index, CodeCompleter completer)
	{
		this.index = index;
		this.completer = completer;
		Unhighlight();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		completer.SelectOption(index);
		completer.Commit();
	}

	public void Highlight()
	{
		ColorTheme theme = ThemeManager.Inst.Theme;
		highlightImage.color = theme.code.CompleteSelectedBackgroundColor;
		((Graphic)(object)text).color = theme.code.CompleteSelectedTextColor;
	}

	public void Unhighlight()
	{
		ColorTheme theme = ThemeManager.Inst.Theme;
		highlightImage.color = Color.clear;
		((Graphic)(object)text).color = theme.code.CompleteTextColor;
	}

	public TooltipInfo GetTooltipInfo(Action updateTooltipCallback)
	{
		TooltipInfo completionTooltip = completer.GetCompletionTooltip(index);
		if (completionTooltip != null)
		{
			completionTooltip.delay = 0.2f;
			completionTooltip.fixedPosition = tooltipPosition.position;
			completionTooltip.anchor = TooltipInfo.Anchor.BottomRight;
		}
		return completionTooltip;
	}

	public void TooltipGone()
	{
	}
}
