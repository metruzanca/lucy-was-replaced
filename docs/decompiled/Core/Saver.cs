using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class Saver
{
	private static FileSystemWatcher watcher;

	private static List<(string, string)> codeToUpdate = new List<(string, string)>();

	private static object codeToUpdateLock = new object();

	private const int version = 3;

	public static void Save(MainSim mainSim)
	{
		SaveProgress(mainSim);
		SaveCode(mainSim);
	}

	public static void SaveProgress(MainSim mainSim)
	{
		if (!(mainSim == null))
		{
			bool num = mainSim.MightBeSimulating();
			SaveGame saveGame = new SaveGame();
			if (num)
			{
				saveGame.items = new ItemBlock(mainSim.storedSim.farm.Items);
			}
			else
			{
				saveGame.items = mainSim.GetInventory();
			}
			saveGame.items.Serialize();
			Vector2 farmPos = ((RectTransform)mainSim.workspace.container.GetChild(0)).anchoredPosition;
			IEnumerable<Window> openWindows = mainSim.workspace.OpenWindows;
			saveGame.openFilePositions = Enumerable.ToList<Pair<string, Vector2>>(Enumerable.Select<Window, Pair<string, Vector2>>(openWindows, (Func<Window, Pair<string, Vector2>>)((Window f) => new Pair<string, Vector2>(f.windowName, f.WindowRect.anchoredPosition - farmPos))));
			saveGame.openFileSizes = Enumerable.ToList<Pair<string, Vector2>>(Enumerable.Select<Window, Pair<string, Vector2>>(openWindows, (Func<Window, Pair<string, Vector2>>)((Window f) => new Pair<string, Vector2>(f.windowName, f.playerSetSize))));
			saveGame.openFileScrollPositions = Enumerable.ToList<Pair<string, float>>(Enumerable.Select<Window, Pair<string, float>>(Enumerable.Where<Window>(openWindows, (Func<Window, bool>)((Window w) => w.TryGetComponent<CodeWindow>(out var _))), (Func<Window, Pair<string, float>>)((Window w) => new Pair<string, float>(w.windowName, ((RectTransform)w.GetComponent<CodeWindow>().CodeInput.transform).anchoredPosition.y))));
			saveGame.dockedFiles = Enumerable.ToList<Pair<string, string>>(Enumerable.Select<Window, Pair<string, string>>(Enumerable.Where<Window>(openWindows, (Func<Window, bool>)((Window f) => f.dockedParent != null)), (Func<Window, Pair<string, string>>)((Window f) => new Pair<string, string>(f.windowName, f.dockedParent.windowName))));
			saveGame.minimizedFiles = Enumerable.ToList<string>(Enumerable.Select<Window, string>(Enumerable.Where<Window>(openWindows, (Func<Window, bool>)((Window f) => f.isMinimized)), (Func<Window, string>)((Window f) => f.windowName)));
			saveGame.openDocPages = Enumerable.ToList<Pair<string, string>>(Enumerable.Select<Window, Pair<string, string>>(Enumerable.Where<Window>(openWindows, (Func<Window, bool>)((Window w) => w.GetComponent<DocsWindow>() != null)), (Func<Window, Pair<string, string>>)((Window w) => new Pair<string, string>(w.windowName, w.GetComponent<DocsWindow>().openDoc))));
			if (num)
			{
				saveGame.unlocks = mainSim.storedSim.farm.SerializeUnlocks();
			}
			else
			{
				saveGame.unlocks = mainSim.GetSerializedUnlocks();
			}
			saveGame.version = 3;
			string @string = OptionHolder.GetString("activeSave", "Save0");
			try
			{
				string backupPath = CreateBackupPath(@string);
				string pathOfSaveDirectory = GetPathOfSaveDirectory(@string);
				WriteSaveGame(saveGame, pathOfSaveDirectory, backupPath);
			}
			catch (IOException ex)
			{
				Debug.LogError(ex.Message);
				List<WarningPopup.ButtonData> buttonsToAdd = new List<WarningPopup.ButtonData>
				{
					new WarningPopup.ButtonData("ok", mainSim.warningPopup.Close)
				};
				mainSim.warningPopup.ShowPopup(CodeUtilities.LocalizeAndFormat("popup_warning_failed_write_save", @string), buttonsToAdd);
			}
			mainSim.dirty = false;
		}
	}

	public static void SaveCode(MainSim mainSim)
	{
		string @string = OptionHolder.GetString("activeSave", "Save0");
		try
		{
			string backupPath = CreateBackupPath(@string);
			string pathOfSaveDirectory = GetPathOfSaveDirectory(@string);
			WriteCodeFiles(mainSim.workspace, pathOfSaveDirectory, backupPath);
		}
		catch (IOException ex)
		{
			Debug.LogError(ex.Message);
			List<WarningPopup.ButtonData> buttonsToAdd = new List<WarningPopup.ButtonData>
			{
				new WarningPopup.ButtonData("ok", mainSim.warningPopup.Close)
			};
			mainSim.warningPopup.ShowPopup(CodeUtilities.LocalizeAndFormat("popup_warning_failed_write_save", @string), buttonsToAdd);
		}
	}

	private static void WriteSaveGame(SaveGame sg, string savePath, string backupPath)
	{
		string content = JsonUtility.ToJson((object)sg);
		FileInfo destinationFile = new FileInfo(savePath + "/save.json");
		WriteFilesSafely(new FileInfo(backupPath + "/save.json"), destinationFile, content);
	}

	private static void WriteCodeFiles(Workspace workspace, string savePath, string backupPath)
	{
		foreach (CodeWindow value in workspace.codeWindows.Values)
		{
			FileInfo destinationFile = new FileInfo($"{savePath}/{value.fileName}.py");
			WriteFilesSafely(new FileInfo($"{backupPath}/{value.fileName}.py"), destinationFile, value.CodeInput.text);
		}
		foreach (string item in Enumerable.Where<string>((IEnumerable<string>)Directory.GetFiles(savePath), (Func<string, bool>)((string f) => Path.GetExtension(f) == ".py" && !workspace.IsWindowOpen(Path.GetFileNameWithoutExtension(f)) && !f.EndsWith("__builtins__.py"))))
		{
			try
			{
				File.Delete(item);
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message);
			}
		}
		FileInfo fileInfo = new FileInfo(savePath + "/__builtins__.py");
		if (!fileInfo.Exists)
		{
			string contents = Localizer.LoadBuiltins();
			try
			{
				File.WriteAllText(fileInfo.FullName, contents);
			}
			catch (Exception ex2)
			{
				Debug.LogError(ex2.Message);
			}
		}
	}

	private static string CreateBackupPath(string fileName)
	{
		string text = Helper.persistentDataPath + "/Backup";
		Directory.CreateDirectory(text);
		List<(int, string)> list = Enumerable.ToList<(int, string)>(Enumerable.Select<string, (int, string)>((IEnumerable<string>)Directory.GetDirectories(text, fileName + "*", SearchOption.TopDirectoryOnly), (Func<string, (int, string)>)delegate(string s)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(s);
			int num = fileName.Length + "_backup".Length;
			if (directoryInfo.Name.Length <= num)
			{
				return (0, "");
			}
			int result;
			return int.TryParse(directoryInfo.Name.Substring(num), out result) ? (result, s) : (0, "");
		}));
		list.Sort();
		while (true)
		{
			int num2 = -1;
			int num3 = int.MaxValue;
			int num4 = int.MaxValue;
			for (int i = 1; i < list.Count; i++)
			{
				int num5 = list[i].Item1 - list[i - 1].Item1;
				if (num4 <= num5 && num4 <= num3)
				{
					num2 = i - 2;
					break;
				}
				num4 = num3;
				num3 = num5;
			}
			if (num2 < 0)
			{
				break;
			}
			try
			{
				Directory.Delete(list[num2].Item2, recursive: true);
			}
			catch (Exception)
			{
			}
			list.RemoveAt(num2);
		}
		int num6 = ((list.Count > 0) ? (Enumerable.Last<(int, string)>((IEnumerable<(int, string)>)list).Item1 + 1) : 0);
		return $"{text}/{fileName}_backup{num6}";
	}

	private static void WriteFilesSafely(FileInfo backupFile, FileInfo destinationFile, string content)
	{
		backupFile.Directory.Create();
		destinationFile.Directory.Create();
		try
		{
			File.WriteAllText(backupFile.FullName, content);
			File.Copy(backupFile.FullName, destinationFile.FullName, overwrite: true);
		}
		catch (Exception ex)
		{
			Debug.LogError(ex.Message);
		}
	}

	public static void Load(MainSim mainSim)
	{
		SaveGame currentSaveGame = GetCurrentSaveGame();
		if (currentSaveGame != null)
		{
			currentSaveGame.items.Deserialize();
			mainSim.SetupSim(currentSaveGame.unlocks, new ItemBlock(currentSaveGame.items), null, null, currentSaveGame.version < 3);
			Vector2 anchoredPosition = ((RectTransform)mainSim.workspace.container.GetChild(0)).anchoredPosition;
			DirectoryInfo directoryInfo = new DirectoryInfo(GetPathOfSaveDirectory(OptionHolder.GetString("activeSave", "Save0")));
			directoryInfo.Create();
			IEnumerable<string> enumerable = Enumerable.Where<string>((IEnumerable<string>)Directory.GetFiles(directoryInfo.FullName), (Func<string, bool>)((string f) => Path.GetExtension(f) == ".py" && Path.GetFileName(f) != "__builtins__.py"));
			IEnumerable<string> enumerable2 = Enumerable.Select<string, string>(enumerable, (Func<string, string>)((string f) => Path.GetFileNameWithoutExtension(f)));
			IEnumerable<string> enumerable3 = Enumerable.Select<string, string>(enumerable, (Func<string, string>)((string f) => File.ReadAllText(f).Replace("\r", "")));
			Dictionary<string, Vector2> dictionary = Enumerable.ToDictionary<Pair<string, Vector2>, string, Vector2>((IEnumerable<Pair<string, Vector2>>)currentSaveGame.openFilePositions, (Func<Pair<string, Vector2>, string>)((Pair<string, Vector2> x) => x.key), (Func<Pair<string, Vector2>, Vector2>)((Pair<string, Vector2> x) => x.value));
			Dictionary<string, Vector2> dictionary2 = Enumerable.ToDictionary<Pair<string, Vector2>, string, Vector2>((IEnumerable<Pair<string, Vector2>>)currentSaveGame.openFileSizes, (Func<Pair<string, Vector2>, string>)((Pair<string, Vector2> x) => x.key), (Func<Pair<string, Vector2>, Vector2>)((Pair<string, Vector2> x) => x.value));
			foreach (var item in Enumerable.Zip<string, string, (string, string)>(enumerable2, enumerable3, (Func<string, string, (string, string)>)((string file, string code) => (file: file, code: code))))
			{
				mainSim.workspace.OpenCodeWindow(item.Item1, item.Item2, dictionary.GetValueOrDefault(item.Item1) + anchoredPosition, dictionary2.GetValueOrDefault(item.Item1));
			}
			foreach (Pair<string, string> openDocPage in currentSaveGame.openDocPages)
			{
				mainSim.workspace.OpenDocsWindow(openDocPage.key, openDocPage.value, dictionary.GetValueOrDefault(openDocPage.key) + anchoredPosition, dictionary2.GetValueOrDefault(openDocPage.key));
			}
			foreach (Pair<string, float> openFileScrollPosition in currentSaveGame.openFileScrollPositions)
			{
				if (mainSim.workspace.TryGetOpenWindow(openFileScrollPosition.key, out var window) && window.TryGetComponent<CodeWindow>(out var component))
				{
					((RectTransform)component.CodeInput.transform).anchoredPosition += new Vector2(0f, openFileScrollPosition.value);
				}
			}
			foreach (string minimizedFile in currentSaveGame.minimizedFiles)
			{
				if (mainSim.workspace.TryGetOpenWindow(minimizedFile, out var window2))
				{
					window2.SetMinmized(minimized: true);
				}
			}
			for (int i = 0; i < currentSaveGame.dockedFiles.Count; i++)
			{
				if (mainSim.workspace.TryGetOpenWindow(currentSaveGame.dockedFiles[i].key, out var window3) && mainSim.workspace.TryGetOpenWindow(currentSaveGame.dockedFiles[i].value, out var window4))
				{
					window3.DockOnto(window4);
				}
			}
			if (currentSaveGame.version < 3)
			{
				mainSim.workspace.AddNewDocsWindow("docs/patchnotes.md");
				List<WarningPopup.ButtonData> buttonsToAdd = new List<WarningPopup.ButtonData>
				{
					new WarningPopup.ButtonData("ok", mainSim.warningPopup.Close)
				};
				mainSim.warningPopup.ShowPopup("popup_warning_old_save", buttonsToAdd);
			}
		}
		else
		{
			mainSim.SetupSim(Enumerable.Empty<string>(), ItemBlock.CreateEmpty());
			mainSim.workspace.OpenCodeWindow("main", "", new Vector2(300f, -150f));
			mainSim.workspace.AddNewDocsWindow("docs/getting_started.md", new Vector2(-300f, 100f));
		}
		mainSim.researchMenu.Setup();
		mainSim.inv.SetUp(mainSim.GetInventory);
		LeanTween.init(1600);
		mainSim.dirty = false;
		StartFileWatcher();
	}

	public static void SaveTitleSave(Menu menu)
	{
		TitleSave titleSave = new TitleSave();
		IEnumerable<Window> openWindows = menu.titleWorkspace.OpenWindows;
		titleSave.openTitlePages = Enumerable.ToList<Pair<string, string>>(Enumerable.Select<(Window, TitleWindow), Pair<string, string>>(Enumerable.Where<(Window, TitleWindow)>(Enumerable.Select<Window, (Window, TitleWindow)>(openWindows, (Func<Window, (Window, TitleWindow)>)((Window w) => (window: w, title: w.GetComponent<TitleWindow>()))), (Func<(Window, TitleWindow), bool>)(((Window window, TitleWindow title) d) => d.title != null)), (Func<(Window, TitleWindow), Pair<string, string>>)(((Window window, TitleWindow title) w) => new Pair<string, string>(w.window.windowName, w.title.openPage))));
		titleSave.openFilePositions = Enumerable.ToList<Pair<string, Vector2>>(Enumerable.Select<Window, Pair<string, Vector2>>(openWindows, (Func<Window, Pair<string, Vector2>>)((Window f) => new Pair<string, Vector2>(f.windowName, f.WindowRect.anchoredPosition))));
		titleSave.openFileSizes = Enumerable.ToList<Pair<string, Vector2>>(Enumerable.Select<Window, Pair<string, Vector2>>(openWindows, (Func<Window, Pair<string, Vector2>>)((Window f) => new Pair<string, Vector2>(f.windowName, f.playerSetSize))));
		titleSave.dockedFiles = Enumerable.ToList<Pair<string, string>>(Enumerable.Select<Window, Pair<string, string>>(Enumerable.Where<Window>(openWindows, (Func<Window, bool>)((Window f) => f.dockedParent != null)), (Func<Window, Pair<string, string>>)((Window f) => new Pair<string, string>(f.windowName, f.dockedParent.windowName))));
		titleSave.minimizedFiles = Enumerable.ToList<string>(Enumerable.Select<Window, string>(Enumerable.Where<Window>(openWindows, (Func<Window, bool>)((Window f) => f.isMinimized)), (Func<Window, string>)((Window f) => f.windowName)));
		HashSet<string> windowHistory = menu.titleWorkspace.windowHistory;
		titleSave.windowHistory = ((windowHistory != null) ? Enumerable.ToList<string>((IEnumerable<string>)windowHistory) : null);
		try
		{
			string contents = JsonUtility.ToJson((object)titleSave);
			FileInfo fileInfo = new FileInfo(Helper.persistentDataPath + "/Title.json");
			fileInfo.Directory.Create();
			File.WriteAllText(fileInfo.FullName, contents);
		}
		catch (IOException ex)
		{
			Debug.LogError(ex.Message);
		}
	}

	public static void LoadTitleSave(Menu menu)
	{
		FileInfo fileInfo = new FileInfo(Helper.persistentDataPath + "/Title.json");
		if (!fileInfo.Exists)
		{
			return;
		}
		TitleSave titleSave = JsonUtility.FromJson<TitleSave>(File.ReadAllText(fileInfo.FullName));
		Dictionary<string, Vector2> dictionary = Enumerable.ToDictionary<Pair<string, Vector2>, string, Vector2>((IEnumerable<Pair<string, Vector2>>)titleSave.openFilePositions, (Func<Pair<string, Vector2>, string>)((Pair<string, Vector2> x) => x.key), (Func<Pair<string, Vector2>, Vector2>)((Pair<string, Vector2> x) => x.value));
		Dictionary<string, Vector2> dictionary2 = Enumerable.ToDictionary<Pair<string, Vector2>, string, Vector2>((IEnumerable<Pair<string, Vector2>>)titleSave.openFileSizes, (Func<Pair<string, Vector2>, string>)((Pair<string, Vector2> x) => x.key), (Func<Pair<string, Vector2>, Vector2>)((Pair<string, Vector2> x) => x.value));
		foreach (Pair<string, string> openTitlePage in titleSave.openTitlePages)
		{
			menu.titleWorkspace.OpenWindow(openTitlePage.key, openTitlePage.value, dictionary.GetValueOrDefault(openTitlePage.key), dictionary2.GetValueOrDefault(openTitlePage.key));
		}
		foreach (string minimizedFile in titleSave.minimizedFiles)
		{
			if (menu.titleWorkspace.TryGetOpenWindow(minimizedFile, out var window))
			{
				window.SetMinmized(minimized: true);
			}
		}
		for (int i = 0; i < titleSave.dockedFiles.Count; i++)
		{
			if (menu.titleWorkspace.TryGetOpenWindow(titleSave.dockedFiles[i].key, out var window2) && menu.titleWorkspace.TryGetOpenWindow(titleSave.dockedFiles[i].value, out var window3))
			{
				window2.DockOnto(window3);
			}
		}
		TitleWorkspace titleWorkspace = menu.titleWorkspace;
		if (titleWorkspace.windowHistory == null)
		{
			titleWorkspace.windowHistory = new HashSet<string>();
		}
		foreach (string item in titleSave.windowHistory)
		{
			menu.titleWorkspace.windowHistory.Add(item);
		}
	}

	public static void StartFileWatcher()
	{
		string pathOfSaveDirectory = GetPathOfSaveDirectory(OptionHolder.GetString("activeSave", "Save0"));
		if (!Directory.Exists(pathOfSaveDirectory))
		{
			Directory.CreateDirectory(pathOfSaveDirectory);
		}
		if (watcher != null)
		{
			watcher.Dispose();
		}
		watcher = new FileSystemWatcher(pathOfSaveDirectory);
		watcher.NotifyFilter = watcher.NotifyFilter | NotifyFilters.LastWrite | NotifyFilters.FileName;
		watcher.EnableRaisingEvents = true;
		EnableDisableFileWatcher("file watcher");
		OptionHolder.OnOptionChanged -= EnableDisableFileWatcher;
		OptionHolder.OnOptionChanged += EnableDisableFileWatcher;
	}

	public static void StopFileWatcher()
	{
		if (watcher != null)
		{
			watcher.Dispose();
			watcher = null;
		}
	}

	private static void EnableDisableFileWatcher(string optionName)
	{
		if (optionName == "autosave")
		{
			if (OptionHolder.GetString("autosave") == "enabled" && OptionHolder.GetString("file watcher") == "enabled")
			{
				OptionHolder.SetOption("file watcher", "disabled");
			}
		}
		else
		{
			if (optionName != "file watcher")
			{
				return;
			}
			if (OptionHolder.GetString("file watcher") == "enabled")
			{
				if (OptionHolder.GetString("autosave") == "enabled")
				{
					OptionHolder.SetOption("autosave", "disabled");
				}
				watcher.Changed -= OnFileChanged;
				watcher.Changed += OnFileChanged;
			}
			else
			{
				watcher.Changed -= OnFileChanged;
			}
		}
	}

	private static void OnFileChanged(object sender, FileSystemEventArgs e)
	{
		if (!e.Name.EndsWith(".py"))
		{
			return;
		}
		string item = e.Name.Substring(0, e.Name.Length - 3);
		lock (codeToUpdateLock)
		{
			codeToUpdate.Add((item, File.ReadAllText(e.FullPath)));
		}
	}

	public static void ApplyCodeChanges(Workspace workspace)
	{
		if (codeToUpdate.Count <= 0)
		{
			return;
		}
		lock (codeToUpdateLock)
		{
			foreach (var item in codeToUpdate)
			{
				if (workspace.codeWindows.TryGetValue(item.Item1, out var value))
				{
					value.CodeInput.text = item.Item2;
				}
			}
			codeToUpdate.Clear();
		}
	}

	public static void UpdateFromOldSaveGame(OldSaveGame oldSaveGame)
	{
		string @string = OptionHolder.GetString("activeSave", "Save0");
		SaveGame saveGame = new SaveGame();
		saveGame.unlocks = oldSaveGame.unlocks;
		saveGame.items = oldSaveGame.items;
		saveGame.openDocPages = new List<Pair<string, string>>();
		saveGame.minimizedFiles = oldSaveGame.minimized;
		saveGame.openFilePositions = new List<Pair<string, Vector2>>();
		saveGame.openFileSizes = new List<Pair<string, Vector2>>();
		saveGame.dockedFiles = new List<Pair<string, string>>();
		for (int i = 0; i < oldSaveGame.functionNames.Count; i++)
		{
			FileInfo fileInfo = new FileInfo(Helper.persistentDataPath + $"/Saves/{@string}/{oldSaveGame.functionNames[i]}.py");
			fileInfo.Directory.Create();
			File.WriteAllText(fileInfo.FullName, oldSaveGame.functionCode[i]);
			saveGame.openFilePositions.Add(new Pair<string, Vector2>(oldSaveGame.functionNames[i], oldSaveGame.openFunctionPositions[i]));
			if (i < oldSaveGame.functionDocked.Count && oldSaveGame.functionDocked[i] != "")
			{
				saveGame.dockedFiles.Add(new Pair<string, string>(oldSaveGame.functionNames[i], oldSaveGame.functionDocked[i]));
			}
		}
		_ = Helper.persistentDataPath + "/Backup";
		WriteSaveGame(saveGame, GetPathOfSaveDirectory(@string), CreateBackupPath(@string));
	}

	private static SaveGame GetCurrentSaveGame()
	{
		string currentSaveGameJson = GetCurrentSaveGameJson();
		if (!string.IsNullOrEmpty(currentSaveGameJson))
		{
			SaveGame saveGame = JsonUtility.FromJson<SaveGame>(currentSaveGameJson);
			if (saveGame.openFilePositions.Count > 0)
			{
				return saveGame;
			}
			OldSaveGame oldSaveGame = JsonUtility.FromJson<OldSaveGame>(currentSaveGameJson);
			if (oldSaveGame.functionNames.Count > 0)
			{
				UpdateFromOldSaveGame(oldSaveGame);
				currentSaveGameJson = GetCurrentSaveGameJson();
				return JsonUtility.FromJson<SaveGame>(currentSaveGameJson);
			}
			if (saveGame.unlocks.Count > 0 || !saveGame.items.IsEmpty())
			{
				return saveGame;
			}
			return null;
		}
		return null;
	}

	public static bool GetCurrentSaveGameExists()
	{
		return File.Exists(GetPathOfSaveFile(OptionHolder.GetString("activeSave", "Save0")));
	}

	private static string GetCurrentSaveGameJson()
	{
		string @string = OptionHolder.GetString("activeSave", "Save0");
		FileInfo fileInfo = new FileInfo(GetPathOfSaveFile(@string));
		if (!File.Exists(fileInfo.FullName))
		{
			CreateNewSaveGame(@string);
		}
		return File.ReadAllText(fileInfo.FullName);
	}

	public static bool RenameSave(string saveName, string newSaveName)
	{
		if (string.IsNullOrEmpty(newSaveName) || newSaveName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || Directory.Exists(GetPathOfSaveDirectory(newSaveName)))
		{
			return false;
		}
		try
		{
			Directory.Move(GetPathOfSaveDirectory(saveName), GetPathOfSaveDirectory(newSaveName));
		}
		catch (Exception)
		{
			return false;
		}
		return true;
	}

	public static bool DeleteSave(string saveName)
	{
		try
		{
			Directory.Delete(GetPathOfSaveDirectory(saveName), recursive: true);
		}
		catch (Exception)
		{
			return false;
		}
		return true;
	}

	public static void CreateNewSaveGame(string saveName)
	{
		FileInfo fileInfo = new FileInfo(GetPathOfSaveFile(saveName));
		if (!fileInfo.Exists)
		{
			Directory.CreateDirectory(fileInfo.Directory.FullName);
		}
		File.Create(fileInfo.FullName).Dispose();
		new DirectoryInfo(GetPathOfSaveDirectory(saveName)).Create();
	}

	public static string GetPathOfSaveFile(string saveName)
	{
		string text = $"{Helper.persistentDataPath}/Saves/{saveName}/save.json";
		if (!File.Exists(text))
		{
			string text2 = $"{Helper.persistentDataPath}/Saves/{saveName}.json";
			if (File.Exists(text2))
			{
				text = text2;
			}
		}
		return text;
	}

	public static string GetPathOfSaveDirectory(string saveName)
	{
		return $"{Helper.persistentDataPath}/Saves/{saveName}";
	}
}
