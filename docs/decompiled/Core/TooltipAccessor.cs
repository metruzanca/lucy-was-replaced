using System;
using UnityEngine;

public class TooltipAccessor : MonoBehaviour
{
	[Header("Assign an object that inherits ITooltipHandler here.")]
	public GameObject tooltipHandler;

	public TooltipInfo GetTooltipInfo(Action updateTooltipCallback)
	{
		return tooltipHandler.GetComponent<ITooltipHandler>().GetTooltipInfo(updateTooltipCallback);
	}

	public void TooltipGone()
	{
		tooltipHandler.GetComponent<ITooltipHandler>().TooltipGone();
	}
}
