using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodeCompleter : MonoBehaviour
{
	[SerializeField]
	private RectTransform container;

	[SerializeField]
	private CodeOption optionPrefab;

	[SerializeField]
	private Canvas editorCanvas;

	private List<CodeOption> options = new List<CodeOption>();

	private List<string> optionStrings;

	private int selected;

	private string compareString;

	public int startStringIndex;

	private CodeWindow func;

	public bool IsOpen => base.gameObject.activeInHierarchy;

	public string Selection => ((TMP_Text)options[selected].text).text;

	public bool IsMatch
	{
		get
		{
			int num = 0;
			string selection = Selection;
			foreach (char c in selection)
			{
				if (num >= compareString.Length)
				{
					return true;
				}
				if (char.ToLower(c) == char.ToLower(compareString[num]))
				{
					num++;
				}
			}
			return false;
		}
	}

	public int TypedLength => compareString.Length;

	private void OnEnable()
	{
		ThemeManager.Inst.OnThemeChanged += OnThemeChanged;
	}

	private void OnDisable()
	{
		ThemeManager.Inst.OnThemeChanged -= OnThemeChanged;
	}

	private void OnThemeChanged(ColorTheme theme)
	{
		if (TryGetComponent<Image>(out var component))
		{
			component.color = theme.code.CompleteBackgroundColor;
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			if (options.Count > selected + 1)
			{
				SelectOption(selected + 1);
			}
		}
		else if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			if (selected > 0)
			{
				SelectOption(selected - 1);
			}
		}
		else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
		{
			Close();
		}
	}

	public void SelectOption(int index)
	{
		if (options.Count != 0)
		{
			options[selected].Unhighlight();
			selected = index;
			options[index].Highlight();
			float y = ((RectTransform)options[selected].transform).anchoredPosition.y;
			float num = GetComponent<RectTransform>().sizeDelta.y / 2f;
			float y2 = container.sizeDelta.y;
			container.anchoredPosition = new Vector2(0f, Mathf.Clamp(0f - y - num, 0f, y2 - 2f * num));
		}
	}

	public void Commit()
	{
		func.CompleteWord(removeExtraChar: false);
		func.CodeInput.Select();
		Close();
	}

	public void UpdateString(char addition)
	{
		compareString += addition;
		UpdateOptions();
	}

	public void Backspace()
	{
		if (compareString.Length > 0)
		{
			compareString = compareString.Remove(compareString.Length - 1);
			UpdateOptions();
		}
	}

	public void Scroll(float scroll)
	{
		container.anchoredPosition += new Vector2(0f, (0f - scroll) * 100f);
	}

	public void Open(Vector3 pos, List<string> optionStrings, string compareString, int startStringIndex, CodeWindow func)
	{
		if (optionStrings.Count != 0)
		{
			base.gameObject.SetActive(value: true);
			RectTransform rectTransform = (RectTransform)base.transform;
			rectTransform.position = pos;
			rectTransform.anchoredPosition += new Vector2(rectTransform.rect.size.x, 0f - rectTransform.rect.size.y) / 2f;
			this.compareString = compareString;
			this.optionStrings = optionStrings;
			this.startStringIndex = startStringIndex;
			this.func = func;
			func.CodeInput.blockArrowKeys = true;
			UpdateOptions();
		}
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
		func.CodeInput.blockArrowKeys = false;
	}

	public TooltipInfo GetCompletionTooltip(int optionIndex)
	{
		return TooltipUtils.GetWordTooltip(optionStrings[optionIndex], func);
	}

	private void UpdateOptions()
	{
		optionStrings = Enumerable.ToList<string>((IEnumerable<string>)Enumerable.OrderByDescending<string, float>((IEnumerable<string>)optionStrings, (Func<string, float>)((string s) => CompareScore(s))));
		int i = 0;
		foreach (CodeOption option in options)
		{
			if (i < optionStrings.Count)
			{
				((TMP_Text)option.text).text = optionStrings[i];
				option.Unhighlight();
				option.gameObject.SetActive(value: true);
			}
			else
			{
				option.gameObject.SetActive(value: false);
			}
			i++;
		}
		for (; i < optionStrings.Count; i++)
		{
			options.Add(UnityEngine.Object.Instantiate(optionPrefab, container));
			options[options.Count - 1].SetUp(options.Count - 1, this);
			((TMP_Text)options[options.Count - 1].text).text = optionStrings[i];
		}
		SelectOption(0);
	}

	private float CompareScore(string s)
	{
		int num = 0;
		int num2 = 0;
		float num3 = 0f;
		foreach (char c in s)
		{
			if (num >= compareString.Length)
			{
				break;
			}
			if (c == compareString[num])
			{
				num++;
				num3 += (float)(1 / (1 << num2));
			}
			else if (char.ToLower(c) == char.ToLower(compareString[num]))
			{
				num++;
				num3 += 0.5f / (float)(1 << num2);
			}
			else
			{
				num2++;
			}
		}
		if (num == s.Length)
		{
			num3 += 1f;
		}
		return num3;
	}
}
