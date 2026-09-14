using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class BlinkManager : MonoBehaviour
{
	[SerializeField]
	private float blinkTime;

	[SerializeField]
	private float strength;

	[SerializeField]
	private float fullLineAlpha;

	[SerializeField]
	private float vmargin;

	[SerializeField]
	private float hmargin;

	[SerializeField]
	private float horizontalOffset;

	[SerializeField]
	private float height;

	[SerializeField]
	private CameraController cameraController;

	[SerializeField]
	private Canvas canvas;

	private object blinkLock = new object();

	private Dictionary<Node, float> rects = new Dictionary<Node, float>();

	private Node currentNode;

	private Texture2D texture;

	private float prevTime;

	private HashSet<FunctionNode> visitedFunctions = new HashSet<FunctionNode>();

	private void Awake()
	{
		texture = new Texture2D(1, 1);
		texture.SetPixel(0, 0, Color.white);
		texture.Apply();
	}

	private void OnGUI()
	{
		float num = Time.time - prevTime;
		prevTime = Time.time;
		if (MainSim.Inst.researchMenu.IsOpen || OptionHolder.GetString("code highlights", "enabled") == "disabled")
		{
			return;
		}
		bool stepByStepMode = MainSim.Inst.StepByStepMode;
		RectTransform container = MainSim.Inst.workspace.container;
		RectTransform topWindowRect = null;
		CodeWindow component2;
		if (container.childCount > 0 && container.GetChild(container.childCount - 1).TryGetComponent<DocsWindow>(out var component))
		{
			topWindowRect = component.GetComponent<RectTransform>();
		}
		else if (container.childCount > 0 && container.GetChild(container.childCount - 1).TryGetComponent<CodeWindow>(out component2))
		{
			topWindowRect = component2.GetComponent<RectTransform>();
		}
		ColorTheme theme = ThemeManager.Inst.Theme;
		lock (blinkLock)
		{
			foreach (var (node2, num3) in Enumerable.ToList<KeyValuePair<Node, float>>((IEnumerable<KeyValuePair<Node, float>>)rects))
			{
				if (node2.blink && !node2.boxedParams.codeWindow.GetComponent<Window>().isMinimized)
				{
					float alpha = Mathf.Pow(num3 / blinkTime * strength, 2f);
					GUI.color = theme.code.ExecOverlayColor.MultiplyAlpha(alpha);
					Rect worldWordRect = GetWorldWordRect(node2.boxedParams.wordStart, node2.boxedParams.wordEnd, node2.boxedParams.codeWindow, topWindowRect);
					if (worldWordRect.height > 0f && worldWordRect.width > 0f)
					{
						GUI.DrawTexture(worldWordRect, (Texture)texture, (ScaleMode)0);
					}
				}
				if (num3 <= num)
				{
					rects.Remove(node2);
				}
				else
				{
					rects[node2] = num3 - num;
				}
			}
			if (stepByStepMode && currentNode != null && !currentNode.boxedParams.codeWindow.GetComponent<Window>().isMinimized)
			{
				GUI.color = theme.code.ExecOverlayColor.MultiplyAlpha(fullLineAlpha);
				Rect worldLineRect = GetWorldLineRect(currentNode.boxedParams.wordStart, currentNode.boxedParams.codeWindow, topWindowRect);
				if (worldLineRect.height > 0f && worldLineRect.width > 0f)
				{
					GUI.DrawTexture(worldLineRect, (Texture)texture, (ScaleMode)0);
				}
			}
		}
		List<(FunctionNode, Node)> callStack = MainSim.Inst.GetCallStack();
		if (callStack == null)
		{
			return;
		}
		for (int i = 0; i < callStack.Count; i++)
		{
			FunctionNode item = ((i == callStack.Count - 1) ? null : callStack[i + 1].Item1);
			Node item2 = callStack[i].Item2;
			if (item2 == null || visitedFunctions.Contains(item))
			{
				continue;
			}
			visitedFunctions.Add(item);
			if (!item2.boxedParams.codeWindow.GetComponent<Window>().isMinimized)
			{
				GUI.color = theme.code.ExecOverlayColor.MultiplyAlpha(fullLineAlpha);
				Rect worldLineRect2 = GetWorldLineRect(item2.boxedParams.wordStart, item2.boxedParams.codeWindow, topWindowRect);
				if (worldLineRect2.height > 0f && worldLineRect2.width > 0f)
				{
					GUI.DrawTexture(worldLineRect2, (Texture)texture, (ScaleMode)0);
				}
			}
		}
		visitedFunctions.Clear();
	}

	public void EnqueueBlink(Node n)
	{
		lock (blinkLock)
		{
			if (rects.Count < 1000)
			{
				rects[n] = blinkTime;
				currentNode = n;
			}
		}
	}

	private Rect GetWorldWordRect(int start, int end, CodeWindow codeWindow, RectTransform topWindowRect)
	{
		TMP_CharacterInfo[] characterInfo = codeWindow.CodeInput.textComponent.textInfo.characterInfo;
		Vector3 startLocal = (characterInfo[start].bottomLeft + characterInfo[start].bottomRight) * 0.5f;
		Vector3 endLocal = (characterInfo[end - 1].bottomLeft + characterInfo[end - 1].bottomRight) * 0.5f;
		return GetWorldRect(startLocal, endLocal, height, codeWindow, topWindowRect);
	}

	private Rect GetWorldLineRect(int stringIndex, CodeWindow codeWindow, RectTransform topWindowRect)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		TMP_CharacterInfo[] characterInfo = codeWindow.CodeInput.textComponent.textInfo.characterInfo;
		int lineNumber = characterInfo[stringIndex].lineNumber;
		TMP_LineInfo val = codeWindow.CodeInput.textComponent.textInfo.lineInfo[lineNumber];
		Vector3 bottomLeft = characterInfo[val.firstCharacterIndex].bottomLeft;
		Vector3 bottomRight = characterInfo[val.lastCharacterIndex].bottomRight;
		return GetWorldRect(bottomLeft, bottomRight, height, codeWindow, topWindowRect);
	}

	private Rect GetWorldRect(Vector3 startLocal, Vector3 endLocal, float height, CodeWindow codeWindow, RectTransform topWindowRect)
	{
		RectTransform component = codeWindow.CodeInput.GetComponent<RectTransform>();
		Vector3 vector = GetCodeOffset(component);
		Vector2 vector2 = startLocal * canvas.scaleFactor * cameraController.zoom + component.position + vector;
		Vector2 vector3 = endLocal * canvas.scaleFactor * cameraController.zoom + component.position + vector;
		Vector2 vector4 = vector2 - new Vector2(hmargin, vmargin) * canvas.scaleFactor * cameraController.zoom;
		Vector2 vector5 = vector3 + new Vector2(hmargin, height + vmargin) * canvas.scaleFactor * cameraController.zoom;
		vector4.x += horizontalOffset * canvas.scaleFactor * cameraController.zoom;
		vector5.x += horizontalOffset * canvas.scaleFactor * cameraController.zoom;
		vector4.y = (float)Screen.height - vector4.y;
		vector5.y = (float)Screen.height - vector5.y;
		Rect screenRect = GetScreenRect(codeWindow.scrollViewRect);
		Rect rect = new Rect(vector4, vector5 - vector4);
		rect.height = Mathf.Abs(rect.height);
		rect.y -= rect.height;
		Rect rect2 = Rect.MinMaxRect(Mathf.Max(screenRect.xMin, rect.xMin), Mathf.Max(screenRect.yMin, rect.yMin), Mathf.Min(screenRect.xMax, rect.xMax), Mathf.Min(screenRect.yMax, rect.yMax));
		if (topWindowRect != null && codeWindow != topWindowRect.GetComponent<CodeWindow>())
		{
			Rect screenRect2 = GetScreenRect(topWindowRect);
			MaskOutRect(ref rect2, screenRect2);
		}
		RectTransform component2 = MainSim.Inst.workspace.tooltip.Container.GetComponent<RectTransform>();
		if (component2.localScale != Vector3.zero)
		{
			Rect screenRect3 = GetScreenRect(component2);
			MaskOutRect(ref rect2, screenRect3);
		}
		return rect2;
	}

	private void MaskOutRect(ref Rect rect, Rect mask)
	{
		if (rect.yMin < mask.yMax && rect.yMax > mask.yMin)
		{
			if (rect.xMin < mask.xMin)
			{
				rect.xMax = Mathf.Clamp(mask.xMin, rect.xMin, rect.xMax);
			}
			else
			{
				rect.xMin = Mathf.Clamp(mask.xMax, rect.xMin, rect.xMax);
			}
		}
	}

	private static Rect GetScreenRect(RectTransform rectTransform)
	{
		Vector3[] array = new Vector3[4];
		rectTransform.GetWorldCorners(array);
		array[3].y = (float)Screen.height - array[3].y;
		array[1].y = (float)Screen.height - array[1].y;
		return new Rect(array[1], array[3] - array[1]);
	}

	public static Vector2 GetCodeOffset(RectTransform inputFieldRect)
	{
		Vector3[] array = new Vector3[4];
		inputFieldRect.GetWorldCorners(array);
		return (array[3] - array[0]) * 0.5f + (array[0] - array[1]) * 0.5f;
	}
}
