using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MarkdownText : MonoBehaviour, IPointerMoveHandler, IEventSystemHandler, IPointerExitHandler
{
	private enum SectionType
	{
		Text,
		Image,
		Custom
	}

	private struct TextSection
	{
		public SectionType type;

		public string text;

		public bool isSpoiler;

		public string spoilerText;

		public TextSection(SectionType type, string text, bool isSpoiler, string spoilerText = null)
		{
			this.type = type;
			this.text = text;
			this.isSpoiler = isSpoiler;
			this.spoilerText = spoilerText;
		}
	}

	private struct HoverInfo
	{
		public CodeInputField inputField;

		public string linkID;

		public int startIndex;
	}

	[SerializeField]
	private SpoilerWarning spoilerWarning;

	[SerializeField]
	private CodeInputField textPrefab;

	[SerializeField]
	private Image imgPrefab;

	[SerializeField]
	private VideoUI videoPrefab;

	[SerializeField]
	private OutputText outputPrefab;

	[SerializeField]
	private Inventory invPrefab;

	private List<CodeInputField> textFields = new List<CodeInputField>();

	private Action<string> clickLinkCallback;

	private HoverInfo hoverInfo;

	public string BasePath { get; set; }

	private string ChangeColorBeforeIndex(string text, string color, int index)
	{
		int num = index;
		while (num > 0 && text[num] != '#')
		{
			num--;
		}
		return text.Remove(num, 9).Insert(num, color);
	}

	public void OnPointerMove(PointerEventData eventData)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		HoverInfo hoverInfo = default(HoverInfo);
		foreach (CodeInputField textField in textFields)
		{
			int num = TMP_TextUtilities.FindIntersectingLink(textField.textComponent, Input.mousePosition, MainSim.Inst.workspace.uiCam);
			if (num >= 0)
			{
				TMP_LinkInfo val = textField.textComponent.textInfo.linkInfo[num];
				hoverInfo.startIndex = val.linkIdFirstCharacterIndex;
				hoverInfo.inputField = textField;
				hoverInfo.linkID = ((TMP_LinkInfo)(ref val)).GetLinkID();
				break;
			}
		}
		if (hoverInfo.startIndex != this.hoverInfo.startIndex)
		{
			ColorTheme theme = ThemeManager.Inst.Theme;
			if (this.hoverInfo.linkID != null)
			{
				this.hoverInfo.inputField.text = ChangeColorBeforeIndex(this.hoverInfo.inputField.text, theme.docs.link, this.hoverInfo.startIndex);
			}
			this.hoverInfo = hoverInfo;
			if (this.hoverInfo.linkID != null)
			{
				this.hoverInfo.inputField.text = ChangeColorBeforeIndex(this.hoverInfo.inputField.text, theme.docs.link_hover, this.hoverInfo.startIndex);
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		hoverInfo = default(HoverInfo);
	}

	public void Update()
	{
		if (!string.IsNullOrEmpty(hoverInfo.linkID) && Input.GetKeyDown(KeyCode.Mouse0))
		{
			clickLinkCallback(hoverInfo.linkID);
		}
	}

	public void TriggerLinkClick(string link)
	{
		clickLinkCallback?.Invoke(link);
	}

	public int UpdateSearch(string searchTerm, int occurenceIndex)
	{
		int count = 0;
		foreach (CodeInputField textField in textFields)
		{
			if (string.IsNullOrEmpty(searchTerm))
			{
				textField.text = CodeUtilities.RemoveMarks(textField.text);
			}
			else
			{
				textField.text = CodeUtilities.MarkSearch(CodeUtilities.RemoveMarks(textField.text), searchTerm, occurenceIndex, ref count);
			}
		}
		return count;
	}

	public static string ProcessMetadata(string text, Dictionary<string, string> metadata)
	{
		Match match = new Regex("<meta\\s+((?:\"(?:[^\"\\\\]|\\\\\"|\\\\\\\\)*\"|[^\">])*?)\\/?>\\s*").Match(text);
		if (!match.Success)
		{
			return text;
		}
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		while (match.Success)
		{
			stringBuilder.Append(text.Substring(num, match.Index - num));
			num = match.Index + match.Length;
			if (metadata != null)
			{
				Match match2 = new Regex("\\s*([^=]+?)\\s*=\\s*(?:\"([^\"]+)\"|([^\\s]+))").Match(match.Groups[1].Value);
				while (match2.Success)
				{
					string value = match2.Groups[1].Value;
					string value2 = (match2.Groups[2].Success ? match2.Groups[2] : match2.Groups[3]).Value;
					metadata[value] = value2;
					match2 = match2.NextMatch();
				}
			}
			match = match.NextMatch();
		}
		stringBuilder.Append(text.Substring(num));
		return stringBuilder.ToString();
	}

	public void Setup(string text, Action<string> clickLinkCallback, Dictionary<string, string> metadata = null)
	{
		this.clickLinkCallback = clickLinkCallback;
		ColorTheme theme = ThemeManager.Inst.Theme;
		List<Func<TextSection, IEnumerable<TextSection>>> obj = new List<Func<TextSection, IEnumerable<TextSection>>>
		{
			InsertCustomTexts,
			(TextSection section) => Enumerable.Repeat<TextSection>(new TextSection(section.type, CodeUtilities.ApplyCodeTags(section.text), section.isSpoiler), 1),
			ApplyHeadings,
			ApplyLinks,
			ApplyUnlocks,
			ApplySpoilers,
			ApplyImages,
			InsertCustomSections
		};
		text = ProcessMetadata(text, metadata);
		IEnumerable<TextSection> enumerable = Enumerable.Repeat<TextSection>(new TextSection(SectionType.Text, text, isSpoiler: false), 1);
		foreach (Func<TextSection, IEnumerable<TextSection>> stage in obj)
		{
			enumerable = Enumerable.SelectMany<TextSection, TextSection>(enumerable, (Func<TextSection, IEnumerable<TextSection>>)((TextSection section) => (section.type != 0) ? Enumerable.Repeat<TextSection>(section, 1) : stage(section)));
		}
		foreach (TextSection item in enumerable)
		{
			if (string.IsNullOrEmpty(item.text))
			{
				continue;
			}
			switch (item.type)
			{
			case SectionType.Text:
			{
				CodeInputField codeInputField = UnityEngine.Object.Instantiate(textPrefab, base.transform);
				theme.docs.text.ApplyTo(codeInputField);
				codeInputField.text = item.text;
				if (item.isSpoiler)
				{
					UnityEngine.Object.Instantiate(spoilerWarning, codeInputField.transform).SetText(item.spoilerText);
				}
				textFields.Add(codeInputField);
				break;
			}
			case SectionType.Image:
			{
				int num = item.text.IndexOf('\u001e');
				if (num < 0)
				{
					break;
				}
				string text2 = item.text.Substring(0, num);
				string text3 = item.text.Substring(num + 1);
				string text4 = null;
				num = text3.IndexOf('\u001e');
				if (num > 0)
				{
					text4 = text3.Substring(num + 1);
					text3 = text3.Substring(0, num);
				}
				bool num2 = text3.StartsWith("video:", StringComparison.OrdinalIgnoreCase);
				int result = -1;
				Component component = null;
				if (!num2)
				{
					string text5 = text3;
					if (char.IsDigit(text5[text5.Length - 1]) && string.IsNullOrEmpty(text2))
					{
						int i;
						for (i = 1; i < text3.Length; i++)
						{
							string text6 = text3;
							int num3 = i + 1;
							if (!char.IsDigit(text6[text6.Length - num3]))
							{
								break;
							}
						}
						int.TryParse(text3.Substring(text3.Length - i), out result);
						text3 = text3.Substring(0, text3.Length - i);
					}
					Image image = UnityEngine.Object.Instantiate(imgPrefab, base.transform);
					component = image;
					if (image.TryGetComponent<ImageLoader>(out var component2))
					{
						component2.Show(text3, BasePath);
					}
					else
					{
						image.sprite = ResourceManager.GetSprite(text3);
					}
				}
				else
				{
					Component component3 = (component = UnityEngine.Object.Instantiate(videoPrefab, base.transform));
					text3 = text3.Substring(6);
					((VideoUI)component3).Play(text3, (string)null, BasePath);
				}
				if (!string.IsNullOrEmpty(text4) && component.TryGetComponent<MarkdownLink>(out var component4))
				{
					component4.markdown = this;
					component4.url = text4;
				}
				if (!component.TryGetComponent<LayoutElement>(out var component5))
				{
					break;
				}
				int num4 = text2.IndexOf('|');
				if (num4 >= 0)
				{
					string text7 = text2.Substring(num4 + 1);
					int num5 = text7.IndexOf('x');
					string text8 = null;
					string text9 = null;
					if (num5 >= 0)
					{
						text8 = text7.Substring(0, num5);
						text9 = text7.Substring(num5 + 1);
					}
					if (text8 != null && int.TryParse(text8, out var result2))
					{
						component5.preferredWidth = result2;
					}
					else
					{
						component5.flexibleWidth = 1f;
					}
					if (text9 != null && int.TryParse(text9, out var result3))
					{
						component5.preferredHeight = result3;
					}
					else
					{
						component5.flexibleHeight = 1f;
					}
				}
				else if (result > 0)
				{
					component5.preferredHeight = result;
				}
				break;
			}
			case SectionType.Custom:
				if (item.text == "output")
				{
					OutputText outputText = UnityEngine.Object.Instantiate(outputPrefab, base.transform);
					theme.docs.text.ApplyTo(outputText.Text);
				}
				else
				{
					if (!item.text.StartsWith("itemblock"))
					{
						break;
					}
					string[] array = item.text.Split(' ');
					Func<ItemBlock> up;
					if (array[1] == "stats_sum")
					{
						up = delegate
						{
							if (!MainSim.Inst.MightBeSimulating())
							{
								Achievements.UpdateSum(MainSim.Inst.GetCurrentTime());
							}
							return Achievements.GetSum();
						};
					}
					else if (array[1] == "stats_best")
					{
						up = Achievements.GetBest;
					}
					else if (!(array[1] == "cost"))
					{
						up = ((!(array[1] == "tooltip")) ? ((Func<ItemBlock>)(() => (ItemBlock)null)) : ((Func<ItemBlock>)(() => MainSim.Inst.workspace.tooltip.Info?.itemBlock)));
					}
					else if (array[2] == "object")
					{
						FarmObjectSO fo = ResourceManager.GetFarmObject(array[3]);
						ItemBlock cost = fo.cost;
						int upgradeCount = Mathf.Max(0, MainSim.Inst.NumUnlocked(fo.yieldUpgradeName) - 1);
						up = () => cost * ((!(fo.yieldUpgradeName != "")) ? 1 : Mathf.Max(1, 1 << upgradeCount));
					}
					else if (array[2] == "unlock")
					{
						UnlockSO unlockSO = ResourceManager.GetUnlock(array[3]);
						up = ((unlockSO != null) ? ((Func<ItemBlock>)(() => MainSim.Inst.GetUnlockCost(unlockSO))) : ((Func<ItemBlock>)(() => (ItemBlock)null)));
					}
					else
					{
						up = () => (ItemBlock)null;
					}
					UnityEngine.Object.Instantiate(invPrefab, base.transform).SetUp(up);
				}
				break;
			}
		}
	}

	public void UpdateText(string text)
	{
		for (int num = base.transform.childCount - 1; num >= 0; num--)
		{
			UnityEngine.Object.Destroy(base.transform.GetChild(num).gameObject);
		}
		textFields.Clear();
		Setup(text, clickLinkCallback);
	}

	private IEnumerable<TextSection> ApplyHeadings(TextSection section)
	{
		Regex regex = new Regex("(?<=^|\\r\\n|\\n)#+ .*");
		StringBuilder stringBuilder = new StringBuilder(section.text);
		foreach (Match item in Enumerable.Reverse<Match>((IEnumerable<Match>)regex.Matches(section.text)))
		{
			Capture capture = Enumerable.First<Capture>((IEnumerable<Capture>)item.Captures);
			stringBuilder.Remove(capture.Index, capture.Length);
			int num = capture.Value.IndexOf('#');
			int num2 = 40;
			while (capture.Value[num + 1] == '#')
			{
				num++;
				num2 -= 8;
			}
			string arg = capture.Value.Substring(num + 2);
			string value = $"<size={num2}px>{arg}</size><line-height=50%>\n</line-height>";
			stringBuilder.Insert(capture.Index, value);
		}
		section.text = stringBuilder.ToString();
		return Enumerable.Repeat<TextSection>(section, 1);
	}

	private IEnumerable<TextSection> ApplyLinks(TextSection section)
	{
		ColorTheme theme = ThemeManager.Inst.Theme;
		Regex regex = new Regex("(?<!!)\\[(?!!).*?\\]\\(.*?\\)");
		StringBuilder stringBuilder = new StringBuilder(section.text);
		foreach (Match item in Enumerable.Reverse<Match>((IEnumerable<Match>)regex.Matches(section.text)))
		{
			Capture capture = Enumerable.First<Capture>((IEnumerable<Capture>)item.Captures);
			int num = capture.Value.IndexOf(']');
			string arg = capture.Value.Substring(1, num - 1);
			string arg2 = capture.Value.Substring(num + 2, capture.Value.Length - (num + 2) - 1);
			stringBuilder.Remove(capture.Index, capture.Length);
			string value = $"<font=\"FiraCode-Regular SDF\"><color={theme.docs.link}><u><link=\"{arg2}\">{arg}</link></u></color></font>";
			stringBuilder.Insert(capture.Index, value);
		}
		section.text = stringBuilder.ToString();
		return Enumerable.Repeat<TextSection>(section, 1);
	}

	private IEnumerable<TextSection> ApplyUnlocks(TextSection section)
	{
		IEnumerable<string> enumerable = Enumerable.Select<Match, string>((IEnumerable<Match>)new Regex("<unlock=.*?>").Matches(section.text), (Func<Match, string>)((Match m) => Enumerable.First<Capture>((IEnumerable<Capture>)m.Captures).Value.Substring(8, Enumerable.First<Capture>((IEnumerable<Capture>)m.Captures).Value.Length - 9)));
		string[] array = new Regex("</?unlock=?.*?>").Split(section.text);
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		IEnumerator<string> enumerator = enumerable.GetEnumerator();
		string[] array2 = array;
		foreach (string value in array2)
		{
			if (flag)
			{
				enumerator.MoveNext();
				if (MainSim.Inst.IsUnlocked(enumerator.Current))
				{
					stringBuilder.Append(value);
				}
			}
			else
			{
				stringBuilder.Append(value);
			}
			flag = !flag;
		}
		section.text = stringBuilder.ToString();
		return Enumerable.Repeat<TextSection>(section, 1);
	}

	private IEnumerable<TextSection> ApplySpoilers(TextSection section)
	{
		IEnumerable<string> enumerable = Enumerable.Select<Match, string>((IEnumerable<Match>)new Regex("<spoiler=.*?>").Matches(section.text), (Func<Match, string>)((Match m) => Enumerable.First<Capture>((IEnumerable<Capture>)m.Captures).Value.Substring(9, Enumerable.First<Capture>((IEnumerable<Capture>)m.Captures).Value.Length - 10)));
		string[] array = new Regex("</?spoiler=?.*?>").Split(section.text);
		List<TextSection> list = new List<TextSection>();
		bool flag = false;
		IEnumerator<string> enumerator = enumerable.GetEnumerator();
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (flag)
			{
				enumerator.MoveNext();
				list.Add(new TextSection(SectionType.Text, text, isSpoiler: true, enumerator.Current));
			}
			else
			{
				list.Add(new TextSection(SectionType.Text, text, isSpoiler: false));
			}
			flag = !flag;
		}
		return list;
	}

	private IEnumerable<TextSection> ApplyImages(TextSection section)
	{
		Match match = new Regex("!\\[(.*?)\\]\\((.*?)\\)|\\[!\\[(.*?)\\]\\((.*?)\\)\\]\\((.*?)\\)").Match(section.text);
		int num = 0;
		List<TextSection> list = new List<TextSection>();
		while (match.Success)
		{
			if (num < match.Index)
			{
				string text = section.text.Substring(num, match.Index - num);
				list.Add(new TextSection(SectionType.Text, text, section.isSpoiler, section.spoilerText));
			}
			string text2;
			if (match.Groups[1].Success)
			{
				string value = match.Groups[1].Value;
				string value2 = match.Groups[2].Value;
				text2 = value + "\u001e" + value2;
			}
			else
			{
				string value3 = match.Groups[3].Value;
				string value4 = match.Groups[4].Value;
				string value5 = match.Groups[5].Value;
				text2 = value3 + "\u001e" + value4 + "\u001e" + value5;
			}
			TextSection item = new TextSection(SectionType.Image, text2, section.isSpoiler, section.spoilerText);
			list.Add(item);
			num = match.Index + match.Length;
			match = match.NextMatch();
		}
		if (num < section.text.Length)
		{
			string text3 = section.text.Substring(num, section.text.Length - num);
			list.Add(new TextSection(SectionType.Text, text3, section.isSpoiler, section.spoilerText));
		}
		return list;
	}

	private IEnumerable<TextSection> InsertCustomTexts(TextSection section)
	{
		Regex regex = new Regex("{{.*}}");
		StringBuilder stringBuilder = new StringBuilder(section.text);
		foreach (Match item in Enumerable.Reverse<Match>((IEnumerable<Match>)regex.Matches(section.text)))
		{
			Capture capture = Enumerable.First<Capture>((IEnumerable<Capture>)item.Captures);
			string text = capture.Value.Substring(2, capture.Value.Length - 4);
			stringBuilder.Remove(capture.Index, capture.Length);
			string value = capture.Value;
			switch (text)
			{
			case "unlocksTOC":
				value = GenerateUnlockTOC();
				break;
			case "builtinsTOC":
				value = GenerateBuiltinsTOC();
				break;
			case "itemsTOC":
				value = GenerateItemsTOC();
				break;
			case "entitiesTOC":
				value = GenerateEntitiesTOC();
				break;
			case "groundsTOC":
				value = GenerateGroundsTOC();
				break;
			default:
				if (text.StartsWith("@"))
				{
					value = Localizer.Localize(text.Substring(1));
				}
				break;
			}
			stringBuilder.Insert(capture.Index, value);
		}
		section.text = stringBuilder.ToString();
		return Enumerable.Repeat<TextSection>(section, 1);
	}

	private IEnumerable<TextSection> InsertCustomSections(TextSection section)
	{
		Regex regex = new Regex("{{.*}}");
		List<TextSection> list = Enumerable.ToList<TextSection>(Enumerable.Select<string, TextSection>((IEnumerable<string>)regex.Split(section.text), (Func<string, TextSection>)((string s) => new TextSection(SectionType.Text, s, section.isSpoiler, section.spoilerText))));
		int num = 1;
		foreach (Match item2 in regex.Matches(section.text))
		{
			Capture capture = Enumerable.First<Capture>((IEnumerable<Capture>)item2.Captures);
			string text = capture.Value.Substring(2, capture.Value.Length - 4);
			TextSection item = new TextSection(SectionType.Custom, text, section.isSpoiler, section.spoilerText);
			list.Insert(num, item);
			num += 2;
		}
		return list;
	}

	private string GenerateUnlockTOC()
	{
		IOrderedEnumerable<(string, string, string)> orderedEnumerable = Enumerable.OrderBy<(string, string, string), string>(Enumerable.Append<(string, string, string)>(Enumerable.Select<UnlockSO, (string, string, string)>(Enumerable.Where<UnlockSO>(ResourceManager.GetAllUnlocks(), (Func<UnlockSO, bool>)((UnlockSO unlock) => unlock.enabled)), (Func<UnlockSO, (string, string, string)>)delegate(UnlockSO unlock)
		{
			string text = unlock.docs;
			if (string.IsNullOrEmpty(text))
			{
				text = "unlocks/" + unlock.unlockName;
			}
			return (unlock.name, unlock.name, docLink: text);
		}), ("for", "expand_2", "docs/unlocks/expand_2.md")), (Func<(string, string, string), string>)(((string, string, string) s) => s.Item2));
		StringBuilder stringBuilder = new StringBuilder();
		foreach (var item in (IEnumerable<(string, string, string)>)orderedEnumerable)
		{
			string value = $"<unlock={item.Item1}>[{Localizer.Localize(item.Item2)}]({item.Item3})      </unlock>";
			stringBuilder.Append(value);
		}
		return stringBuilder.ToString();
	}

	private string GenerateBuiltinsTOC()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string item in (IEnumerable<string>)Enumerable.OrderBy<string, string>((IEnumerable<string>)BuiltinFunctions.Functions.Keys, (Func<string, string>)((string s) => s)))
		{
			string value = string.Format("<unlock={0}>[{0}](functions/{0})      </unlock>", item);
			stringBuilder.Append(value);
		}
		return stringBuilder.ToString();
	}

	private string GenerateItemsTOC()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (ItemSO item in Enumerable.Where<ItemSO>(ResourceManager.GetAllItems(), (Func<ItemSO, bool>)((ItemSO i) => i.enabled)))
		{
			string value = $"<unlock={item.itemName}>[{CodeUtilities.ToUpperSnake(item.itemName)}](items/{item.itemName})      </unlock>";
			stringBuilder.Append(value);
		}
		return stringBuilder.ToString();
	}

	private string GenerateEntitiesTOC()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (FarmObjectSO item in Enumerable.Where<FarmObjectSO>(ResourceManager.GetAllFarmObjects(), (Func<FarmObjectSO, bool>)((FarmObjectSO f) => !f.isGround)))
		{
			string value = $"<unlock={item.objectName}>[{CodeUtilities.ToUpperSnake(item.objectName)}](objects/{item.objectName})      </unlock>";
			stringBuilder.Append(value);
		}
		return stringBuilder.ToString();
	}

	private string GenerateGroundsTOC()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (FarmObjectSO item in Enumerable.Where<FarmObjectSO>(ResourceManager.GetAllFarmObjects(), (Func<FarmObjectSO, bool>)((FarmObjectSO f) => f.isGround)))
		{
			string value = $"<unlock={item.objectName}>[{CodeUtilities.ToUpperSnake(item.objectName)}](objects/{item.objectName})      </unlock>";
			stringBuilder.Append(value);
		}
		return stringBuilder.ToString();
	}
}
