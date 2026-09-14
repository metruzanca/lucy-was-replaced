using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CodeWindow : MonoBehaviour, ITooltipHandler
{
	[SerializeField]
	private float minWidth;

	[SerializeField]
	private float scrollingStartSize;

	[SerializeField]
	private Vector2 extraSize;

	private const float colorFadeDuration = 0.2f;

	private float fadeStartTime = -1f;

	private bool isFading;

	[SerializeField]
	private Image windowBackground;

	[SerializeField]
	private ScrollRect scrollView;

	public RectTransform scrollViewRect;

	[SerializeField]
	private CodeInputField codeInput;

	[SerializeField]
	private BreakPointPanel breakpointPanel;

	[SerializeField]
	private TextMeshProUGUI codeText;

	[SerializeField]
	private TMP_InputField fileNameText;

	[SerializeField]
	private ColoredButton executeButton;

	[SerializeField]
	private ColoredButton stepByStepButton;

	[SerializeField]
	private Image executeImg;

	[SerializeField]
	private Image stepByStepImg;

	[SerializeField]
	private Sprite executeSprite;

	[SerializeField]
	private Sprite stopSprite;

	[SerializeField]
	private Sprite stepByStepSprite;

	[SerializeField]
	private Sprite pauseSprite;

	[SerializeField]
	private ErrorMessage errorMessage;

	[SerializeField]
	private Color errorColor;

	public Workspace workspace;

	public bool isExecuting;

	public TokenStream tokens;

	public string fileName;

	private volatile string errorString;

	private volatile int errorStartIndex;

	private volatile int errorEndIndex;

	public ConcurrentDictionary<string, FunctionNode> parsedFunctions = new ConcurrentDictionary<string, FunctionNode>();

	public HashSet<string> tokenizedIdentifiers = new HashSet<string>();

	private volatile Program cachedProgram;

	private volatile bool dirty = true;

	private volatile ParseException parseException;

	private object parseLock = new object();

	private string undoState = "";

	private float undoChangeTime;

	private bool undoPreventNextChange;

	private int prevSelectionStart;

	private int prevSelectionEnd;

	private string prevText;

	private Action tooltipUpdateCallback;

	private int hoverWordStart;

	private int hoverWordEnd;

	private int updateSizeInXFrames;

	public CodeInputField CodeInput => codeInput;

	public CodeWindow DockedParent => GetComponent<Window>().dockedParent?.GetComponent<CodeWindow>();

	public CodeWindow DockedChild => GetComponent<Window>().DockedChild?.GetComponent<CodeWindow>();

	private void OnEnable()
	{
		ThemeManager.Inst.OnThemeChanged += OnThemeChanged;
	}

	private void OnDisable()
	{
		ThemeManager.Inst.OnThemeChanged -= OnThemeChanged;
	}

	private void Update()
	{
		if (!errorMessage.IsShowing() && errorString != null)
		{
			ShowError();
		}
		if (updateSizeInXFrames > 0)
		{
			if (updateSizeInXFrames == 1)
			{
				UpdateSize();
			}
			updateSizeInXFrames--;
		}
		UpdateFade();
		UpdateCodeTooltips();
	}

	private void OnThemeChanged(ColorTheme theme)
	{
		windowBackground.color = theme.ui.WindowFrameColor;
		theme.ui.text.ApplyTo(fileNameText);
		theme.code.text.ApplyTo(codeInput);
		((Graphic)(object)codeInput.textComponent).color = new Color(1f, 1f, 1f, 0f);
		((Graphic)(object)codeText).color = theme.code.text.TextColor;
		if (codeInput.TryGetComponent<Image>(out var component))
		{
			component.color = theme.code.BackgroundColor;
		}
		if (breakpointPanel.TryGetComponent<Image>(out var component2))
		{
			component2.color = theme.code.BackgroundColor;
		}
		if (scrollView.verticalScrollbar.handleRect.TryGetComponent<Image>(out var component3))
		{
			component3.color = theme.ui.ScrollbarColor;
		}
		((TMP_Text)codeText).text = CodeUtilities.SyntaxColor2(codeInput.text, ThemeManager.Inst.Theme);
	}

	private void UpdateCodeTooltips()
	{
		if (tooltipUpdateCallback == null)
		{
			return;
		}
		int num;
		int i = (num = TMP_TextUtilities.FindIntersectingCharacter(codeInput.textComponent, new Vector3(Input.mousePosition.x, Input.mousePosition.y), workspace.uiCam, true));
		while (num > 0 && (Helper.IsValidNameChar(codeInput.text[num - 1]) || codeInput.text[num - 1] == '.'))
		{
			num--;
		}
		for (; i + 1 < codeInput.text.Length && (Helper.IsValidNameChar(codeInput.text[i + 1]) || codeInput.text[i + 1] == '.'); i++)
		{
		}
		if (num != hoverWordStart || i != hoverWordEnd)
		{
			hoverWordStart = num;
			hoverWordEnd = i;
			TimerManager.StopTimer(CallTooltipUpdate);
			if (hoverWordStart < 0 || hoverWordEnd >= codeInput.text.Length)
			{
				CallTooltipUpdate();
			}
			else
			{
				TimerManager.StartTimer(CallTooltipUpdate, 0.4);
			}
		}
	}

	private void CallTooltipUpdate()
	{
		if (tooltipUpdateCallback != null)
		{
			tooltipUpdateCallback();
		}
	}

	public void PressExecuteOrStop()
	{
		workspace.activeWindow = GetComponent<Window>();
		if (MainSim.Inst.StepByStepMode)
		{
			MainSim.Inst.StepByStepMode = false;
			return;
		}
		if (!MainSim.Inst.IsExecuting())
		{
			Node node = Parse();
			if (node != null && node != null)
			{
				MainSim.Inst.StartMainExecution(this, node);
				return;
			}
		}
		MainSim.Inst.StopMainExecution();
	}

	public void PressStepByStepButton()
	{
		workspace.activeWindow = GetComponent<Window>();
		if (MainSim.Inst.StepByStepMode)
		{
			MainSim.Inst.NextExecutionStep();
			return;
		}
		if (!MainSim.Inst.IsExecuting())
		{
			PressExecuteOrStop();
		}
		MainSim.Inst.StepByStepMode = true;
	}

	public void StartStepByStepMode()
	{
		stepByStepImg.sprite = stepByStepSprite;
		executeImg.sprite = executeSprite;
	}

	public void StartExecutionMode(bool closeErrors = true)
	{
		executeButton.IsRunning = true;
		stepByStepButton.IsRunning = true;
		isExecuting = true;
		executeImg.sprite = stopSprite;
		stepByStepImg.sprite = pauseSprite;
		if (closeErrors)
		{
			CloseError();
		}
	}

	public void StopExecutionMode()
	{
		if (isExecuting)
		{
			executeButton.IsRunning = false;
			stepByStepButton.IsRunning = false;
			isExecuting = false;
			executeImg.sprite = executeSprite;
			stepByStepImg.sprite = stepByStepSprite;
		}
	}

	public void SetExecutionColor()
	{
		if (!(OptionHolder.GetString("code highlights", "enabled") == "disabled"))
		{
			fadeStartTime = Time.time;
			isFading = true;
		}
	}

	private void UpdateFade()
	{
		if (isFading)
		{
			ColorTheme theme = ThemeManager.Inst.Theme;
			float num = Time.time - fadeStartTime;
			if (num >= 0.2f)
			{
				windowBackground.color = theme.ui.WindowFrameColor;
				isFading = false;
			}
			else
			{
				windowBackground.color = Color.Lerp(theme.code.WindowFrameExecColor, theme.ui.WindowFrameColor, num / 0.2f);
			}
		}
	}

	public void Load(string code)
	{
		codeInput.onTextInserted.AddListener(OnCodeInserted);
		codeInput.onTextMeshUpdated.AddListener(OnCodeChanged);
		codeInput.onEndEdit.AddListener(OnCodeCommit);
		codeInput.onCarretMoved.AddListener(OnCarretMoved);
		codeInput.onBackSpace.AddListener(OnBackspace);
		codeInput.onSelect.AddListener(OnFocus);
		codeInput.onTextSelection.AddListener(OnTextSelection);
		codeInput.uiCam = workspace.uiCam;
		codeInput.codeCompleter = (RectTransform)workspace.codeCompleter.transform;
		codeInput.text = code;
		fileNameText.text = fileName;
		undoState = codeInput.text;
		undoChangeTime = Time.time;
	}

	public void PromptDelete()
	{
		if (isExecuting)
		{
			return;
		}
		string text = string.Format(Localizer.Localize("popup_warning_delete_files"), fileName);
		List<WarningPopup.ButtonData> buttonsToAdd = new List<WarningPopup.ButtonData>
		{
			new WarningPopup.ButtonData("delete", delegate
			{
				MainSim.Inst.warningPopup.Close();
				if (!isExecuting)
				{
					GetComponent<Window>().Close();
				}
			}),
			new WarningPopup.ButtonData("cancel", MainSim.Inst.warningPopup.Close)
		};
		MainSim.Inst.warningPopup.ShowPopup(text, buttonsToAdd);
	}

	public void OnNameTextEdited()
	{
		Rename(fileNameText.text);
	}

	public void Rename(string newName)
	{
		if (workspace.IsValidFileName(newName))
		{
			workspace.RenameWindow(GetComponent<Window>(), newName);
			fileName = newName;
			fileNameText.text = newName;
		}
		else
		{
			fileNameText.text = fileName;
		}
	}

	public List<CodeWindow> GetDockedChildren()
	{
		if (DockedChild == null)
		{
			return new List<CodeWindow>();
		}
		List<CodeWindow> dockedChildren = DockedChild.GetDockedChildren();
		dockedChildren.Add(DockedChild);
		return dockedChildren;
	}

	public void SetMinimized()
	{
		_ = GetComponent<Window>().isMinimized;
		errorMessage.Close();
		updateSizeInXFrames = 2;
	}

	public bool IsPointerOverCodeInput()
	{
		if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)codeInput.textViewport.transform, (Vector2)Input.mousePosition, workspace.uiCam))
		{
			return RectTransformUtility.RectangleContainsScreenPoint(scrollViewRect, (Vector2)Input.mousePosition, workspace.uiCam);
		}
		return false;
	}

	public void OnCodeCommit(string s)
	{
		Parse();
	}

	private void OnCodeChanged()
	{
		UpdateTokens();
		UpdateSize();
		int stringPosition = Mathf.Clamp(codeInput.stringPosition - 1, 0, codeInput.text.Length);
		ScrollToStringPosition(stringPosition);
	}

	public void ScrollToStringPosition(int stringPosition)
	{
		TMP_TextInfo textInfo = codeInput.textComponent.textInfo;
		if (textInfo.characterCount != 0 && stringPosition < textInfo.characterCount)
		{
			float y = textInfo.characterInfo[stringPosition].bottomLeft.y;
			y -= ((RectTransform)((TMP_Text)codeText).transform).rect.height / 2f;
			y = 0f - y;
			RectTransform rectTransform = (RectTransform)codeInput.transform;
			float y2 = rectTransform.anchoredPosition.y;
			float height = scrollViewRect.rect.height;
			float lineHeight = textInfo.lineInfo[0].lineHeight;
			if (y < y2 + 30f)
			{
				rectTransform.anchoredPosition = new Vector2(0f, Mathf.Max(y - 30f, 0f));
			}
			else if (y > y2 + height - (70f + lineHeight))
			{
				rectTransform.anchoredPosition = new Vector2(0f, Mathf.Min(y - height + (70f + lineHeight), Mathf.Max(rectTransform.rect.height - height, 0f)));
			}
		}
	}

	public void Scroll(float scroll)
	{
		((RectTransform)codeInput.transform).anchoredPosition += new Vector2(0f, scroll * -500f);
	}

	private void UpdateSize()
	{
		Window component = GetComponent<Window>();
		Vector2 vector = codeInput.textComponent.GetRenderedValues(false) + extraSize;
		component.minSize.x = Mathf.Max(vector.x, minWidth, 0f);
		component.automaticSize.y = Mathf.Min(vector.y, scrollingStartSize);
		component.UpdateSize();
		UpdateCodeInputSize();
		if (errorMessage.IsShowing())
		{
			ShowError();
		}
	}

	public void UpdateCodeInputSize()
	{
		Window component = GetComponent<Window>();
		codeInput.GetComponent<LayoutElement>().minHeight = Mathf.Max(component.minSize.y, component.playerSetSize.y) - 70f;
	}

	private void OnCarretMoved()
	{
		bool flag = RectTransformUtility.RectangleContainsScreenPoint((RectTransform)workspace.codeCompleter.transform, (Vector2)Input.mousePosition, workspace.uiCam);
		if (workspace.codeCompleter.IsOpen && !flag)
		{
			workspace.codeCompleter.Close();
			codeInput.blockArrowKeys = false;
		}
	}

	private void OnTextSelection(string s, int start, int end)
	{
		prevSelectionEnd = Mathf.Max(start, end);
		prevSelectionStart = Mathf.Min(start, end);
	}

	private void OnFocus(string code)
	{
		CloseError();
		GetComponent<Window>().MoveToFront();
		workspace.activeWindow = GetComponent<Window>();
	}

	private void OnBackspace()
	{
		if (workspace.codeCompleter.IsOpen && codeInput.stringPosition > 0 && !char.IsLetterOrDigit(codeInput.text[codeInput.stringPosition - 1]))
		{
			workspace.codeCompleter.Close();
		}
		else if (workspace.codeCompleter.IsOpen)
		{
			workspace.codeCompleter.Backspace();
		}
	}

	private void OnCodeInserted(string inserted)
	{
		if (inserted.Contains('\r'))
		{
			codeInput.text = codeInput.text.Replace("\r", "");
		}
		if (isExecuting)
		{
			return;
		}
		workspace?.tooltip?.CloseTooltip();
		CodeCompleter codeCompleter = workspace.codeCompleter;
		if (inserted.Length == 1 && (Helper.IsValidNameChar(inserted[0]) || inserted[0] == '.'))
		{
			if (codeCompleter.IsOpen && inserted[0] != '.')
			{
				codeCompleter.UpdateString(inserted[0]);
			}
			else if (((char.IsLetter(inserted[0]) || inserted[0] == '_') && (codeInput.stringPosition <= 1 || !Helper.IsValidNameChar(codeInput.text[codeInput.stringPosition - 2]))) || inserted[0] == '.')
			{
				Vector3 bottomLeft = codeInput.textComponent.textInfo.characterInfo[codeInput.stringPosition - 1].bottomLeft;
				Vector3 vector = BlinkManager.GetCodeOffset(codeInput.GetComponent<RectTransform>());
				bottomLeft = codeInput.transform.TransformPoint(bottomLeft) + vector;
				if (inserted[0] == '.')
				{
					codeCompleter.Open(bottomLeft, GetSubWordList(GetWordBefore(codeInput.stringPosition - 2)), "", codeInput.stringPosition, this);
				}
				else if (codeInput.stringPosition >= 2 && codeInput.text[codeInput.stringPosition - 2] == '.')
				{
					codeCompleter.Open(bottomLeft, GetSubWordList(GetWordBefore(codeInput.stringPosition - 3)), inserted, codeInput.stringPosition - 1, this);
				}
				else if (IsImportStatement(codeInput.text, codeInput.stringPosition - 1))
				{
					codeCompleter.Open(bottomLeft, Enumerable.ToList<string>(Enumerable.Select<CodeWindow, string>((IEnumerable<CodeWindow>)workspace.codeWindows.Values, (Func<CodeWindow, string>)((CodeWindow cw) => cw.fileName))), inserted, codeInput.stringPosition - 1, this);
				}
				else
				{
					codeCompleter.Open(bottomLeft, GetWordList(), inserted, codeInput.stringPosition - 1, this);
				}
			}
		}
		else if (codeCompleter.IsOpen && inserted.Length == 1 && (inserted[0] == '\t' || inserted[0] == '\n'))
		{
			if (codeCompleter.IsMatch || inserted[0] == '\t')
			{
				CompleteWord(removeExtraChar: true);
			}
			codeCompleter.Close();
		}
		else if (workspace.codeCompleter.IsOpen)
		{
			codeCompleter.Close();
		}
		if (prevText.Length > 0 && (OptionHolder.GetKeyCombination("indent selection").IsKeyPressed(pressedRightThisFrame: true) || OptionHolder.GetKeyCombination("unindent selection").IsKeyPressed(pressedRightThisFrame: true)))
		{
			List<int> list = new List<int>();
			bool flag = prevText.Length > codeInput.text.Length;
			if (flag)
			{
				int num = prevSelectionEnd - 1;
				while (num >= 0 && (num >= prevSelectionStart || prevText[num] != '\n'))
				{
					if (prevText[num] == '\n')
					{
						list.Add(num);
					}
					num--;
				}
				list.Add(num);
			}
			else
			{
				int num2 = codeInput.stringPosition - 2;
				while (num2 >= 0 && prevText[num2] != '\n')
				{
					num2--;
				}
				list.Add(num2);
			}
			bool flag2 = OptionHolder.GetKeyCombination("unindent selection").IsKeyPressed(pressedRightThisFrame: true);
			if (list.Count > 1 || flag2)
			{
				StringBuilder stringBuilder = new StringBuilder(prevText);
				int num3 = 0;
				bool flag3 = false;
				foreach (int item in list)
				{
					if (flag2)
					{
						if (stringBuilder.Length > item + 1 && stringBuilder[item + 1] == '\t')
						{
							stringBuilder.Remove(item + 1, 1);
							num3++;
						}
						else if (stringBuilder.Length > item + 4 && stringBuilder[item + 1] == ' ' && stringBuilder[item + 2] == ' ' && stringBuilder[item + 3] == ' ' && stringBuilder[item + 4] == ' ')
						{
							stringBuilder.Remove(item + 1, 4);
							num3 += 4;
							flag3 = true;
						}
					}
					else
					{
						bool flag4 = OptionHolder.GetString("tabs to spaces") == "enabled";
						stringBuilder.Insert(item + 1, flag4 ? "    " : "\t");
						num3 += ((!flag4) ? 1 : 4);
					}
				}
				int stringPosition = codeInput.stringPosition;
				codeInput.text = stringBuilder.ToString();
				if (flag2)
				{
					if (flag)
					{
						codeInput.stringPosition = prevSelectionEnd - num3;
						codeInput.selectionStringFocusPosition = prevSelectionStart - ((!flag3) ? 1 : 4);
					}
					else
					{
						codeInput.stringPosition = stringPosition - (1 + num3);
					}
				}
				else
				{
					codeInput.stringPosition = prevSelectionEnd + num3;
					codeInput.selectionStringFocusPosition = prevSelectionStart + 1;
				}
				if (workspace.codeCompleter.IsOpen)
				{
					codeCompleter.Close();
				}
				UpdateTokens();
				return;
			}
		}
		if (inserted.Contains('\r'))
		{
			codeInput.text = codeInput.text.Replace("\r", "");
		}
		UpdateTokens();
		int num4 = codeInput.stringPosition - 1;
		if (num4 > 0 && codeInput.text[num4] == '\n')
		{
			Token lastNewLine = tokens.GetLastNewLine(num4);
			string text = ((lastNewLine != null) ? lastNewLine.value.Remove(0, 1) : "\t");
			if (num4 - 1 > 0 && codeInput.text[num4 - 1] == ':' && lastNewLine != null)
			{
				text += (text.StartsWith(' ') ? "    " : ((object)'\t'));
			}
			codeInput.text = codeInput.text.Insert(num4 + 1, text);
			codeInput.stringPosition += text.Length;
		}
	}

	public void OpenCodeCompleterAtCarret()
	{
		if (codeInput.text[codeInput.stringPosition - 1] == ' ')
		{
			codeInput.text = codeInput.text.Remove(codeInput.stringPosition - 1, 1);
			codeInput.stringPosition--;
		}
		CodeCompleter codeCompleter = workspace.codeCompleter;
		int num = Mathf.Max(0, codeInput.stringPosition - 1);
		if (codeInput.text[num] == '.')
		{
			Vector3 bottomLeft = codeInput.textComponent.textInfo.characterInfo[num].bottomLeft;
			Vector3 vector = BlinkManager.GetCodeOffset(codeInput.GetComponent<RectTransform>());
			bottomLeft = codeInput.transform.TransformPoint(bottomLeft) + vector;
			codeCompleter.Open(bottomLeft, GetSubWordList(GetWordBefore(num - 1)), "", num + 1, this);
			return;
		}
		int num2 = num;
		while (num2 > 0 && Helper.IsValidNameChar(codeInput.text[num2]) && Helper.IsValidNameChar(codeInput.text[num2 - 1]))
		{
			num2--;
		}
		Vector3 bottomLeft2 = codeInput.textComponent.textInfo.characterInfo[num2].bottomLeft;
		Vector3 vector2 = BlinkManager.GetCodeOffset(codeInput.GetComponent<RectTransform>());
		bottomLeft2 = codeInput.transform.TransformPoint(bottomLeft2) + vector2;
		if (num2 != num && num2 > 0 && codeInput.text[num2 - 1] == '.')
		{
			codeCompleter.Open(bottomLeft2, GetSubWordList(GetWordBefore(num2 - 2)), codeInput.text.Substring(num2, num - num2), num2, this);
		}
		else
		{
			codeCompleter.Open(bottomLeft2, GetWordList(), codeInput.text.Substring(num2, num - num2), num2, this);
		}
	}

	private bool IsImportStatement(string text, int stringIndex)
	{
		int num = stringIndex - 1;
		while (num >= 0 && char.IsWhiteSpace(text[num]))
		{
			num--;
		}
		if (num < 0)
		{
			return false;
		}
		int num2 = num;
		while (num >= 0 && char.IsLetterOrDigit(text[num]))
		{
			num--;
		}
		int num3 = num + 1;
		int num4 = num2 - num;
		bool num5 = num4 == 6 && text[num3] == 'i' && text[num3 + 1] == 'm' && text[num3 + 2] == 'p' && text[num3 + 3] == 'o' && text[num3 + 4] == 'r' && text[num3 + 5] == 't';
		bool flag = num4 == 4 && text[num3] == 'f' && text[num3 + 1] == 'r' && text[num3 + 2] == 'o' && text[num3 + 3] == 'm';
		if (num5)
		{
			num = num3 - 1;
			while (num >= 0 && char.IsWhiteSpace(text[num]))
			{
				num--;
			}
			while (num >= 0 && char.IsLetterOrDigit(text[num]))
			{
				num--;
			}
			while (num >= 0 && char.IsWhiteSpace(text[num]))
			{
				num--;
			}
			if (num < 0)
			{
				return true;
			}
			int num6 = num;
			while (num >= 0 && char.IsLetterOrDigit(text[num]))
			{
				num--;
			}
			int num7 = num + 1;
			if (num6 - num == 4 && text[num7] == 'f' && text[num7 + 1] == 'r' && text[num7 + 2] == 'o' && text[num7 + 3] == 'm')
			{
				return false;
			}
			return true;
		}
		if (flag)
		{
			return true;
		}
		return false;
	}

	public void CompleteWord(bool removeExtraChar)
	{
		CodeCompleter codeCompleter = workspace.codeCompleter;
		bool flag = codeInput.stringPosition == codeInput.text.Length;
		string text = codeInput.text;
		int startStringIndex = codeCompleter.startStringIndex;
		text = text.Remove(startStringIndex, codeCompleter.TypedLength + (removeExtraChar ? 1 : 0));
		text = text.Insert(startStringIndex, codeCompleter.Selection);
		codeInput.text = text;
		codeInput.stringPosition += codeCompleter.Selection.Length - ((removeExtraChar && !flag) ? 1 : 0) - codeCompleter.TypedLength;
	}

	public void UpdateSearch(string word, int selectedIndex = -1)
	{
		((TMP_Text)codeText).text = CodeUtilities.SyntaxColor2(codeInput.text, ThemeManager.Inst.Theme, word, selectedIndex);
	}

	private void ConvertSpacesToTabs()
	{
		int num = 0;
		bool flag = true;
		List<int> list = null;
		for (int i = 0; i < codeInput.text.Length; i++)
		{
			if (codeInput.text[i] == ' ' && flag)
			{
				num++;
				if (num == 4)
				{
					num = 0;
					codeInput.stringPosition -= 3;
					if (list == null)
					{
						list = new List<int>();
					}
					list.Add(i - 3);
				}
			}
			else if (codeInput.text[i] == '\n' || codeInput.text[i] == '\t')
			{
				num = 0;
				flag = true;
			}
			else
			{
				num = 0;
				flag = false;
			}
		}
		if (list == null)
		{
			return;
		}
		list.Reverse();
		StringBuilder stringBuilder = new StringBuilder(codeInput.text);
		foreach (int item in list)
		{
			stringBuilder.Remove(item, 4);
			stringBuilder.Insert(item, '\t');
		}
		codeInput.text = stringBuilder.ToString();
	}

	private bool UpdateTokens()
	{
		if (codeInput.text == prevText)
		{
			return false;
		}
		if (isExecuting)
		{
			MainSim.Inst.StopMainExecution();
		}
		if (OptionHolder.GetString("tabs to spaces") == "enabled" && codeInput.text.Contains('\t'))
		{
			int num = Enumerable.Count<char>(Enumerable.Take<char>((IEnumerable<char>)codeInput.text, codeInput.stringPosition + 1), (Func<char, bool>)((char c) => c == '\t'));
			codeInput.text = codeInput.text.Replace("\t", "    ");
			codeInput.stringPosition += num * 3;
		}
		else if (OptionHolder.GetString("tabs to spaces") == "disabled")
		{
			ConvertSpacesToTabs();
		}
		prevText = codeInput.text;
		dirty = true;
		((TMP_Text)codeText).text = CodeUtilities.SyntaxColor2(codeInput.text, ThemeManager.Inst.Theme);
		Tokenizer.Tokenize(codeInput.text, out tokens);
		updateTokenizedIdentifiers(tokens);
		if (undoPreventNextChange)
		{
			undoPreventNextChange = false;
		}
		else
		{
			if (Time.time - undoChangeTime > 0.5f)
			{
				workspace.undoHistory.AddChange(SetToCodeState, undoState, base.gameObject);
				undoState = codeInput.text;
			}
			undoChangeTime = Time.time;
		}
		return true;
		void updateTokenizedIdentifiers(TokenStream t)
		{
			tokenizedIdentifiers.Clear();
			foreach (Token item in t)
			{
				if (item.type == TokenType.IDENTIFIER)
				{
					tokenizedIdentifiers.Add(item.value);
				}
			}
		}
	}

	private object SetToCodeState(object state)
	{
		string text = codeInput.text;
		undoPreventNextChange = true;
		codeInput.text = (string)state;
		if (!codeInput.isFocused)
		{
			Parse();
		}
		undoState = codeInput.text;
		return text;
	}

	public Node Parse()
	{
		lock (parseLock)
		{
			UpdateTokens();
			if (!dirty)
			{
				if (parseException != null)
				{
					errorString = parseException.Message;
					errorStartIndex = parseException.startIndex;
					errorEndIndex = parseException.endIndex;
				}
				return cachedProgram?.syntaxTree;
			}
			MainSim.Inst.dirty = true;
			dirty = false;
			parsedFunctions.Clear();
			try
			{
				cachedProgram = Parser.Parse(tokens, this);
				cachedProgram.syntaxTree.CheckForNonsensicalCode();
				breakpointPanel.UpdateBreakpoints(cachedProgram.syntaxTree);
				parseException = null;
				return cachedProgram?.syntaxTree;
			}
			catch (ParseException ex)
			{
				errorString = ex.Message;
				errorStartIndex = ex.startIndex;
				errorEndIndex = ex.endIndex;
				parseException = ex;
				cachedProgram = null;
				return null;
			}
		}
	}

	public void SetErrorMessage(string message, int startIndex, int endIndex)
	{
		errorString = message;
		errorStartIndex = startIndex;
		errorEndIndex = endIndex;
	}

	private void ShowError()
	{
		if (!(EventSystem.current.currentSelectedGameObject == codeInput.gameObject))
		{
			Vector3 vector;
			Vector3 right;
			if (GetComponent<Window>().isMinimized)
			{
				errorMessage.transform.SetParent(base.transform);
				Rect rect = ((RectTransform)base.transform).rect;
				vector = base.transform.TransformPoint(new Vector3((rect.xMin + rect.xMax) / 2f, rect.yMin + 10f, 0f));
				right = vector;
				workspace.MoveCameraTo(GetComponent<Window>());
			}
			else
			{
				errorMessage.transform.SetParent(codeInput.transform);
				TMP_CharacterInfo[] characterInfo = codeInput.textComponent.textInfo.characterInfo;
				Vector3 vector2 = BlinkManager.GetCodeOffset(codeInput.GetComponent<RectTransform>());
				ScrollToStringPosition(errorStartIndex);
				vector = codeInput.transform.TransformPoint(characterInfo[errorStartIndex].bottomLeft) + vector2;
				right = codeInput.transform.TransformPoint(characterInfo[errorEndIndex].bottomRight) + vector2;
				workspace.MoveCameraTo(GetComponent<Window>(), alwaysMoveWindow: false, errorStartIndex, codeInput);
			}
			errorMessage.ShowError(errorString, vector, right);
		}
	}

	private void CloseError()
	{
		errorString = null;
		errorMessage.Close();
	}

	private List<string> GetWordList()
	{
		HashSet<string> hashSet = Enumerable.ToHashSet<string>(Enumerable.Where<string>((IEnumerable<string>)BuiltinFunctions.Functions.Keys, (Func<string, bool>)((string x) => MainSim.Inst.IsUnlocked(x) && x != "tap")));
		hashSet.UnionWith((IEnumerable<string>)MainSim.Inst.GetUnlockedKeywords());
		hashSet.UnionWith((IEnumerable<string>)tokenizedIdentifiers);
		if (cachedProgram != null)
		{
			hashSet.UnionWith((IEnumerable<string>)cachedProgram.allVars);
			foreach (string importedModule in cachedProgram.importedModules)
			{
				foreach (CodeWindow value in workspace.codeWindows.Values)
				{
					if (value.fileName == importedModule && value.cachedProgram != null)
					{
						hashSet.UnionWith((IEnumerable<string>)value.cachedProgram.allVars);
					}
				}
			}
		}
		List<string> list = Enumerable.ToList<string>((IEnumerable<string>)hashSet);
		list.Add("__name__");
		list.Sort();
		return list;
	}

	private List<string> GetSubWordList(string domain)
	{
		IEnumerable<string> enumerable = domain switch
		{
			"Entities" => Enumerable.Where<string>(Enumerable.Select<FarmObjectSO, string>(Enumerable.Where<FarmObjectSO>(ResourceManager.GetAllFarmObjects(), (Func<FarmObjectSO, bool>)((FarmObjectSO f) => !f.isGround)), (Func<FarmObjectSO, string>)((FarmObjectSO f) => f.objectName)), (Func<string, bool>)((string s) => MainSim.Inst.IsUnlocked(s))), 
			"Grounds" => Enumerable.Where<string>(Enumerable.Select<FarmObjectSO, string>(Enumerable.Where<FarmObjectSO>(ResourceManager.GetAllFarmObjects(), (Func<FarmObjectSO, bool>)((FarmObjectSO f) => f.isGround)), (Func<FarmObjectSO, string>)((FarmObjectSO f) => f.objectName)), (Func<string, bool>)((string s) => MainSim.Inst.IsUnlocked(s))), 
			"Items" => Enumerable.Select<ItemSO, string>(Enumerable.Where<ItemSO>(ResourceManager.GetAllItems(), (Func<ItemSO, bool>)((ItemSO i) => MainSim.Inst.IsUnlocked(i.itemName) && i.enabled)), (Func<ItemSO, string>)((ItemSO i) => i.itemName)), 
			"Unlocks" => Enumerable.Select<UnlockSO, string>(Enumerable.Where<UnlockSO>(ResourceManager.GetAllUnlocks(), (Func<UnlockSO, bool>)((UnlockSO u) => u.enabled)), (Func<UnlockSO, string>)((UnlockSO u) => u.unlockName)), 
			"Leaderboards" => Enumerable.Select<LeaderboardSO, string>(ResourceManager.GetAllLeaderboards(), (Func<LeaderboardSO, string>)((LeaderboardSO l) => l.leaderboardName)), 
			"Hats" => Enumerable.Select<HatSO, string>(Enumerable.Where<HatSO>(ResourceManager.GetAllHats(), (Func<HatSO, bool>)((HatSO h) => MainSim.Inst.IsUnlocked(h.hatName) && !h.hidden)), (Func<HatSO, string>)((HatSO h) => h.hatName)), 
			_ => null, 
		};
		if (enumerable == null)
		{
			bool flag = false;
			HashSet<string> hashSet = new HashSet<string>();
			foreach (CodeWindow value in workspace.codeWindows.Values)
			{
				if (value.fileName == domain && value.cachedProgram != null)
				{
					hashSet.UnionWith((IEnumerable<string>)value.cachedProgram.allVars);
					flag = true;
				}
			}
			if (!flag)
			{
				hashSet.UnionWith((IEnumerable<string>)Enumerable.ToHashSet<string>(Enumerable.Where<string>((IEnumerable<string>)BuiltinFunctions.Methods.Keys, (Func<string, bool>)((string s) => MainSim.Inst.IsUnlocked(s)))));
			}
			return Enumerable.ToList<string>((IEnumerable<string>)hashSet);
		}
		if (enumerable != null)
		{
			return Enumerable.ToList<string>(Enumerable.Select<string, string>(enumerable, (Func<string, string>)CodeUtilities.ToUpperSnake));
		}
		return new List<string>();
	}

	private string GetWordBefore(int stringPos)
	{
		int num = stringPos;
		while (num >= 0 && codeInput.text.Length > num && Helper.IsValidNameChar(codeInput.text[num]))
		{
			num--;
		}
		return codeInput.text.Substring(num + 1, stringPos - num);
	}

	public TooltipInfo GetTooltipInfo(Action updateTooltipCallback)
	{
		tooltipUpdateCallback = updateTooltipCallback;
		if (hoverWordStart < 0 || hoverWordEnd >= CodeInput.text.Length)
		{
			return null;
		}
		string word = ((codeInput.text == "") ? "" : codeInput.text.Substring(hoverWordStart, hoverWordEnd - hoverWordStart + 1));
		TooltipInfo tooltipInfo = TooltipUtils.GetWordTooltip(word, this);
		if (MainSim.Inst.EvaluateName(word, out var value))
		{
			string text = CodeUtilities.ToNiceString(value);
			tooltipInfo = new TooltipInfo("`" + text + "`");
		}
		if (tooltipInfo != null)
		{
			Vector3 bottomLeft = codeInput.textComponent.textInfo.characterInfo[hoverWordStart].bottomLeft;
			Vector3 vector = BlinkManager.GetCodeOffset(codeInput.GetComponent<RectTransform>());
			Vector3 fixedPosition = codeInput.transform.TransformPoint(bottomLeft) + vector;
			tooltipInfo.fixedPosition = fixedPosition;
			tooltipInfo.anchor = TooltipInfo.Anchor.BottomRight;
			tooltipInfo.delay = 0.3f;
		}
		return tooltipInfo;
	}

	public void TooltipGone()
	{
		tooltipUpdateCallback = null;
	}
}
