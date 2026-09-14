using UnityEngine;

public class TooltipPositioner : MonoBehaviour
{
	private Vector2 offset = new Vector2(-20f, -20f);

	private void Start()
	{
		UpdatePosition();
	}

	private void Update()
	{
		UpdatePosition();
	}

	public void UpdatePosition()
	{
		RectTransform obj = (RectTransform)base.transform;
		Vector2 vector = offset;
		Vector2 pivot = default(Vector2);
		if (Input.mousePosition.x * 2f > (float)Screen.width)
		{
			pivot.x = 1f;
		}
		else
		{
			pivot.x = 0f;
			vector *= new Vector2(-1f, 1f);
		}
		if (Input.mousePosition.y * 2f > (float)Screen.height)
		{
			pivot.y = 1f;
		}
		else
		{
			pivot.y = 0f;
			vector *= new Vector2(1f, -1f);
		}
		obj.pivot = pivot;
		obj.anchoredPosition = (Vector2)Input.mousePosition + vector;
	}
}
