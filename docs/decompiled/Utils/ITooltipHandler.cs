using System;

public interface ITooltipHandler
{
	TooltipInfo GetTooltipInfo(Action updateTooltipCallback);

	void TooltipGone();
}
