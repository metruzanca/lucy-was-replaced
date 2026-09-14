using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class Window : MonoBehaviour, IDragHandler, IEventSystemHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
{
	[SerializeField]
	private bool dockable;

	public RectTransform dockPosition;

	public WorkspaceBase workspace;

	public string windowName;

	public bool isMinimized;

	public Vector2 minimizedSize;

	public Vector2 playerSetSize;

	public Vector2 automaticSize;

	public Vector2 minSize;

	public float resizeMargin = 10f;

	public List<GameObject> disableOnMinimize;

	public UnityEvent<bool> OnMinimizeChanged;

	private bool horitontalResize;

	private bool verticalResize;

	private RectTransform windowRect;

	public Window dockedParent { get; private set; }

	public RectTransform WindowRect => windowRect;

	public Window DockedChild
	{
		get
		{
			if (dockPosition == null || dockPosition.childCount == 0)
			{
				return null;
			}
			return dockPosition.GetChild(0)?.GetComponent<Window>();
		}
	}

	public Window LeastDockedChild
	{
		get
		{
			Window window = this;
			while (window.DockedChild != null)
			{
				window = window.DockedChild;
			}
			return window;
		}
	}

	public Window HighestDockedParent
	{
		get
		{
			Window window = this;
			while (window.dockedParent != null)
			{
				window = window.dockedParent;
			}
			return window;
		}
	}

	public float DockingOffset
	{
		get
		{
			Window window = HighestDockedParent;
			float num = 0f;
			while (window != this)
			{
				num += windowRect.rect.height;
				window = window.DockedChild;
			}
			return num;
		}
	}

	public void SetMinmized(bool minimized)
	{
		isMinimized = minimized;
		workspace.OnWindowRectChanged(this);
		OnMinimizeChanged?.Invoke(minimized);
		foreach (GameObject item in disableOnMinimize)
		{
			item.SetActive(!minimized);
		}
		UpdateSize();
		MoveToFront();
	}

	public void Close()
	{
		if (DockedChild != null)
		{
			DockedChild.GetComponent<Window>().Undock();
		}
		workspace.RemoveWindow(this);
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void UpdateSize()
	{
		TryGetComponent<RectTransform>(out windowRect);
		if (isMinimized)
		{
			windowRect.sizeDelta = minimizedSize;
			return;
		}
		Vector2 sizeDelta = Vector2.Max(playerSetSize, minSize);
		if (playerSetSize.y < minSize.y)
		{
			sizeDelta.y = MathF.Max(automaticSize.y, minSize.y);
		}
		windowRect.sizeDelta = sizeDelta;
		if (TryGetComponent<CodeWindow>(out var component))
		{
			component.UpdateCodeInputSize();
		}
	}

	public void ToggleMinimize()
	{
		SetMinmized(!isMinimized);
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (eventData.button != 0)
		{
			return;
		}
		if (horitontalResize || verticalResize)
		{
			Vector2 vector = windowRect.InverseTransformVector(eventData.delta);
			if (horitontalResize)
			{
				playerSetSize.x += vector.x;
				playerSetSize.x = MathF.Max(playerSetSize.x, minSize.x);
			}
			if (verticalResize)
			{
				playerSetSize.y -= vector.y;
				playerSetSize.y = MathF.Max(playerSetSize.y, minSize.y);
			}
			if (horitontalResize || verticalResize)
			{
				UpdateSize();
			}
		}
		else
		{
			Vector2 vector2 = windowRect.parent.InverseTransformVector(eventData.delta);
			windowRect.anchoredPosition += vector2;
		}
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			if (IsPointerOnResizeArea(eventData.pressPosition, out horitontalResize, out verticalResize))
			{
				playerSetSize = windowRect.sizeDelta;
			}
			if (dockable && !verticalResize && !horitontalResize)
			{
				Undock();
				GetComponent<CanvasGroup>().blocksRaycasts = false;
			}
		}
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (eventData.button != 0)
		{
			return;
		}
		workspace.OnWindowRectChanged(this);
		if (dockable && !verticalResize && !horitontalResize)
		{
			Window window = eventData.pointerCurrentRaycast.gameObject?.GetComponent<Window>();
			if (window == null)
			{
				window = eventData.pointerCurrentRaycast.gameObject?.GetComponentInParent<Window>();
			}
			DockOnto(window);
			GetComponent<CanvasGroup>().blocksRaycasts = true;
		}
		if (playerSetSize.x <= minSize.x)
		{
			playerSetSize.x = 0f;
		}
		if (playerSetSize.y <= minSize.y)
		{
			playerSetSize.y = 0f;
		}
		UpdateSize();
		horitontalResize = false;
		verticalResize = false;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			MoveToFront();
		}
	}

	public bool IsPointerOnResizeArea(Vector2 screenPoint, out bool horizontal, out bool vertical)
	{
		horizontal = (vertical = false);
		if (isMinimized)
		{
			return false;
		}
		Vector2 vector = default(Vector2);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(windowRect, screenPoint, (Camera)null, ref vector);
		vector.y = 0f - vector.y;
		Rect rect = windowRect.rect;
		horizontal = rect.width - vector.x < resizeMargin;
		vertical = rect.height - vector.y < resizeMargin;
		return horizontal | vertical;
	}

	public void MoveToFront()
	{
		HighestDockedParent.transform.SetAsLastSibling();
	}

	public void DockOnto(Window other)
	{
		if (other == null || !other.dockable || !dockable)
		{
			return;
		}
		Window window = other;
		do
		{
			if (window == this)
			{
				Debug.LogError("Docking window '" + windowName + "' onto '" + other.windowName + "' would create a docking loop, prevented docking.");
				return;
			}
			window = window.dockedParent;
		}
		while (window != null);
		other.DockedChild?.DockOnto(LeastDockedChild);
		base.transform.SetParent(other.dockPosition, worldPositionStays: false);
		windowRect.anchoredPosition = Vector2.zero;
		dockedParent = other;
	}

	public void Undock()
	{
		if (dockedParent != null)
		{
			base.transform.SetParent(workspace.container);
			dockedParent = null;
		}
	}

	private void Awake()
	{
		TryGetComponent<RectTransform>(out windowRect);
	}
}
