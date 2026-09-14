using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class TitleWorkspace : WorkspaceBase
{
	public TitleWindow tilteWinPrefab;

	public GameObject openNewsButton;

	[NonSerialized]
	public HashSet<string> windowHistory;

	public List<Dictionary<string, string>> allNews;

	public const string MetaFilePath = "filePath";

	public const string MetaFileName = "fileName";

	public const string MetaNewsType = "newsType";

	public const string NewsTypeDefault = "default";

	public const string NewsTypeUpdate = "update";

	public const string MetaOpenOnClose = "openOnClose";

	public const string MetaWindowType = "windowType";

	public const string WindowTypeMaximized = "maximized";

	public const string WindowTypeCorner = "corner";

	public const string WindowTypeSide = "side";

	public const string MetaWindowWidth = "windowWidth";

	public const string MetaWindowHeight = "windowHeight";

	public const string MetaTitlebar = "titlebar";

	public const string MetaTitle = "title";

	private string currentLanguage;

	public string NewsLanguage => currentLanguage;

	public string NewsPath => Path.Combine(Application.streamingAssetsPath, "news", currentLanguage);

	public static event Action OnNewsChanged;

	public TitleWindow OpenWindow(string windowName, string windowPage, Vector2 offset = default(Vector2), Vector2 size = default(Vector2))
	{
		if (IsWindowOpen(windowName))
		{
			windowName = GenerateFileName(windowName);
		}
		if (windowHistory == null)
		{
			windowHistory = new HashSet<string>();
		}
		windowHistory.Add(windowName);
		TitleWindow titleWindow = OpenWindow(tilteWinPrefab, windowName, offset, size);
		titleWindow.LoadPage(windowPage);
		if (offset == default(Vector2) && size == default(Vector2))
		{
			PositionNewWindow(titleWindow);
		}
		return titleWindow;
	}

	public void OpenNewsIndexWindow()
	{
		OpenWindow("news_index", "index");
	}

	public void Scroll(float scroll)
	{
		Window openWindow = GetOpenWindow(Input.mousePosition);
		if (openWindow != null && openWindow.TryGetComponent<TitleWindow>(out var component))
		{
			component.Scroll(scroll);
		}
	}

	public void FitWindowsIntoContainer()
	{
		foreach (Window openWindow in base.OpenWindows)
		{
			Rect rect = container.rect;
			RectTransform component = openWindow.GetComponent<RectTransform>();
			Rect rect2 = component.rect;
			Matrix4x4 matrix4x = Matrix4x4.TRS(component.localPosition, component.localRotation, component.localScale);
			rect2.position = matrix4x.MultiplyPoint(rect2.position);
			rect2.size = matrix4x.MultiplyVector(rect2.size);
			if (rect2.yMax > rect.yMax)
			{
				Vector3 localPosition = component.localPosition;
				localPosition.y -= rect2.yMax - rect.yMax;
				component.localPosition = localPosition;
			}
			else if (rect2.yMax - 30f < rect.yMin)
			{
				Vector3 localPosition2 = component.localPosition;
				localPosition2.y += rect.yMin - (rect2.yMax - 30f);
				component.localPosition = localPosition2;
			}
			if (rect2.xMin + 30f > rect.xMax)
			{
				Vector3 localPosition3 = component.localPosition;
				localPosition3.x -= rect2.xMin + 30f - rect.xMax;
				component.localPosition = localPosition3;
			}
			else if (rect2.xMax - 30f < rect.xMin)
			{
				Vector3 localPosition4 = component.localPosition;
				localPosition4.x += rect.xMin - (rect2.xMax - 30f);
				component.localPosition = localPosition4;
			}
		}
	}

	public void OpenLatestNews(bool firstTime)
	{
		if (allNews == null)
		{
			Debug.LogError("TitleWorkspace: OpenLatestNews called before workspace was active and enabled");
			return;
		}
		if (firstTime)
		{
			foreach (Dictionary<string, string> item in allNews)
			{
				if ((!item.TryGetValue("newsType", out var value) || !(value != "update")) && item.TryGetValue("fileName", out var value2))
				{
					windowHistory.Add(value2);
				}
			}
			return;
		}
		string text = null;
		string text2 = null;
		foreach (Dictionary<string, string> item2 in allNews)
		{
			if (item2.TryGetValue("filePath", out var value3) && item2.TryGetValue("fileName", out var value4) && (text == null || string.CompareOrdinal(value4, text2) > 0))
			{
				text = value3;
				text2 = value4;
			}
		}
		if (text != null)
		{
			HashSet<string> hashSet = windowHistory;
			if (hashSet == null || !hashSet.Contains(text2))
			{
				OpenWindow(text2, text);
			}
		}
	}

	public string GetNewsIndexPage()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("<meta windowType=side>");
		stringBuilder.Append("# ").AppendLine("Latest News");
		foreach (Dictionary<string, string> item in (IEnumerable<Dictionary<string, string>>)Enumerable.OrderByDescending<Dictionary<string, string>, string>((IEnumerable<Dictionary<string, string>>)allNews, (Func<Dictionary<string, string>, string>)((Dictionary<string, string> n) => n["fileName"]), (IComparer<string>)StringComparer.OrdinalIgnoreCase))
		{
			if (!item.TryGetValue("filePath", out var value))
			{
				continue;
			}
			if (!item.TryGetValue("title", out var value2))
			{
				string value4;
				if (item.TryGetValue("titlebar", out var value3))
				{
					value2 = value3;
				}
				else if (item.TryGetValue("fileName", out value4))
				{
					value2 = value4;
				}
			}
			if (!string.IsNullOrEmpty(value2))
			{
				stringBuilder.AppendLine("[" + value2 + "](" + value + ")");
			}
		}
		return stringBuilder.ToString();
	}

	public override void RemoveWindow(Window window)
	{
		base.RemoveWindow(window);
		if (!window.TryGetComponent<TitleWindow>(out var component) || !component.Metadata.TryGetValue("openOnClose", out var value) || !(value != component.openPage))
		{
			return;
		}
		foreach (Dictionary<string, string> item in allNews)
		{
			if (item.TryGetValue("filePath", out var value2) && Path.GetFileName(value2) == value)
			{
				OpenWindow(item["fileName"], value2);
				break;
			}
		}
	}

	private void OnEnable()
	{
		OptionHolder.OnOptionChanged += OnOptionChanged;
		if (currentLanguage != Localizer.Lang)
		{
			FindAllNews();
		}
	}

	private void OnDisable()
	{
		OptionHolder.OnOptionChanged -= OnOptionChanged;
	}

	private void OnOptionChanged(string item)
	{
		if (item == "language")
		{
			FindAllNews();
		}
	}

	private void FindAllNews()
	{
		if (allNews == null)
		{
			allNews = new List<Dictionary<string, string>>();
		}
		allNews.Clear();
		currentLanguage = Localizer.Lang;
		string newsPath = NewsPath;
		if (!Directory.Exists(newsPath))
		{
			currentLanguage = "EN";
			newsPath = NewsPath;
			if (!Directory.Exists(newsPath))
			{
				return;
			}
		}
		foreach (string item in Directory.EnumerateFiles(newsPath))
		{
			if (item.EndsWith(".md"))
			{
				string value = item.Substring(NewsPath.Length + 1);
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item);
				string text = File.ReadAllText(item);
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				MarkdownText.ProcessMetadata(text, dictionary);
				dictionary["fileName"] = fileNameWithoutExtension;
				dictionary["filePath"] = value;
				allNews.Add(dictionary);
			}
		}
		if (openNewsButton != null)
		{
			openNewsButton.SetActive(allNews.Count > 0);
		}
		TitleWorkspace.OnNewsChanged?.Invoke();
	}

	private void PositionNewWindow(TitleWindow titleWin)
	{
		if (!titleWin.TryGetComponent<Window>(out var component))
		{
			return;
		}
		titleWin.Metadata.TryGetValue("windowType", out var value);
		Vector2 vector = default(Vector2);
		if (titleWin.Metadata.TryGetValue("windowWidth", out var value2))
		{
			float.TryParse(value2, out vector.x);
		}
		if (titleWin.Metadata.TryGetValue("windowHeight", out var value3))
		{
			float.TryParse(value3, out vector.y);
		}
		RectTransform component2 = component.GetComponent<RectTransform>();
		Rect rect = container.rect;
		if (string.Equals(value, "maximized", StringComparison.OrdinalIgnoreCase))
		{
			if (vector.x <= 0f)
			{
				vector.x = rect.size.x;
			}
			if (vector.y <= 0f)
			{
				vector.y = rect.size.y;
			}
			Window window = component;
			Vector2 playerSetSize = (component2.sizeDelta = (vector - 2f * new Vector2(50f, 50f)) / component2.localScale.x);
			window.playerSetSize = playerSetSize;
			component2.anchoredPosition = new Vector2(50f, component2.sizeDelta.y * component2.localScale.y / 2f);
		}
		else if (string.Equals(value, "corner", StringComparison.OrdinalIgnoreCase))
		{
			if (vector.x <= 0f)
			{
				vector.x = 250f;
			}
			if (vector.y <= 0f)
			{
				vector.y = 150f;
			}
			Window window2 = component;
			Vector2 playerSetSize = (component2.sizeDelta = vector / component2.localScale.x);
			window2.playerSetSize = playerSetSize;
			component2.localPosition = new Vector2(rect.xMax - 20f - vector.x, rect.yMin + 20f + vector.y) - rect.center;
		}
		else
		{
			if (vector.x <= 0f)
			{
				vector.x = rect.width - 375f - 75f;
			}
			if (vector.y <= 0f)
			{
				vector.y = rect.height - 150f;
			}
			Window window3 = component;
			Vector2 playerSetSize = (component2.sizeDelta = vector / component2.localScale.x);
			window3.playerSetSize = playerSetSize;
			component2.anchoredPosition = new Vector2(375f, component2.sizeDelta.y * component2.localScale.y / 2f);
		}
	}
}
