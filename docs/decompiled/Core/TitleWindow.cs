using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleWindow : MonoBehaviour
{
	[SerializeField]
	private ScrollRect scrollView;

	[SerializeField]
	private RectTransform container;

	[SerializeField]
	private TMP_Text title;

	[SerializeField]
	private MarkdownText markdownPrefab;

	public string openPage;

	public string openLanguage;

	private TitleWorkspace titleWorkspace;

	public MarkdownText OpenMarkdownText { get; private set; }

	public Dictionary<string, string> Metadata { get; private set; }

	public void LoadPage(string doc)
	{
		if (container.childCount > 0)
		{
			Transform child = container.GetChild(0);
			container.DetachChildren();
			Object.Destroy(child.gameObject);
		}
		container.anchoredPosition = new Vector2(0f, 0f);
		if (TryGetComponent<Window>(out var component) && component.workspace is TitleWorkspace titleWorkspace)
		{
			this.titleWorkspace = titleWorkspace;
			string path = Path.Combine(this.titleWorkspace.NewsPath, doc);
			string text = null;
			text = ((doc == "index") ? this.titleWorkspace.GetNewsIndexPage() : ((!File.Exists(path)) ? "" : File.ReadAllText(path)));
			Metadata = new Dictionary<string, string>();
			MarkdownText markdownText = Object.Instantiate(markdownPrefab, container);
			markdownText.BasePath = Path.GetDirectoryName(path);
			markdownText.Setup(text, LinkCalled, Metadata);
			if ((Object)(object)title != null && Metadata.TryGetValue("titlebar", out var value) && !string.IsNullOrEmpty(value))
			{
				((Component)(object)title).gameObject.SetActive(value: true);
				title.text = value;
			}
			else
			{
				((Component)(object)title).gameObject.SetActive(value: false);
			}
			openPage = doc;
			openLanguage = this.titleWorkspace.NewsLanguage;
			OpenMarkdownText = markdownText;
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
		else if (link.StartsWith("https://store.steampowered.com/app/"))
		{
			string text = link.Substring(35);
			int num = text.IndexOf('/');
			uint result;
			if (num < 0)
			{
				UnityEngine.Debug.LogError("Failed to parse Steam App ID from url: " + link);
			}
			else if (!uint.TryParse(text.Substring(num), out result))
			{
				UnityEngine.Debug.LogError("Failed to parse Steam App ID from url: " + link);
			}
			else if (!TryShowOverlay(result))
			{
				Application.OpenURL(link);
			}
		}
		else
		{
			LoadPage(link);
		}
	}

	public void Scroll(float scroll)
	{
		container.anchoredPosition += new Vector2(0f, scroll * -500f);
	}

	private void Awake()
	{
		TitleWorkspace.OnNewsChanged += OnNewsChanged;
		ThemeManager.Inst.OnThemeChanged += OnThemeChanged;
	}

	private void OnEnable()
	{
		if (!string.IsNullOrEmpty(openPage) && openLanguage != titleWorkspace.NewsLanguage)
		{
			LoadPage(openPage);
		}
	}

	private void OnDestroy()
	{
		TitleWorkspace.OnNewsChanged -= OnNewsChanged;
		ThemeManager.Inst.OnThemeChanged -= OnThemeChanged;
	}

	private void OnNewsChanged()
	{
		if (!string.IsNullOrEmpty(openPage))
		{
			LoadPage(openPage);
		}
	}

	private void OnThemeChanged(ColorTheme theme)
	{
		theme.ui.text.ApplyTo(title);
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
		LoadPage(openPage);
	}

	private bool TryShowOverlay(uint appId)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (!SteamManager.Initialized)
		{
			return false;
		}
		if (!SteamUtils.IsOverlayEnabled())
		{
			return false;
		}
		SteamFriends.ActivateGameOverlayToStore(new AppId_t(appId), (EOverlayToStoreFlag)0);
		return true;
	}
}
