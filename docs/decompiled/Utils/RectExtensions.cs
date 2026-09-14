using UnityEngine;

public static class RectExtensions
{
	public static Rect Intersection(this Rect rect1, Rect rect2)
	{
		float num = Mathf.Max(rect1.xMin, rect2.xMin);
		float num2 = Mathf.Min(rect1.xMax, rect2.xMax);
		float num3 = Mathf.Max(rect1.yMin, rect2.yMin);
		float num4 = Mathf.Min(rect1.yMax, rect2.yMax);
		if (num < num2 && num3 < num4)
		{
			return new Rect(num, num3, num2 - num, num4 - num3);
		}
		return Rect.zero;
	}

	public static float Area(this Rect rect)
	{
		return rect.width * rect.height;
	}
}
