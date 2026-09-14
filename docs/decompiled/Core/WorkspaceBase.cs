using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class WorkspaceBase : MonoBehaviour
{
	public RectTransform container;

	public Camera uiCam;

	[SerializeField]
	private Texture2D horizontalResizeCursor;

	[SerializeField]
	private Texture2D verticalResizeCursor;

	[SerializeField]
	private Texture2D bidirectionalResizeCursor;

	private static char[] invalidFileNameChars;

	protected Dictionary<string, Window> openWindows = new Dictionary<string, Window>();

	public IEnumerable<Window> OpenWindows => openWindows.Values;

	public TWindow OpenWindow<TWindow>(TWindow prefab, string windowName, Vector2 offset, Vector2 size = default(Vector2)) where TWindow : Component
	{
		TWindow val = UnityEngine.Object.Instantiate(prefab, container);
		if (!val.TryGetComponent<Window>(out var component))
		{
			Debug.LogError("Invalid window prefab '" + prefab.name + "': No Window component found", prefab);
			return null;
		}
		RegisterWindow(component, windowName, offset, size);
		return val;
	}

	public virtual void OnWindowRectChanged(Window window)
	{
	}

	public bool IsWindowOpen(string windowName)
	{
		return openWindows.ContainsKey(windowName);
	}

	public bool IsWindowOpen(string windowName, bool ignoreCase = false)
	{
		if (!ignoreCase)
		{
			return openWindows.ContainsKey(windowName);
		}
		foreach (string key in openWindows.Keys)
		{
			if (StringComparer.OrdinalIgnoreCase.Equals(key, windowName))
			{
				return true;
			}
		}
		return false;
	}

	public Window GetOpenWindow(string windowName)
	{
		return openWindows.GetValueOrDefault(windowName);
	}

	public bool TryGetOpenWindow(string windowName, out Window window)
	{
		return openWindows.TryGetValue(windowName, out window);
	}

	public Window GetOpenWindow(Vector2 screenPos)
	{
		Window result = null;
		int num = -1;
		foreach (Window value in openWindows.Values)
		{
			int siblingIndex = value.transform.GetSiblingIndex();
			if (siblingIndex >= num && RectTransformUtility.RectangleContainsScreenPoint(value.WindowRect, screenPos, uiCam))
			{
				result = value;
				num = siblingIndex;
			}
		}
		return result;
	}

	public bool TryGetOpenWindow(Vector2 screenPos, out Window window)
	{
		window = GetOpenWindow(screenPos);
		return window != null;
	}

	public virtual void RenameWindow(Window window, string newWindowName)
	{
		openWindows.Remove(window.windowName);
		openWindows[newWindowName] = window;
		window.windowName = newWindowName;
	}

	public virtual void RemoveWindow(Window window)
	{
		openWindows.Remove(window.windowName);
	}

	public string GenerateFileName(string prefix)
	{
		int num = 0;
		string text;
		do
		{
			text = prefix + num;
			num++;
		}
		while (!IsValidFileName(text));
		return text;
	}

	public virtual bool IsValidFileName(string fileName)
	{
		if (string.IsNullOrEmpty(fileName) || IsWindowOpen(fileName, ignoreCase: true))
		{
			return false;
		}
		if (invalidFileNameChars == null)
		{
			invalidFileNameChars = Path.GetInvalidFileNameChars();
		}
		return fileName.IndexOfAny(invalidFileNameChars) < 0;
	}

	protected virtual void Start()
	{
	}

	protected virtual void Update()
	{
		Texture2D texture2D = null;
		if (TryGetOpenWindow(Input.mousePosition, out var window))
		{
			texture2D = GetCursorForHoveredWindow(window);
		}
		if (texture2D != null)
		{
			Cursor.SetCursor(texture2D, new Vector2((float)texture2D.width / 2f, (float)texture2D.height / 2f), CursorMode.Auto);
		}
		else
		{
			Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
		}
	}

	protected virtual void RegisterWindow(Window window, string windowName, Vector2 offset, Vector2 size = default(Vector2))
	{
		window.workspace = this;
		openWindows[windowName] = window;
		window.windowName = windowName;
		window.GetComponent<RectTransform>().anchoredPosition = offset;
		if (size != default(Vector2))
		{
			window.playerSetSize = size;
			window.UpdateSize();
		}
	}

	protected virtual Texture2D GetCursorForHoveredWindow(Window window)
	{
		if (window.IsPointerOnResizeArea(Input.mousePosition, out var horizontal, out var vertical))
		{
			if (horizontal && vertical)
			{
				return bidirectionalResizeCursor;
			}
			if (horizontal)
			{
				return horizontalResizeCursor;
			}
			return verticalResizeCursor;
		}
		return null;
	}
}
