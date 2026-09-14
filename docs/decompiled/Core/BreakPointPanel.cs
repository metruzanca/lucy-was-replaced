using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BreakPointPanel : MonoBehaviour, IPointerDownHandler, IEventSystemHandler
{
	[SerializeField]
	private CodeWindow function;

	[SerializeField]
	private float topMargin;

	[SerializeField]
	private float bottomMargin;

	[SerializeField]
	private RawImage borderLine;

	[SerializeField]
	private GameObject breakpointPrefab;

	private List<int> breakpointLines = new List<int>();

	private Dictionary<int, GameObject> breakpoints = new Dictionary<int, GameObject>();

	private int prevLineCount;

	private volatile Node syntaxTree;

	private void OnEnable()
	{
		ThemeManager.Inst.OnThemeChanged += OnThemeChanged;
	}

	private void OnDisable()
	{
		ThemeManager.Inst.OnThemeChanged -= OnThemeChanged;
	}

	private void Start()
	{
		function.CodeInput.onValueChanged.AddListener(UpdatePanel);
		UpdatePanel(function.CodeInput.text);
	}

	private void OnThemeChanged(ColorTheme theme)
	{
		borderLine.color = theme.code.BreakpointLineColor;
		foreach (GameObject value in breakpoints.Values)
		{
			if (value.TryGetComponent<Image>(out var component))
			{
				component.color = theme.code.BreakpointColor;
			}
		}
	}

	private void UpdatePanel(string value)
	{
		if (breakpointLines.Count == 0)
		{
			return;
		}
		if (Enumerable.Count<char>((IEnumerable<char>)value, (Func<char, bool>)((char c) => c == '\n')) + 1 != prevLineCount)
		{
			foreach (int breakpointLine in breakpointLines)
			{
				UnityEngine.Object.Destroy(breakpoints[breakpointLine]);
			}
			breakpointLines.Clear();
			breakpoints.Clear();
		}
		_ = prevLineCount;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (!MainSim.Inst.IsUnlocked("debug"))
		{
			return;
		}
		int num = Enumerable.Count<char>((IEnumerable<char>)function.CodeInput.text, (Func<char, bool>)((char c) => c == '\n')) + 1;
		RectTransform obj = (RectTransform)base.transform;
		float preferredHeight = function.CodeInput.textComponent.preferredHeight;
		float num2 = preferredHeight / (float)num;
		Vector2 vector = default(Vector2);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(obj, eventData.position, eventData.pressEventCamera, ref vector);
		int num3 = Mathf.FloorToInt((0f - vector.y - topMargin) / preferredHeight * (float)num);
		if (num3 < 0 || num3 >= num)
		{
			return;
		}
		if (breakpointLines.Contains(num3))
		{
			breakpointLines.Remove(num3);
			UnityEngine.Object.Destroy(breakpoints[num3]);
			breakpoints.Remove(num3);
		}
		else
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(breakpointPrefab, base.transform);
			RectTransform obj2 = (RectTransform)gameObject.transform;
			float y = (0f - num2) * (float)num3 - 0.5f * num2 - topMargin;
			obj2.anchoredPosition += new Vector2(0f, y);
			if (gameObject.TryGetComponent<Image>(out var component))
			{
				component.color = ThemeManager.Inst.Theme.code.BreakpointColor;
			}
			breakpointLines.Add(num3);
			breakpointLines.Sort();
			breakpoints[num3] = gameObject;
		}
		UpdateBreakpoints(syntaxTree);
	}

	public void UpdateBreakpoints(Node syntaxTree)
	{
		this.syntaxTree = syntaxTree;
		TMP_LineInfo[] lineInfos = function.CodeInput.textComponent.textInfo.lineInfo;
		syntaxTree?.InsertBreakpointsRec(Enumerable.ToHashSet<int>(Enumerable.Select<int, int>((IEnumerable<int>)breakpointLines, (Func<int, int>)((int i) => lineInfos[i].firstCharacterIndex))));
	}
}
