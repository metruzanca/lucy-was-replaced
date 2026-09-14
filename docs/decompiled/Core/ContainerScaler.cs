using System.Collections.Generic;
using UnityEngine;

public class ContainerScaler : MonoBehaviour
{
	[SerializeField]
	private RectTransform contentRect;

	[SerializeField]
	private Canvas canvas;

	[SerializeField]
	private CameraController cameraController;

	private Vector2 rectSize;

	public void UpdateMarginSize()
	{
		Vector2 vector = new Vector2(Screen.width, Screen.height) / canvas.scaleFactor / cameraController.zoom;
		GetComponent<RectTransform>().sizeDelta = rectSize + vector * 1.4f;
	}

	public void UpdateSize()
	{
		RectTransform component = base.transform.parent.GetComponent<RectTransform>();
		List<RectTransform> list = new List<RectTransform>();
		int childCount = base.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			RectTransform rectTransform = (RectTransform)base.transform.GetChild(0);
			list.Add(rectTransform);
			rectTransform.transform.SetParent(component, worldPositionStays: true);
		}
		Vector2 vector = Vector2.zero;
		Vector2 vector2 = Vector2.zero;
		if (list.Count > 0)
		{
			vector = list[0].rect.min + list[0].anchoredPosition;
			vector2 = list[0].rect.max + list[0].anchoredPosition;
			foreach (RectTransform item in list)
			{
				vector = Vector2.Min(vector, item.rect.min + item.anchoredPosition);
				Window window = item.GetComponent<Window>();
				while (window?.DockedChild != null)
				{
					window = window.DockedChild;
					vector.y += ((RectTransform)window.transform).rect.y;
				}
				vector2 = Vector2.Max(vector2, item.rect.max + item.anchoredPosition);
			}
		}
		Vector2 vector3 = new Vector2(Screen.width, Screen.height) / canvas.scaleFactor;
		Rect rect = default(Rect);
		rect.min = vector;
		rect.max = vector2;
		RectTransform component2 = GetComponent<RectTransform>();
		component2.sizeDelta = rect.size + vector3 * 1.4f;
		rectSize = rect.size;
		component2.anchoredPosition = rect.center;
		foreach (RectTransform item2 in list)
		{
			item2.SetParent(base.transform, worldPositionStays: true);
		}
	}
}
