using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ThemeManager : MonoBehaviour
{
	public const string ThemeFileExtension = ".tfwrTheme";

	public const int ThemeVersion = 1;

	public const string ThemesSubdir = "Themes";

	public const string ExampleThemeName = "Custom";

	[SerializeField]
	private ColorTheme currentTheme;

	[SerializeField]
	private bool hotReloadCurentTheme;

	private List<ColorTheme> availableThemes;

	private ColorTheme defaultTheme;

	private ColorTheme lastTheme;

	private FileSystemWatcher watcher;

	public static ThemeManager Inst { get; private set; }

	public ColorTheme Theme
	{
		get
		{
			return currentTheme;
		}
		set
		{
			if (!(currentTheme == value))
			{
				lastTheme = (currentTheme = value);
				if (currentTheme == null)
				{
					Debug.Log("Changed Theme to null");
				}
				else
				{
					Debug.Log("Changed Theme to " + currentTheme.theme_name + "\n(" + (currentTheme.IsBuiltin ? "built-in theme" : $"theme at path '{currentTheme.FileUri}'") + ")");
				}
				UpdateWatcher();
				this._onThemeChanged?.Invoke(currentTheme);
			}
		}
	}

	public IEnumerable<ColorTheme> AvailableThemes
	{
		get
		{
			IEnumerable<ColorTheme> enumerable = availableThemes;
			return enumerable ?? Enumerable.Empty<ColorTheme>();
		}
	}

	public string ThemesPath => Path.Combine(Helper.persistentDataPath, "Themes");

	public event Action<ColorTheme> OnThemeChanged
	{
		add
		{
			_onThemeChanged += value;
			if (Theme != null)
			{
				value(Theme);
			}
		}
		remove
		{
			_onThemeChanged -= value;
		}
	}

	private event Action<ColorTheme> _onThemeChanged;

	public void ReloadCurrentTheme()
	{
		if (!(Theme == null))
		{
			if (Theme.IsBuiltin)
			{
				this._onThemeChanged?.Invoke(currentTheme);
			}
			else
			{
				ReloadJsonTheme(Theme.FileUri.LocalPath);
			}
		}
	}

	public ColorTheme FindThemeByName(string themeName)
	{
		if (availableThemes == null || string.IsNullOrEmpty(themeName))
		{
			return null;
		}
		foreach (ColorTheme availableTheme in availableThemes)
		{
			if (!string.IsNullOrEmpty(availableTheme.theme_name) && string.Equals(availableTheme.theme_name, themeName, StringComparison.OrdinalIgnoreCase))
			{
				return availableTheme;
			}
		}
		return null;
	}

	public void ReloadJsonThemes()
	{
		ReloadJsonThemesFrom(ThemesPath);
	}

	public void ReloadJsonThemesFrom(string directory)
	{
		if (!Directory.Exists(directory))
		{
			return;
		}
		foreach (string item in Directory.EnumerateFiles(directory, "*.tfwrTheme"))
		{
			ReloadJsonTheme(item);
		}
	}

	public void ReloadJsonTheme(string path)
	{
		if (!File.Exists(path))
		{
			return;
		}
		try
		{
			if (!Uri.TryCreate(path, UriKind.Absolute, out Uri uri))
			{
				Debug.LogError("ReloadJsonTheme: Failed to create Uri from path: " + path);
				return;
			}
			string text = File.ReadAllText(path);
			ColorTheme colorTheme = ScriptableObject.CreateInstance<ColorTheme>();
			JsonUtility.FromJsonOverwrite(text, (object)colorTheme);
			colorTheme.FileUri = uri;
			if (colorTheme.theme_version != 1)
			{
				Debug.LogWarning($"Theme at path is not compatible (version {colorTheme.theme_version} is not the required version {1}): {path}");
				return;
			}
			if (string.IsNullOrEmpty(colorTheme.theme_name))
			{
				colorTheme.theme_name = Path.GetFileNameWithoutExtension(path);
			}
			colorTheme.name = colorTheme.theme_name;
			int num = availableThemes.FindIndex((ColorTheme t) => t.FileUri == uri);
			if (num >= 0)
			{
				ColorTheme colorTheme2 = availableThemes[num];
				availableThemes[num] = colorTheme;
				if (colorTheme2 == Theme)
				{
					Theme = colorTheme;
				}
				return;
			}
			ColorTheme colorTheme3 = FindThemeByName(colorTheme.theme_name);
			if (colorTheme3 != null)
			{
				Debug.LogWarning("Theme with name '" + colorTheme.theme_name + "' already exists, ignoring file at '" + colorTheme.FileUri.LocalPath + "'\n(conflicts with " + (colorTheme3.IsBuiltin ? "built-in theme" : ("theme at path '" + colorTheme3.FileUri.LocalPath + "'")) + ")");
			}
			else
			{
				availableThemes.Add(colorTheme);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Failed to load theme at '" + path + "': " + ex.Message);
		}
	}

	private void OnEnable()
	{
		Inst = this;
		defaultTheme = (lastTheme = currentTheme);
		InitializeThemesDirectory();
		if (availableThemes == null)
		{
			availableThemes = new List<ColorTheme>();
		}
		availableThemes.Clear();
		ColorTheme[] array = Resources.LoadAll<ColorTheme>("Themes/");
		foreach (ColorTheme item in array)
		{
			availableThemes.Add(item);
		}
		_ = availableThemes.Count;
		ReloadJsonThemes();
		Debug.Log($"Loaded {availableThemes.Count} themes:\n- " + string.Join("\n- ", Enumerable.Select<ColorTheme, string>((IEnumerable<ColorTheme>)availableThemes, (Func<ColorTheme, string>)((ColorTheme t) => t.theme_name + " (" + (t.IsBuiltin ? "built-in" : t.FileUri.LocalPath) + ")"))));
	}

	private void Start()
	{
		string @string = OptionHolder.GetString("color theme", null);
		if (@string != null)
		{
			ColorTheme colorTheme = FindThemeByName(@string);
			if (colorTheme == null)
			{
				Debug.LogError("Could not find saved theme with name: '" + @string + "'");
				return;
			}
			Debug.Log("Restoring selected Theme from settings: " + colorTheme.theme_name);
			Theme = colorTheme;
		}
		else
		{
			Debug.Log("No 'color theme' option set, using Default theme");
		}
	}

	private void OnDisable()
	{
		if (watcher != null)
		{
			watcher.Dispose();
			watcher = null;
		}
	}

	private void InitializeThemesDirectory()
	{
		try
		{
			if (!Directory.Exists(ThemesPath))
			{
				Directory.CreateDirectory(ThemesPath);
				ColorTheme colorTheme = UnityEngine.Object.Instantiate(defaultTheme);
				colorTheme.theme_name = "Custom";
				colorTheme.author = "";
				colorTheme.FileUri = null;
				File.WriteAllText(Path.Combine(ThemesPath, "Custom.tfwrTheme"), JsonUtility.ToJson((object)colorTheme, true));
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Error initializing themes directory '" + ThemesPath + "': " + ex.Message);
		}
	}

	private void UpdateWatcher()
	{
		if (!hotReloadCurentTheme || !(currentTheme != null) || !(currentTheme.FileUri != null) || !File.Exists(currentTheme.FileUri.LocalPath))
		{
			if (watcher != null)
			{
				watcher.EnableRaisingEvents = false;
			}
			return;
		}
		if (watcher == null)
		{
			if (watcher == null)
			{
				watcher = new FileSystemWatcher();
			}
			watcher.NotifyFilter = NotifyFilters.LastWrite;
			watcher.Changed += OnWatcherChanged;
			watcher.Error += OnWatcherError;
		}
		string localPath = currentTheme.FileUri.LocalPath;
		watcher.Path = Path.GetDirectoryName(localPath);
		watcher.Filter = Path.GetFileName(localPath);
		watcher.EnableRaisingEvents = true;
	}

	private void OnWatcherChanged(object sender, FileSystemEventArgs args)
	{
		ReloadThemeOnMainThread(args.FullPath);
	}

	private void OnWatcherError(object sender, ErrorEventArgs args)
	{
		Debug.LogError("Theme Watching Failed: " + args.GetException().Message);
	}

	private async Awaitable ReloadThemeOnMainThread(string path)
	{
		try
		{
			await Awaitable.MainThreadAsync();
			ReloadJsonTheme(path);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	private void OnValidate()
	{
		if (lastTheme != currentTheme)
		{
			lastTheme = currentTheme;
			TriggerOnThemeChangedDelayed();
		}
	}

	private async Awaitable TriggerOnThemeChangedDelayed()
	{
		try
		{
			await Awaitable.NextFrameAsync(base.destroyCancellationToken);
			this._onThemeChanged?.Invoke(currentTheme);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}
}
