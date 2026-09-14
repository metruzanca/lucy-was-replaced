using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SearchBox : MonoBehaviour
{
	private struct SearchResult
	{
		public CodeWindow codeWindow;

		public DocsWindow docsWindow;

		public int stringIndex;

		public SearchResult(CodeWindow codeWindow, int stringIndex)
		{
			this.codeWindow = codeWindow;
			this.stringIndex = stringIndex;
			docsWindow = null;
		}

		public SearchResult(DocsWindow docsWindow, int stringIndex)
		{
			this.docsWindow = docsWindow;
			this.stringIndex = stringIndex;
			codeWindow = null;
		}
	}

	private enum MoveWindowMode
	{
		Always,
		OnlyIfOffScreen,
		Never
	}

	[SerializeField]
	private CodeInputField inputField;

	[SerializeField]
	private Button nextButton;

	[SerializeField]
	private Button previousButton;

	[SerializeField]
	private ColoredButton closeButton;

	[SerializeField]
	private TextMeshProUGUI searchIndexText;

	private List<SearchResult> searchResults = new List<SearchResult>();

	private int currentSearchIndex;

	private string currentSearchString = "";

	private Window currentWindow;

	private bool wasWindowMinimized;

	private void Start()
	{
		inputField.onValueChanged.AddListener(delegate(string s)
		{
			UpdateSearchResults(s);
		});
		nextButton.onClick.AddListener(OnNextButtonClicked);
		previousButton.onClick.AddListener(OnPreviousButtonClicked);
		closeButton.OnClick.AddListener(CloseSearchBox);
	}

	private void UpdateSearchResults(string searchString, MoveWindowMode moveWindowMode = MoveWindowMode.OnlyIfOffScreen)
	{
		int num = searchString.IndexOf('\n');
		num = ((num == -1) ? 29 : Mathf.Min(num, 29));
		if (searchString.Length > num)
		{
			searchString = searchString.Substring(0, num);
			inputField.text = searchString;
		}
		inputField.Select();
		searchResults.Clear();
		if (string.IsNullOrEmpty(searchString))
		{
			foreach (Window openWindow in MainSim.Inst.workspace.OpenWindows)
			{
				DocsWindow component2;
				if (openWindow.TryGetComponent<CodeWindow>(out var component))
				{
					component.UpdateSearch("");
				}
				else if (openWindow.TryGetComponent<DocsWindow>(out component2))
				{
					component2.OpenMarkdownText.UpdateSearch("", -1);
				}
			}
			currentSearchString = "";
			((TMP_Text)searchIndexText).text = "0 / 0";
			LeaveCurrentSearchResult();
			return;
		}
		foreach (Window openWindow2 in MainSim.Inst.workspace.OpenWindows)
		{
			DocsWindow component4;
			if (openWindow2.TryGetComponent<CodeWindow>(out var component3))
			{
				string text = component3.CodeInput.text;
				for (int num2 = text.IndexOf(searchString, 0, StringComparison.OrdinalIgnoreCase); num2 != -1; num2 = text.IndexOf(searchString, num2 + searchString.Length, StringComparison.OrdinalIgnoreCase))
				{
					searchResults.Add(new SearchResult(component3, num2));
				}
				component3.UpdateSearch(searchString);
			}
			else if (openWindow2.TryGetComponent<DocsWindow>(out component4))
			{
				int num3 = component4.OpenMarkdownText.UpdateSearch(searchString, -1);
				for (int i = 0; i < num3; i++)
				{
					searchResults.Add(new SearchResult(component4, i));
				}
			}
		}
		currentSearchString = searchString;
		GoToSearchResult(0, moveWindowMode);
	}

	public void StartSearch(string searchTerm = "")
	{
		base.gameObject.SetActive(value: true);
		inputField.Select();
		inputField.text = searchTerm;
		UpdateSearchResults(searchTerm);
	}

	public void RefreshSearchResults()
	{
		UpdateSearchResults(currentSearchString, MoveWindowMode.Never);
	}

	public void CloseSearchBox()
	{
		UpdateSearchResults("");
		base.gameObject.SetActive(value: false);
	}

	private void OnNextButtonClicked()
	{
		if (searchResults.Count != 0)
		{
			currentSearchIndex = (currentSearchIndex + 1) % searchResults.Count;
			GoToSearchResult(currentSearchIndex, MoveWindowMode.Always);
		}
	}

	private void OnPreviousButtonClicked()
	{
		if (searchResults.Count != 0)
		{
			currentSearchIndex = (currentSearchIndex - 1 + searchResults.Count) % searchResults.Count;
			GoToSearchResult(currentSearchIndex, MoveWindowMode.Always);
		}
	}

	private void GoToSearchResult(int searchIndex, MoveWindowMode moveWindowMode = MoveWindowMode.OnlyIfOffScreen)
	{
		LeaveCurrentSearchResult();
		if (searchResults.Count == 0)
		{
			currentSearchIndex = 0;
			((TMP_Text)searchIndexText).text = "0 / 0";
			return;
		}
		((TMP_Text)searchIndexText).text = $"{searchIndex + 1} / {searchResults.Count}";
		currentSearchIndex = searchIndex;
		if (searchIndex >= searchResults.Count || searchIndex < 0)
		{
			currentSearchIndex = 0;
		}
		CodeWindow codeWindow = searchResults[currentSearchIndex].codeWindow;
		if (codeWindow != null)
		{
			currentWindow = codeWindow.GetComponent<Window>();
			if (currentWindow.isMinimized)
			{
				currentWindow.SetMinmized(minimized: false);
				wasWindowMinimized = true;
			}
			if (moveWindowMode != MoveWindowMode.Never)
			{
				MainSim.Inst.workspace.MoveCameraTo(currentWindow, moveWindowMode == MoveWindowMode.Always, searchResults[currentSearchIndex].stringIndex, codeWindow.CodeInput);
			}
			int stringIndex = searchResults[currentSearchIndex].stringIndex;
			codeWindow.ScrollToStringPosition(stringIndex);
			codeWindow.UpdateSearch(currentSearchString, stringIndex);
			codeWindow.GetComponent<Window>().MoveToFront();
		}
		DocsWindow docsWindow = searchResults[currentSearchIndex].docsWindow;
		if (docsWindow != null)
		{
			currentWindow = docsWindow.GetComponent<Window>();
			if (currentWindow.isMinimized)
			{
				currentWindow.SetMinmized(minimized: false);
				wasWindowMinimized = true;
			}
			if (moveWindowMode != MoveWindowMode.Never)
			{
				MainSim.Inst.workspace.MoveCameraTo(currentWindow, moveWindowMode == MoveWindowMode.Always);
			}
			int stringIndex2 = searchResults[currentSearchIndex].stringIndex;
			docsWindow.OpenMarkdownText.UpdateSearch(currentSearchString, stringIndex2);
			docsWindow.GetComponent<Window>().MoveToFront();
		}
	}

	private void LeaveCurrentSearchResult()
	{
		if (currentWindow != null && wasWindowMinimized)
		{
			currentWindow.SetMinmized(minimized: true);
		}
		if (currentWindow != null && currentWindow.TryGetComponent<CodeWindow>(out var component))
		{
			component.UpdateSearch(currentSearchString);
		}
		currentWindow = null;
		wasWindowMinimized = false;
	}

	public void SetValue(string value)
	{
		inputField.text = value;
	}
}
