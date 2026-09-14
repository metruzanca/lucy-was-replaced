using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScaledScrollRect : ScrollRect
{
	public Transform zoomContainer;

	private Vector2 lastLocalPointerPosition;

	private Canvas rootCanvas;

	protected override void Awake()
	{
		base.Awake();
		zoomContainer = base.content.parent;
		rootCanvas = GetComponentInParent<Canvas>();
	}

	public override void OnBeginDrag(PointerEventData eventData)
	{
		base.OnBeginDrag(eventData);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(base.viewport, eventData.position, eventData.pressEventCamera, ref lastLocalPointerPosition);
	}

	public override void OnDrag(PointerEventData eventData)
	{
		Vector2 vector = default(Vector2);
		if (IsActive() && !(base.content == null) && RectTransformUtility.ScreenPointToLocalPointInRectangle(base.viewport, eventData.position, eventData.pressEventCamera, ref vector))
		{
			Vector3 vector2 = ((zoomContainer != null) ? zoomContainer.lossyScale : Vector3.one);
			vector2.x /= rootCanvas.scaleFactor;
			vector2.y /= rootCanvas.scaleFactor;
			Vector2 vector3 = vector - lastLocalPointerPosition;
			vector3.x /= vector2.x;
			vector3.y /= vector2.y;
			Vector2 contentAnchoredPosition = base.content.anchoredPosition + vector3;
			SetContentAnchoredPosition(contentAnchoredPosition);
			lastLocalPointerPosition = vector;
		}
	}
}
