using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Steamworks;
using UnityEngine;

public static class OptionHolder
{
	public delegate void OptionChangedAction(string item);

	private static Dictionary<string, object> options;

	public static event OptionChangedAction OnOptionChanged;

	public static object GetOption(string optionName, object defaultValue = null)
	{
		if (options == null)
		{
			LoadOptions();
		}
		return options.GetValueOrDefault(optionName, defaultValue);
	}

	public static float GetFloat(string optionName, float defaultValue = 0f)
	{
		object option = GetOption(optionName);
		if (option is float)
		{
			return (float)option;
		}
		return defaultValue;
	}

	public static string GetString(string optionName, string defaultValue = "")
	{
		object option = GetOption(optionName);
		if (option is string)
		{
			return (string)option;
		}
		return defaultValue;
	}

	public static KeyCombination GetKeyCombination(string optionName)
	{
		object option = GetOption(optionName);
		if (option is KeyCombination)
		{
			return (KeyCombination)option;
		}
		return new KeyCombination(KeyCode.None, alt: false, ctrl: false, shift: false);
	}

	public static void SetOption(string optionName, object value)
	{
		if (options == null)
		{
			LoadOptions();
		}
		options[optionName] = value;
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, object> option in options)
		{
			stringBuilder.Append($"{option.Key} = {Convert.ToString(option.Value, CultureInfo.InvariantCulture)}\n");
		}
		FileInfo fileInfo = new FileInfo(Helper.persistentDataPath + "/options.txt");
		fileInfo.Directory.Create();
		File.WriteAllText(fileInfo.FullName, stringBuilder.ToString());
		switch (optionName)
		{
		case "graphics":
			SetGraphics();
			break;
		case "screen mode":
		case "resolution":
			SetScreenMode();
			break;
		case "frames":
			SetFrameLimit();
			break;
		case "language":
		{
			string string2 = GetString("language", "EN");
			Localizer.LoadLang(string2);
			if (string2 != "EN" && string2 != "DE" && string2 != "FR" && string2 != "ES" && string2 != "IT" && string2 != "JA" && string2 != "KO" && string2 != "PL" && string2 != "PT" && string2 != "RU" && string2 != "ZH")
			{
				string text = Localizer.Localize("popup_warning_unofficial_language");
				List<WarningPopup.ButtonData> buttonsToAdd = new List<WarningPopup.ButtonData>
				{
					new WarningPopup.ButtonData("ok", delegate
					{
						WarningPopup.Inst.Close();
					})
				};
				WarningPopup.Inst.ShowPopup(text, buttonsToAdd);
			}
			break;
		}
		case "volume":
			SetVolume();
			break;
		case "ambience volume":
			FMODSoundManager.SetAmbienceVCAVolume(GetFloat("ambience volume", 1f) * GetFloat("volume", 1f));
			break;
		case "music volume":
			FMODSoundManager.SetMusicVCAVolume(GetFloat("music volume", 1f) * GetFloat("volume", 1f));
			break;
		case "drone volume":
			FMODSoundManager.SetDroneVCAVolume(GetFloat("drone volume", 1f) * GetFloat("volume", 1f));
			break;
		case "SFX volume":
			FMODSoundManager.SetSFXVCAVolume(GetFloat("SFX volume", 1f) * GetFloat("volume", 1f));
			break;
		case "UI volume":
			FMODSoundManager.SetUIVCAVolume(GetFloat("UI volume", 1f) * GetFloat("volume", 1f));
			break;
		case "color theme":
		{
			string @string = GetString("color theme", "Default");
			ColorTheme colorTheme = ThemeManager.Inst.FindThemeByName(@string);
			if (colorTheme == null)
			{
				Debug.LogError("Could not find selected theme with name: '" + @string + "'");
			}
			else
			{
				ThemeManager.Inst.Theme = colorTheme;
			}
			break;
		}
		}
		OptionHolder.OnOptionChanged?.Invoke(optionName);
	}

	private static void LoadOptions()
	{
		options = new Dictionary<string, object>();
		Dictionary<string, OptionSO> dictionary = new Dictionary<string, OptionSO>();
		OptionSO[] array = Resources.LoadAll<OptionSO>("Options/");
		foreach (OptionSO optionSO in array)
		{
			if (!dictionary.TryAdd(optionSO.optionName, optionSO))
			{
				Debug.LogError("LoadOptions: Duplicate option name '" + optionSO.optionName + "'");
				continue;
			}
			if (optionSO.optionName == "resolution")
			{
				ResolutionOptionUpdater.UpdateOptions();
			}
			if (optionSO.optionName == "language")
			{
				if (SteamManager.Initialized)
				{
					string currentGameLanguage = SteamApps.GetCurrentGameLanguage();
					OptionSO optionSO2 = optionSO;
					optionSO2.defaultValue = currentGameLanguage switch
					{
						"german" => "DE", 
						"french" => "FR", 
						"spanish" => "ES", 
						"italian" => "IT", 
						"japanese" => "JA", 
						"koreana" => "KO", 
						"polish" => "PL", 
						"portuguese" => "PT", 
						"brazilian" => "PT", 
						"russian" => "RU", 
						"schinese" => "ZH", 
						_ => "EN", 
					};
				}
				DropdownOptionSO dropdownOptionSO = optionSO as DropdownOptionSO;
				dropdownOptionSO.options.Clear();
				dropdownOptionSO.options.AddRange(new string[11]
				{
					"EN", "DE", "FR", "ES", "IT", "JA", "KO", "PL", "PT", "RU",
					"ZH"
				});
				dropdownOptionSO.options.Add("");
				foreach (string availableLanguage in Localizer.GetAvailableLanguages())
				{
					if (!dropdownOptionSO.options.Contains(availableLanguage))
					{
						dropdownOptionSO.options.Add(availableLanguage);
					}
				}
			}
			if (optionSO.optionName == "color theme")
			{
				DropdownOptionSO obj = (DropdownOptionSO)optionSO;
				obj.options.Clear();
				obj.options.AddRange(Enumerable.Select<ColorTheme, string>((IEnumerable<ColorTheme>)Enumerable.OrderBy<ColorTheme, string>(ThemeManager.Inst.AvailableThemes, (Func<ColorTheme, string>)((ColorTheme t) => t.theme_name)), (Func<ColorTheme, string>)((ColorTheme t) => t.theme_name)));
			}
			options[optionSO.optionName] = ValueOfString(optionSO.defaultValue, optionSO.ValueType);
		}
		FileInfo fileInfo = new FileInfo(Helper.persistentDataPath + "/options.txt");
		if (!File.Exists(fileInfo.FullName))
		{
			return;
		}
		string[] array2 = File.ReadAllText(fileInfo.FullName).Split('\n');
		for (int i = 0; i < array2.Length; i++)
		{
			string[] array3 = array2[i].Split(" = ");
			if (array3.Length == 2)
			{
				OptionValueType valueType = OptionValueType.Unknown;
				if (dictionary.TryGetValue(array3[0], out var value))
				{
					valueType = value.ValueType;
				}
				options[array3[0]] = ValueOfString(array3[1], valueType);
			}
		}
		SetGraphics();
		SetVolume();
	}

	private static void SetGraphics()
	{
		switch (options["graphics"] as string)
		{
		case "high":
			QualitySettings.SetQualityLevel(2);
			break;
		case "medium":
			QualitySettings.SetQualityLevel(1);
			break;
		case "low":
			QualitySettings.SetQualityLevel(0);
			break;
		}
		SetFrameLimit();
	}

	private static void SetVolume()
	{
		FMODSoundManager.SetAmbienceVCAVolume(GetFloat("ambience volume", 1f) * GetFloat("volume", 1f));
		FMODSoundManager.SetMusicVCAVolume(GetFloat("music volume", 1f) * GetFloat("volume", 1f));
		FMODSoundManager.SetDroneVCAVolume(GetFloat("drone volume", 1f) * GetFloat("volume", 1f));
		FMODSoundManager.SetSFXVCAVolume(GetFloat("SFX volume", 1f) * GetFloat("volume", 1f));
		FMODSoundManager.SetUIVCAVolume(GetFloat("UI volume", 1f) * GetFloat("volume", 1f));
	}

	private static void SetScreenMode()
	{
		switch (options["screen mode"] as string)
		{
		case "fullscreen":
		{
			string[] array = GetString("resolution", $"{Screen.currentResolution.width}x{Screen.currentResolution.height}").Split('x');
			if (array.Length != 2 || !int.TryParse(array[0], out var result) || !int.TryParse(array[1], out var result2))
			{
				Debug.LogError("Invalid resolution format. Expected 'widthxheight'. Using current resolution instead.");
				result = Screen.currentResolution.width;
				result2 = Screen.currentResolution.height;
			}
			Screen.SetResolution(result, result2, FullScreenMode.FullScreenWindow);
			break;
		}
		case "windowed":
			Screen.fullScreenMode = FullScreenMode.Windowed;
			break;
		}
	}

	private static void SetFrameLimit()
	{
		switch (options["frames"] as string)
		{
		case "full vsync":
			QualitySettings.vSyncCount = 1;
			Application.targetFrameRate = 60;
			break;
		case "half vsync":
			QualitySettings.vSyncCount = 2;
			Application.targetFrameRate = 30;
			break;
		case "1/4 vsync":
			QualitySettings.vSyncCount = 4;
			Application.targetFrameRate = 15;
			break;
		case "60 fps":
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = 60;
			break;
		case "30 fps":
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = 30;
			break;
		case "10 fps":
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = 10;
			break;
		}
	}

	public static object ValueOfString(string s, OptionValueType valueType)
	{
		if ((valueType == OptionValueType.Unknown || valueType == OptionValueType.Float) && float.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		if ((valueType == OptionValueType.Unknown || valueType == OptionValueType.KeyCombination) && KeyCombination.TryParse(s, out var k))
		{
			return k;
		}
		return s;
	}
}
