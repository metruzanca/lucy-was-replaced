using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResolutionOptionUpdater : MonoBehaviour
{
	private static DropdownOptionSO resolutionOptionSO;

	private static DisplayInfo lastDisplay;

	public static void UpdateOptions()
	{
		if (resolutionOptionSO == null)
		{
			resolutionOptionSO = Resources.Load<DropdownOptionSO>("Options/resolution");
			if (resolutionOptionSO == null)
			{
				Debug.LogError("Failed to load 'resolution' options resource");
				return;
			}
		}
		DisplayInfo mainWindowDisplayInfo = Screen.mainWindowDisplayInfo;
		if (!lastDisplay.Equals(mainWindowDisplayInfo))
		{
			lastDisplay = mainWindowDisplayInfo;
			resolutionOptionSO.defaultValue = $"{Screen.currentResolution.width}x{Screen.currentResolution.height}";
			DropdownOptionSO dropdownOptionSO = resolutionOptionSO;
			if (dropdownOptionSO.options == null)
			{
				dropdownOptionSO.options = new List<string>();
			}
			resolutionOptionSO.options.Clear();
			resolutionOptionSO.options.AddRange(Enumerable.Distinct<string>(Enumerable.Select<Resolution, string>((IEnumerable<Resolution>)Enumerable.OrderByDescending<Resolution, int>((IEnumerable<Resolution>)Screen.resolutions, (Func<Resolution, int>)((Resolution s) => s.width * s.height)), (Func<Resolution, string>)((Resolution r) => $"{r.width}x{r.height}"))));
			string @string = OptionHolder.GetString(resolutionOptionSO.optionName, null);
			if (!string.IsNullOrEmpty(@string) && !resolutionOptionSO.options.Contains(@string))
			{
				resolutionOptionSO.options.Insert(0, @string);
			}
			resolutionOptionSO.TriggerOptionChanged();
		}
	}

	private void Update()
	{
		DisplayInfo mainWindowDisplayInfo = Screen.mainWindowDisplayInfo;
		if (!lastDisplay.Equals(mainWindowDisplayInfo))
		{
			UpdateOptions();
		}
	}
}
