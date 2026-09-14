using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Menu : MonoBehaviour
{
	[SerializeField]
	private GameObject menu;

	[SerializeField]
	private GameObject[] uiCanvases;

	[SerializeField]
	private GameObject titlePage;

	public TitleWorkspace titleWorkspace;

	[SerializeField]
	private SaveChooser saveChooser;

	[SerializeField]
	private OptionMenu options;

	[SerializeField]
	private ColoredButton saveButton;

	private void Start()
	{
		ResourceManager.LoadAll();
		Achievements.LoadStats();
		bool currentSaveGameExists = Saver.GetCurrentSaveGameExists();
		string @string = OptionHolder.GetString("activeSave", "Save0");
		LoadSave(@string);
		try
		{
			Saver.Load(MainSim.Inst);
		}
		catch (IOException message)
		{
			Debug.LogError(message);
			List<WarningPopup.ButtonData> buttonsToAdd = new List<WarningPopup.ButtonData>
			{
				new WarningPopup.ButtonData("ok", MainSim.Inst.warningPopup.Close)
			};
			MainSim.Inst.warningPopup.ShowPopup(CodeUtilities.LocalizeAndFormat("popup_warning_failed_read_save", @string), buttonsToAdd);
		}
		catch (ArgumentException message2)
		{
			Debug.LogError(message2);
			List<WarningPopup.ButtonData> buttonsToAdd2 = new List<WarningPopup.ButtonData>
			{
				new WarningPopup.ButtonData("ok", delegate
				{
					MainSim.Inst.warningPopup.Close();
					MainSim.Inst.workspace.AddNewDocsWindow("docs/backup.md");
					Play();
				})
			};
			MainSim.Inst.warningPopup.ShowPopup(CodeUtilities.LocalizeAndFormat("popup_warning_corrupted_save", @string), buttonsToAdd2);
		}
		Open();
		Saver.LoadTitleSave(this);
		StartCoroutine(OpenLatestNews(!currentSaveGameExists));
		RestartAutosave("autosave");
		RestartAutosave("autosave progress");
		OptionHolder.OnOptionChanged -= RestartAutosave;
		OptionHolder.OnOptionChanged += RestartAutosave;
	}

	private void OnDisable()
	{
		Saver.SaveTitleSave(this);
	}

	private IEnumerator OpenLatestNews(bool firstTime)
	{
		yield return null;
		titleWorkspace.FitWindowsIntoContainer();
		titleWorkspace.OpenLatestNews(firstTime);
	}

	private void RestartAutosave(string optionName)
	{
		if (optionName == "autosave")
		{
			TimerManager.StopTimer(AutosaveCode);
			if (OptionHolder.GetString("autosave") == "enabled")
			{
				TimerManager.StartTimer(AutosaveCode, 30.0, loop: true);
			}
		}
		else if (optionName == "autosave progress")
		{
			TimerManager.StopTimer(AutosaveProgress);
			if (OptionHolder.GetString("autosave progress") == "enabled")
			{
				TimerManager.StartTimer(AutosaveProgress, 30.0, loop: true);
			}
		}
	}

	private void AutosaveProgress()
	{
		Saver.SaveProgress(MainSim.Inst);
	}

	private void AutosaveCode()
	{
		Saver.SaveCode(MainSim.Inst);
	}

	public void LoadSave(string saveName)
	{
		if (OptionHolder.GetString("activeSave", "Save0") != saveName)
		{
			OptionHolder.SetOption("activeSave", saveName);
		}
	}

	public void Play()
	{
		MainSim.Inst.workspace.gameObject.SetActive(value: true);
		GameObject[] array = uiCanvases;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: true);
		}
		menu.gameObject.SetActive(value: false);
		FMODSoundManager.CloseMenu();
	}

	public void ChooseSave()
	{
		titlePage.gameObject.SetActive(value: false);
		saveChooser.gameObject.SetActive(value: true);
		saveChooser.Setup();
	}

	public void PressSaveButton()
	{
		TimerManager.StartTimer(ReleaseSaveButton, 1.0);
		saveButton.Interactable = false;
		saveButton.Text = Localizer.Localize("saved");
		Saver.Save(MainSim.Inst);
		Saver.SaveTitleSave(this);
	}

	private void ReleaseSaveButton()
	{
		saveButton.Interactable = true;
		saveButton.Text = Localizer.Localize("save");
	}

	public void Open()
	{
		MainSim.Inst.workspace.gameObject.SetActive(value: false);
		GameObject[] array = uiCanvases;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.SetActive(value: false);
		}
		menu.gameObject.SetActive(value: true);
		titlePage.gameObject.SetActive(value: true);
		saveChooser.gameObject.SetActive(value: false);
		options.gameObject.SetActive(value: false);
	}

	public void Options()
	{
		titlePage.gameObject.SetActive(value: false);
		options.gameObject.SetActive(value: true);
		options.Setup();
	}

	public void Quit()
	{
		Saver.SaveTitleSave(this);
		if (MainSim.Inst.dirty)
		{
			List<WarningPopup.ButtonData> buttonsToAdd = new List<WarningPopup.ButtonData>
			{
				new WarningPopup.ButtonData("save", delegate
				{
					Saver.Save(MainSim.Inst);
					Application.Quit();
				}),
				new WarningPopup.ButtonData("don't save", Application.Quit)
			};
			MainSim.Inst.warningPopup.ShowPopup("popup_warning_exit_game", buttonsToAdd);
		}
		else
		{
			Application.Quit();
		}
	}

	public void JoinDiscord()
	{
		Application.OpenURL("https://discord.com/invite/kj33cJkeJn");
	}
}
