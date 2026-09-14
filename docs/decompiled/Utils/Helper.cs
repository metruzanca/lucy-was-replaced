using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEngine;

public static class Helper
{
	public static string persistentDataPath => Application.persistentDataPath;

	public static void SetAnchoredPoint(RectTransform rectTransform, Vector2 anchorPoint)
	{
		Vector2 vector = rectTransform.localPosition;
		rectTransform.anchorMin = anchorPoint;
		rectTransform.anchorMax = anchorPoint;
		rectTransform.localPosition = vector;
	}

	public static string NrToText(double nr)
	{
		if (nr == 0.0)
		{
			return "0";
		}
		int num = Math.Max(0, (int)Math.Log10(nr) / 3);
		double num2 = nr / Math.Pow(10.0, 3 * num);
		double num3 = ((num2 < 10.0) ? 0.01 : ((num2 < 100.0) ? 0.1 : 1.0));
		string text = (Math.Truncate(num2 * 1.00001 / num3) * num3).ToString("0.##", CultureInfo.InvariantCulture);
		return num switch
		{
			0 => text, 
			1 => text + "k", 
			2 => text + "M", 
			3 => text + "B", 
			4 => text + "T", 
			5 => text + "q", 
			6 => text + "Q", 
			7 => text + "s", 
			8 => text + "S", 
			9 => text + "O", 
			10 => text + "N", 
			11 => text + "D", 
			_ => nr.ToString(CultureInfo.InvariantCulture), 
		};
	}

	public static bool ContainsAnywhere(string a, string b)
	{
		if (a == null || b == null)
		{
			throw new NullReferenceException();
		}
		int i = 0;
		int num = 0;
		for (; i < a.Length; i++)
		{
			if (num >= b.Length)
			{
				break;
			}
			if (a[i] == b[num])
			{
				num++;
			}
		}
		return num == b.Length;
	}

	public static string StripRichText(string a)
	{
		Regex regex = new Regex("<\\/?(font[^>]*|color[^>]*)>");
		if (regex.IsMatch(a))
		{
			return regex.Replace(a, string.Empty);
		}
		return a;
	}

	public static bool IsValidNameChar(char c)
	{
		if (!char.IsLetterOrDigit(c))
		{
			return c == '_';
		}
		return true;
	}

	public static double RoundToNDecimalDigits(double d, int n)
	{
		int num = (int)Math.Floor(Math.Log10(Math.Abs(d)));
		double num2 = Math.Pow(10.0, num - n + 1);
		return Math.Round(d / num2) * num2;
	}

	public static int WorldSizeScale(int numExpandUpgrades)
	{
		return numExpandUpgrades switch
		{
			0 => 1, 
			1 => 2, 
			2 => 3, 
			3 => 4, 
			4 => 6, 
			5 => 8, 
			6 => 12, 
			7 => 16, 
			8 => 22, 
			9 => 32, 
			_ => 32, 
		};
	}

	public static int NumDrones(int numMegafarmUpgrades)
	{
		return 1 << numMegafarmUpgrades;
	}

	public static int Squared(this int value)
	{
		return value * value;
	}

	public static int JustSha256It(System.Random random)
	{
		using SHA256 sHA = SHA256.Create();
		byte[] buffer = new byte[16];
		random.NextBytes(buffer);
		return BitConverter.ToInt32(sHA.ComputeHash(buffer), 0) & 0x7FFFFFFF;
	}

	public static Color32 MultiplyAlpha(this Color32 color, float alpha)
	{
		float value = (float)(int)color.a / 255f * alpha;
		color.a = (byte)Mathf.Round(Mathf.Clamp01(value) * 255f);
		return color;
	}
}
