using System.Collections.Concurrent;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Workspace : WorkspaceBase
{
	public RectTransform zoomContainer;

	public Canvas editorCanvas;

	public CodeCompleter codeCompleter;

	public CameraController cameraController;

	public SearchBox searchBox;

	public Tooltip tooltip;

	[SerializeField]
	private ScrollRect scrollRect;

	[SerializeField]
	private float zoomSpeed;

	[SerializeField]
	private float maxCanvasScaleFactor;

	[SerializeField]
	private float minCanvasScaleFactor;

	public bool slowMode;

	[SerializeField]
	private Vector2 spawnWindowOffset;

	[SerializeField]
	private Vector2 spawnDocsWindowOffset;

	[SerializeField]
	private CodeWindow codeWinPrefab;

	[SerializeField]
	private DocsWindow docWinPrefab;

	[SerializeField]
	private Texture2D textCursor;

	public Window activeWindow;

	public UndoHistory undoHistory = new UndoHistory();

	public volatile ConcurrentDictionary<string, CodeWindow> codeWindows = new ConcurrentDictionary<string, CodeWindow>();

	private bool containerDirty;

	private Window transitionTarget;

	private int transitionTargetCharIndex;

	private CodeInputField transitionTargetInputField;

	private float transitionStartTime;

	private float transitionEndTime;

	private Vector2 transitionStartPosition;

	public void Scroll(float scroll)
	{
		Window openWindow = GetOpenWindow(Input.mousePosition);
		CodeWindow component2;
		if (openWindow != null && openWindow.TryGetComponent<DocsWindow>(out var component))
		{
			component.Scroll(scroll);
		}
		else if (openWindow != null && openWindow.TryGetComponent<CodeWindow>(out component2))
		{
			component2.Scroll(scroll);
		}
		else
		{
			Zoom(scroll);
		}
	}

	public void Zoom(float zoom)
	{
		if (zoom > 0f)
		{
			cameraController.zoom = Mathf.Clamp(cameraController.zoom * zoomSpeed, minCanvasScaleFactor, maxCanvasScaleFactor);
			zoomContainer.localScale = Vector3.one * cameraController.zoom;
			container.GetComponent<ContainerScaler>().UpdateMarginSize();
		}
		else if (zoom < 0f)
		{
			cameraController.zoom = Mathf.Clamp(cameraController.zoom / zoomSpeed, minCanvasScaleFactor, maxCanvasScaleFactor);
			zoomContainer.localScale = Vector3.one * cameraController.zoom;
			container.GetComponent<ContainerScaler>().UpdateMarginSize();
		}
	}

	protected override void Update()
	{
		base.Update();
		if (containerDirty)
		{
			UpdateContainerSizeInternal();
		}
		LerpCameraToTarget();
	}

	protected override void Start()
	{
		base.Start();
		SetViewportOptions("viewport sliding");
		OptionHolder.OnOptionChanged += SetViewportOptions;
	}

	protected override Texture2D GetCursorForHoveredWindow(Window window)
	{
		Texture2D cursorForHoveredWindow = base.GetCursorForHoveredWindow(window);
		if (cursorForHoveredWindow != null)
		{
			return cursorForHoveredWindow;
		}
		if (window.TryGetComponent<CodeWindow>(out var component) && component.IsPointerOverCodeInput())
		{
			return textCursor;
		}
		return null;
	}

	private void SetViewportOptions(string option)
	{
		if (option == "viewport sliding")
		{
			scrollRect.inertia = OptionHolder.GetString("viewport sliding") == "enabled";
		}
	}

	public override void OnWindowRectChanged(Window window)
	{
		base.OnWindowRectChanged(window);
		containerDirty = true;
	}

	private void UpdateContainerSizeInternal()
	{
		container.GetComponent<ContainerScaler>().UpdateSize();
		containerDirty = false;
	}

	public void OpenCodeWindow(string fileName, string code, Vector2 offset, Vector2 size = default(Vector2))
	{
		if (!IsWindowOpen(fileName, ignoreCase: true))
		{
			CodeWindow codeWindow = OpenWindow(codeWinPrefab, fileName, offset, size);
			codeWindow.workspace = this;
			if (!string.IsNullOrEmpty(code))
			{
				codeWindow.Load(code);
			}
			else
			{
				codeWindow.Load("");
			}
			codeWindow.Parse();
		}
	}

	public void OpenDocsWindow(string windowName, string docsPage, Vector2 offset, Vector2 size = default(Vector2))
	{
		OpenWindow(docWinPrefab, windowName, offset, size).LoadDoc(docsPage);
	}

	public void OpenAndGoTo(string doc)
	{
		AddNewDocsWindow(doc);
		if (MainSim.Inst.researchMenu.IsOpen)
		{
			MainSim.Inst.researchMenu.OpenCloseMenu();
		}
	}

	public override void RemoveWindow(Window window)
	{
		base.RemoveWindow(window);
		codeWindows.Remove(window.windowName, out var _);
	}

	public override void RenameWindow(Window window, string newWindowName)
	{
		if (window.TryGetComponent<CodeWindow>(out var component))
		{
			codeWindows.Remove(window.windowName, out var _);
			codeWindows[newWindowName] = component;
		}
		base.RenameWindow(window, newWindowName);
	}

	protected override void RegisterWindow(Window win, string name, Vector2 offset, Vector2 size = default(Vector2))
	{
		base.RegisterWindow(win, name, offset, size);
		if (win.TryGetComponent<CodeWindow>(out var component))
		{
			component.fileName = name;
			codeWindows[name] = component;
		}
		containerDirty = true;
	}

	public void MoveCameraTo(Window target, bool alwaysMoveWindow = false, int charIndex = -1, CodeInputField inputField = null)
	{
		RectTransform component = target.GetComponent<RectTransform>();
		if (!alwaysMoveWindow)
		{
			if (inputField != null)
			{
				Vector3 bottomRight = inputField.textComponent.textInfo.characterInfo[charIndex].bottomRight;
				Vector3 vector = inputField.textComponent.rectTransform.TransformPoint(bottomRight);
				if (vector.x >= 0f && vector.x < (float)Screen.width && vector.y >= 0f && vector.y < (float)Screen.height)
				{
					return;
				}
			}
			else
			{
				Rect rect = new Rect(Vector2.zero, ((RectTransform)base.transform).rect.size);
				Vector2 size = rect.size;
				Vector2 anchoredPosition = target.HighestDockedParent.GetComponent<RectTransform>().anchoredPosition;
				Vector2 vector2 = new Vector2(0f, target.DockingOffset);
				Vector2 position = anchoredPosition - vector2 + container.anchoredPosition + size / 2f;
				Rect rect2 = new Rect(position, component.rect.size);
				rect2.position -= new Vector2(0f, component.rect.size.y);
				if (rect2.Intersection(rect).Area() > rect2.Area() * 0.5f)
				{
					return;
				}
			}
		}
		float num = 0.2f;
		transitionStartPosition = container.anchoredPosition;
		transitionStartTime = Time.time;
		transitionEndTime = transitionStartTime + num;
		transitionTarget = target;
		transitionTargetCharIndex = charIndex;
		transitionTargetInputField = inputField;
	}

	private void LerpCameraToTarget()
	{
		if (transitionEndTime >= Time.time && transitionTarget != null)
		{
			RectTransform component = transitionTarget.GetComponent<RectTransform>();
			Vector2 anchoredPosition = transitionTarget.HighestDockedParent.GetComponent<RectTransform>().anchoredPosition;
			Vector2 vector = new Vector2(0f, transitionTarget.DockingOffset);
			Vector2 b = -anchoredPosition + vector;
			if (transitionTargetInputField != null)
			{
				Vector3 bottomRight = transitionTargetInputField.textComponent.textInfo.characterInfo[transitionTargetCharIndex].bottomRight;
				Vector3 position = transitionTargetInputField.textComponent.rectTransform.TransformPoint(bottomRight);
				Vector3 vector2 = component.InverseTransformPoint(position);
				b -= (Vector2)vector2;
			}
			else
			{
				b += new Vector2(0f - component.rect.width, component.rect.height) / 2f;
			}
			float t = (Time.time - transitionStartTime) / (transitionEndTime - transitionStartTime);
			container.anchoredPosition = Vector2.Lerp(transitionStartPosition, b, t);
		}
		else
		{
			transitionTarget = null;
		}
	}

	public override bool IsValidFileName(string fileName)
	{
		if (fileName == "__builtins__")
		{
			return false;
		}
		return base.IsValidFileName(fileName);
	}

	public void AddNewWindow()
	{
		OpenCodeWindow(GenerateFileName("f"), "", -container.anchoredPosition + spawnWindowOffset);
		if (codeWindows.Count >= 20)
		{
			Achievements.UnlockAchievement("CHAOS");
		}
	}

	public void AddNewDocsWindow(string doc = "docs/home.md", Vector2 offset = default(Vector2))
	{
		OpenDocsWindow(GenerateFileName("docs"), doc, -container.anchoredPosition + spawnDocsWindowOffset + offset);
	}

	public void CallAddNewDocsWindow()
	{
		AddNewDocsWindow();
	}

	public void Undo()
	{
		if (activeWindow != null)
		{
			undoHistory.Undo(activeWindow.gameObject);
		}
	}

	public void Redo()
	{
		if (activeWindow != null)
		{
			undoHistory.Redo(activeWindow.gameObject);
		}
	}
}
