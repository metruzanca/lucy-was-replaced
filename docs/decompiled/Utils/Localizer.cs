using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

public class Localizer
{
	private static Dictionary<string, string> en;

	private static Dictionary<string, string> language;

	private static List<TMP_FontAsset> fallbackFonts;

	public static string Lang { get; private set; }

	public static string Localize(string key)
	{
		if (language == null)
		{
			LoadLang(OptionHolder.GetString("language", "EN"));
		}
		if (language.ContainsKey(key))
		{
			return CodeUtilities.ApplyCodeTags(language[key]);
		}
		if (en == null)
		{
			en = LoadFromDisk("EN");
		}
		if (en.ContainsKey(key))
		{
			return CodeUtilities.ApplyCodeTags(en[key]);
		}
		return key;
	}

	private static Dictionary<string, string> LoadFromDisk(string l)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		string path = Path.Combine(Application.streamingAssetsPath, "Languages", l, "Strings");
		if (!Directory.Exists(path))
		{
			Debug.LogError("Missing language directory for " + l);
			return dictionary;
		}
		foreach (string item in Directory.EnumerateFiles(path))
		{
			if (!item.EndsWith(".txt"))
			{
				continue;
			}
			string[] array = File.ReadAllText(item).Split(new string[4] { "\r\n\r\n@", "\n\n@", "\r\n@", "\n@" }, StringSplitOptions.None);
			array[0] = array[0].Substring(1);
			string[] array2 = array;
			foreach (string text in array2)
			{
				string[] array3 = text.Split(new string[1] { " = " }, 2, StringSplitOptions.None);
				if (array3.Length != 2)
				{
					Debug.LogError("couldn't localize " + text);
				}
				dictionary[array3[0]] = array3[1];
			}
		}
		return dictionary;
	}

	public static void LoadLang(string l)
	{
		if (Lang == l)
		{
			return;
		}
		Lang = l;
		if (!Directory.Exists(Path.Combine(Application.streamingAssetsPath, "Languages", Lang, "Strings")))
		{
			Debug.LogError("Missing language directory for " + Lang);
			if (Lang != "EN")
			{
				Debug.LogError("Attempting to load EN instead");
				LoadLang("EN");
			}
			return;
		}
		if (fallbackFonts == null)
		{
			fallbackFonts = new List<TMP_FontAsset>();
			fallbackFonts.AddRange(Enumerable.Skip<TMP_FontAsset>((IEnumerable<TMP_FontAsset>)TMP_Settings.fallbackFontAssets, 1));
		}
		if (Lang == "ZH")
		{
			TMP_Settings.fallbackFontAssets[0] = fallbackFonts[0];
		}
		else if (Lang == "JA")
		{
			TMP_Settings.fallbackFontAssets[0] = fallbackFonts[1];
		}
		else if (Lang == "KO")
		{
			TMP_Settings.fallbackFontAssets[0] = fallbackFonts[2];
		}
		else if (Lang == "TW")
		{
			TMP_Settings.fallbackFontAssets[0] = fallbackFonts[3];
		}
		language = LoadFromDisk(Lang);
	}

	public static string LoadDoc(string doc)
	{
		if (Lang == null)
		{
			LoadLang(OptionHolder.GetString("language", "EN"));
		}
		string path = Path.Combine(Application.streamingAssetsPath, "Languages", Lang, doc);
		if (File.Exists(path))
		{
			return File.ReadAllText(path);
		}
		return "";
	}

	public static string LoadBuiltins()
	{
		return File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Languages", "builtins.py"));
	}

	public static List<string> GetAvailableLanguages()
	{
		List<string> list = new List<string>();
		string path = Path.Combine(Application.streamingAssetsPath, "Languages");
		if (!Directory.Exists(path))
		{
			Debug.LogError("Missing Languages directory");
			return list;
		}
		foreach (string item in Directory.EnumerateDirectories(path))
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(item);
			if (!directoryInfo.Name.StartsWith("."))
			{
				list.Add(directoryInfo.Name);
			}
		}
		return list;
	}
}
