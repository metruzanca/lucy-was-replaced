using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveChooser : MonoBehaviour
{
	[SerializeField]
	private SaveOption saveOptionPrefab;

	[SerializeField]
	private Image background;

	[SerializeField]
	private Transform content;

	[SerializeField]
	private Menu menu;

	private Dictionary<string, SaveOption> openOptions = new Dictionary<string, SaveOption>();

	public void Setup()
	{
		foreach (SaveOption value in openOptions.Values)
		{
			UnityEngine.Object.Destroy(value.gameObject);
		}
		foreach (string item in Enumerable.Select<string, string>((IEnumerable<string>)Directory.GetDirectories(Helper.persistentDataPath + "/Saves"), (Func<string, string>)((string d) => new DirectoryInfo(d).Name)))
		{
			AddOption(item);
		}
	}

	private void AddOption(string saveName)
	{
		SaveOption saveOption = UnityEngine.Object.Instantiate(saveOptionPrefab, content);
		openOptions[saveName] = saveOption;
		saveOption.Setup(saveName, this);
	}

	public void DeleteSave(string saveName)
	{
		if (!openOptions.ContainsKey(saveName))
		{
			throw new Exception("tried to delete save that doesn't exist");
		}
		string text = string.Format(Localizer.Localize("popup_warning_delete_save"), saveName);
		List<WarningPopup.ButtonData> buttonsToAdd = new List<WarningPopup.ButtonData>
		{
			new WarningPopup.ButtonData("delete", delegate
			{
				if (Saver.DeleteSave(saveName))
				{
					UnityEngine.Object.Destroy(openOptions[saveName].gameObject);
					openOptions.Remove(saveName);
				}
				MainSim.Inst.warningPopup.Close();
			}),
			new WarningPopup.ButtonData("cancel", MainSim.Inst.warningPopup.Close)
		};
		MainSim.Inst.warningPopup.ShowPopup(text, buttonsToAdd);
	}

	public void LoadSave(string saveName)
	{
		if (MainSim.Inst.dirty)
		{
			List<WarningPopup.ButtonData> buttonsToAdd = new List<WarningPopup.ButtonData>
			{
				new WarningPopup.ButtonData("save", delegate
				{
					Saver.StopFileWatcher();
					Saver.Save(MainSim.Inst);
					menu.LoadSave(saveName);
					SceneManager.LoadScene(0);
				}),
				new WarningPopup.ButtonData("don't save", delegate
				{
					Saver.StopFileWatcher();
					menu.LoadSave(saveName);
					SceneManager.LoadScene(0);
				})
			};
			MainSim.Inst.warningPopup.ShowPopup("popup_warning_load_game", buttonsToAdd);
		}
		else
		{
			Saver.StopFileWatcher();
			menu.LoadSave(saveName);
			SceneManager.LoadScene(0);
		}
	}

	public bool RenameSave(string saveName, string newSaveName)
	{
		if (!Saver.RenameSave(saveName, newSaveName))
		{
			return false;
		}
		openOptions[newSaveName] = openOptions[saveName];
		openOptions.Remove(saveName);
		if (OptionHolder.GetString("activeSave", "Save0") == saveName)
		{
			menu.LoadSave(newSaveName);
		}
		return true;
	}

	public void CreateNewSave()
	{
		string text = GenerateUnusedSaveName();
		Saver.CreateNewSaveGame(text);
		AddOption(text);
		openOptions[text].Edit();
	}

	public void OpenFolder()
	{
		Process.Start(Helper.persistentDataPath);
	}

	public static string GenerateUnusedSaveName()
	{
		int num = 0;
		string text;
		do
		{
			text = $"Save{num}";
			num++;
		}
		while (File.Exists(Saver.GetPathOfSaveFile(text)));
		return text;
	}

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
		background.color = theme.ui.OptionsMenuColor;
	}
}
