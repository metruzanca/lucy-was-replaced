using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct KeyCombination
{
	public KeyCode key;

	public bool alt;

	public bool ctrl;

	public bool shift;

	public KeyCombination(KeyCode key, bool alt, bool ctrl, bool shift)
	{
		this.key = key;
		this.alt = alt;
		this.ctrl = ctrl;
		this.shift = shift;
	}

	public override string ToString()
	{
		return ToString(display: false);
	}

	public string ToString(bool display)
	{
		string text = "Ctrl";
		string text2 = (ctrl ? (text + " ") : "");
		text2 = (alt ? (text2 + "Alt ") : text2);
		text2 = (shift ? (text2 + "Shift ") : text2);
		return (key != 0) ? (text2 + key) : text2;
	}

	public bool IsKeyPressed(bool pressedRightThisFrame)
	{
		if (pressedRightThisFrame)
		{
			if (!Input.GetKeyDown(key))
			{
				return false;
			}
		}
		else if (!Input.GetKey(key))
		{
			return false;
		}
		if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) != shift)
		{
			return false;
		}
		if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) != alt)
		{
			return false;
		}
		if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) != ctrl)
		{
			return false;
		}
		return true;
	}

	public static bool TryParse(string s, out KeyCombination k)
	{
		string[] array = s.Split(" ");
		if (Enum.TryParse<KeyCode>(array[^1], ignoreCase: true, out var result))
		{
			k = new KeyCombination(result, Enumerable.Contains<string>((IEnumerable<string>)array, "Alt"), Enumerable.Contains<string>((IEnumerable<string>)array, "Ctrl") || Enumerable.Contains<string>((IEnumerable<string>)array, "Cmd"), Enumerable.Contains<string>((IEnumerable<string>)array, "Shift"));
			return true;
		}
		k = new KeyCombination(KeyCode.None, alt: false, ctrl: false, shift: false);
		return false;
	}
}
