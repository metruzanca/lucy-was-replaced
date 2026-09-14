using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class DocsWindow : MonoBehaviour
{
	[SerializeField]
	private ScrollRect scrollView;

	[SerializeField]
	private RectTransform container;

	[SerializeField]
	private MarkdownText markdownPrefab;

	public string openDoc;

	public string fullOpenDoc;

	public MarkdownText OpenMarkdownText { get; private set; }

	public void LoadDoc(string doc)
	{
		if (container.childCount > 0)
		{
			Transform child = container.GetChild(0);
			container.DetachChildren();
			Object.Destroy(child.gameObject);
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (doc.StartsWith("functions/"))
		{
			string text = doc.Substring("functions/".Length);
			stringBuilder.Append(Localizer.Localize("code_tooltip_" + text));
			AddUnlockedIn(stringBuilder, text);
		}
		else if (doc.StartsWith("unlocks/"))
		{
			string unlockName = doc.Substring("unlocks/".Length);
			stringBuilder.Append(TooltipUtils.UnlockTooltip(unlockName).text);
		}
		else if (doc.StartsWith("items/"))
		{
			string text2 = doc.Substring("items/".Length);
			stringBuilder.Append(TooltipUtils.ItemTooltip(text2).text);
			AddUnlockedIn(stringBuilder, text2);
		}
		else if (doc.StartsWith("objects/"))
		{
			string text3 = doc.Substring("objects/".Length);
			stringBuilder.Append(TooltipUtils.FarmObjectTooltip(text3).text);
			AddUnlockedIn(stringBuilder, text3);
		}
		else
		{
			stringBuilder.Append(Localizer.LoadDoc(doc));
		}
		container.anchoredPosition = new Vector2(0f, 0f);
		MarkdownText markdownText = Object.Instantiate(markdownPrefab, container);
		fullOpenDoc = stringBuilder.ToString();
		markdownText.BasePath = Path.GetDirectoryName(Path.Combine(Application.streamingAssetsPath, "Languages", Localizer.Lang, doc));
		markdownText.Setup(fullOpenDoc, LinkCalled);
		openDoc = doc;
		OpenMarkdownText = markdownText;
		MainSim.Inst.workspace.searchBox.RefreshSearchResults();
	}

	private void AddUnlockedIn(StringBuilder sb, string word)
	{
		string keywordDocsPage = TooltipUtils.GetKeywordDocsPage(word);
		if (!(keywordDocsPage == ""))
		{
			string text = Enumerable.Last<string>((IEnumerable<string>)keywordDocsPage.Split('/'));
			text = text.Substring(0, text.Length - 3);
			string value = string.Format(Localizer.Localize("unlocked_in"), CodeUtilities.ToUpperSnake(text), keywordDocsPage);
			sb.Append('\n');
			sb.Append('\n');
			sb.Append(value);
		}
	}

	private void LinkCalled(string link)
	{
		if (link.StartsWith("persistent_data_path/"))
		{
			Process.Start(Path.Combine(Helper.persistentDataPath, link.Substring("persistent_data_path/".Length)));
		}
		else if (link.StartsWith("https"))
		{
			Process.Start(link);
		}
		else
		{
			LoadDoc(link);
		}
	}

	public void GotoHome()
	{
		LoadDoc("docs/home.md");
	}

	public void Scroll(float scroll)
	{
		container.anchoredPosition += new Vector2(0f, scroll * -500f);
	}

	private void Awake()
	{
		OptionHolder.OnOptionChanged += OnSettingChanged;
		ThemeManager.Inst.OnThemeChanged += OnThemeChanged;
	}

	private void OnDestroy()
	{
		OptionHolder.OnOptionChanged -= OnSettingChanged;
		ThemeManager.Inst.OnThemeChanged -= OnThemeChanged;
	}

	private void OnSettingChanged(string setting)
	{
		if (setting == "language")
		{
			LoadDoc(openDoc);
		}
	}

	private void OnThemeChanged(ColorTheme theme)
	{
		if (TryGetComponent<Image>(out var component))
		{
			component.color = theme.ui.WindowFrameColor;
		}
		if (scrollView.TryGetComponent<Image>(out var component2))
		{
			component2.color = theme.docs.BackgroundColor;
		}
		if (scrollView.verticalScrollbar.handleRect.TryGetComponent<Image>(out var component3))
		{
			component3.color = theme.ui.ScrollbarColor;
		}
		LoadDoc(openDoc);
	}
}
