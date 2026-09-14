using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public static class CodeUtilities
{
	private class ReferenceComparer : IEqualityComparer<object>
	{
		public new bool Equals(object x, object y)
		{
			return x == y;
		}

		public int GetHashCode(object obj)
		{
			return RuntimeHelpers.GetHashCode(obj);
		}
	}

	public static string ApplyCodeTags(string code, ColorTheme colorTheme = null)
	{
		if ((object)colorTheme == null)
		{
			colorTheme = ThemeManager.Inst.Theme;
		}
		string[] array = code.Split(new string[4] { "\r\n```", "\n```", "```", "`" }, StringSplitOptions.None);
		for (int i = 1; i < array.Length; i += 2)
		{
			array[i] = "<font=\"FiraCode-Regular SDF\">" + SyntaxColor2(array[i], colorTheme) + "</font>";
		}
		return string.Join("", array);
	}

	public static string RemoveSyntaxTags(string code)
	{
		return Regex.Replace(code, "<((font|color)=.+?|/(font|color))>", "");
	}

	public static string SyntaxColor2(string code, ColorTheme colorTheme, string searchWord = "", int searchIndex = -1)
	{
		string text = "(?<comment>#.*)|(?<string>(['\"])(.*?)\\1)|(?<keyword>\\b(?:in|for|while|def|if|else|elif|return|pass|break|continue|and|or|not|global|import|from)\\b)|(?<function>\\b[a-zA-Z]\\w*(?=\\())|(?<builtin>\\b(?:True|False|None|North|East|South|West|Entities|Items|Grounds|Unlocks|Leaderboards|Hats)\\b|(?<=[a-zA-Z]\\.)\\w*)|(?<number>\\b(?:\\d*\\.)?\\d+\\b)";
		string pattern = (string.IsNullOrEmpty(searchWord) ? text : ("(?<search>(?i:" + Regex.Escape(searchWord) + ")(?-i))|" + text));
		return Regex.Replace(code, pattern, delegate(Match m)
		{
			if (!string.IsNullOrEmpty(searchWord) && m.Groups["search"].Success)
			{
				int index = m.Index;
				if (searchIndex >= 0 && index == searchIndex)
				{
					return "<mark=" + colorTheme.ui.search_match_current + " from_search>" + m.Value + "</mark>";
				}
				return "<mark=" + colorTheme.ui.search_match + " from_search>" + m.Value + "</mark>";
			}
			string text2 = m.Value;
			if (!string.IsNullOrEmpty(searchWord))
			{
				text2 = Regex.Replace(text2, Regex.Escape(searchWord), delegate(Match match)
				{
					int num = m.Index + match.Index;
					return (searchIndex >= 0 && num == searchIndex) ? ("<mark=" + colorTheme.ui.search_match_current + " from_search>" + match.Value + "</mark>") : ("<mark=" + colorTheme.ui.search_match + " from_search>" + match.Value + "</mark>");
				}, RegexOptions.IgnoreCase);
			}
			if (m.Groups["comment"].Success)
			{
				return "<color=" + colorTheme.code.comment + ">" + text2 + "</color>";
			}
			if (m.Groups["string"].Success)
			{
				return "<color=" + colorTheme.code.@string + ">" + text2 + "</color>";
			}
			if (m.Groups["keyword"].Success)
			{
				return "<color=" + colorTheme.code.keyword + ">" + text2 + "</color>";
			}
			if (m.Groups["function"].Success)
			{
				return "<color=" + colorTheme.code.function + ">" + text2 + "</color>";
			}
			if (m.Groups["builtin"].Success)
			{
				return "<color=" + colorTheme.code.builtin + ">" + text2 + "</color>";
			}
			return m.Groups["number"].Success ? ("<color=" + colorTheme.code.number + ">" + text2 + "</color>") : text2;
		});
	}

	public static string RemoveMarks(string code)
	{
		return Regex.Replace(code, "<mark=#\\w{8} from_search>(.*?)</mark>", "$1");
	}

	public static string MarkSearch(string text, string searchWord, int occuranceIndex, ref int count)
	{
		if (string.IsNullOrEmpty(searchWord))
		{
			return text;
		}
		Regex regex = new Regex(Regex.Escape(searchWord), RegexOptions.IgnoreCase);
		Regex regex2 = new Regex("</?(?:b|i|u|s|sub|sup|size|color|align|indent|line-height|margin|link|mark|noparse|quad|sprite|font)(?:=[^>]+)?>");
		StringBuilder stringBuilder = new StringBuilder();
		int localCount = count;
		int num = 0;
		ColorTheme theme = ThemeManager.Inst.Theme;
		foreach (Match item in regex2.Matches(text))
		{
			if (item.Index > num)
			{
				string input = text.Substring(num, item.Index - num);
				string value = regex.Replace(input, delegate(Match m)
				{
					string result = ((localCount == occuranceIndex) ? ("<mark=" + theme.ui.search_match_current + " from_search>" + m.Value + "</mark>") : ("<mark=" + theme.ui.search_match + " from_search>" + m.Value + "</mark>"));
					localCount++;
					return result;
				});
				stringBuilder.Append(value);
			}
			stringBuilder.Append(item.Value);
			num = item.Index + item.Length;
		}
		if (num < text.Length)
		{
			string input2 = text.Substring(num);
			string value2 = regex.Replace(input2, delegate(Match m)
			{
				string result2 = ((localCount == occuranceIndex) ? ("<mark=" + theme.ui.search_match_current + " from_search>" + m.Value + "</mark>") : ("<mark=" + theme.ui.search_match + " from_search>" + m.Value + "</mark>"));
				localCount++;
				return result2;
			});
			stringBuilder.Append(value2);
		}
		count = localCount;
		return stringBuilder.ToString();
	}

	public static string ToUpperSnake(string a)
	{
		if (string.IsNullOrEmpty(a))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder(a);
		stringBuilder[0] = char.ToUpper(stringBuilder[0]);
		for (int i = 1; i < stringBuilder.Length; i++)
		{
			if (stringBuilder[i - 1] == '_')
			{
				stringBuilder[i] = char.ToUpper(stringBuilder[i]);
			}
		}
		return stringBuilder.ToString();
	}

	public static string LocalizeAndFormat(string s, params object[] args)
	{
		try
		{
			string format = Localizer.Localize(s);
			object[] args2 = Enumerable.ToArray<string>(Enumerable.Select<object, string>((IEnumerable<object>)args, (Func<object, string>)((object o) => ApplyCodeTags("`" + ToNiceString(o) + "`"))));
			return string.Format(format, args2);
		}
		catch (Exception ex)
		{
			Debug.LogError("Error localizing " + s + ": " + ex.ToString());
			return s;
		}
	}

	public static string ToNiceString(object obj, int depth = 0, HashSet<object> loopDetector = null, bool isSequenceElement = false)
	{
		if (depth > 100)
		{
			return "";
		}
		if (obj == null || obj is PyNone)
		{
			return "None";
		}
		if (loopDetector != null && loopDetector.Contains(obj))
		{
			return "...";
		}
		if (obj is PyList)
		{
			if (loopDetector == null)
			{
				loopDetector = new HashSet<object>((IEqualityComparer<object>)new ReferenceComparer());
			}
			loopDetector.Add(obj);
			StringBuilder stringBuilder = new StringBuilder("[");
			foreach (IPyObject item in (PyList)obj)
			{
				stringBuilder.Append(ToNiceString(item, depth + 1, loopDetector, isSequenceElement: true));
				stringBuilder.Append(',');
			}
			if (stringBuilder.Length > 1)
			{
				stringBuilder[stringBuilder.Length - 1] = ']';
			}
			else
			{
				stringBuilder.Append(']');
			}
			return stringBuilder.ToString();
		}
		if (obj is PyDict)
		{
			if (loopDetector == null)
			{
				loopDetector = new HashSet<object>((IEqualityComparer<object>)new ReferenceComparer());
			}
			loopDetector.Add(obj);
			StringBuilder stringBuilder2 = new StringBuilder("{");
			foreach (KeyValuePair<IPyObject, PyObjectBox> item2 in ((PyDict)obj).dict)
			{
				stringBuilder2.Append(ToNiceString(item2.Key, depth + 1, loopDetector, isSequenceElement: true));
				stringBuilder2.Append(":");
				stringBuilder2.Append(ToNiceString(item2.Value.obj, depth + 1, loopDetector, isSequenceElement: true));
				stringBuilder2.Append(',');
			}
			if (stringBuilder2.Length > 1)
			{
				stringBuilder2[stringBuilder2.Length - 1] = '}';
			}
			else
			{
				stringBuilder2.Append('}');
			}
			return stringBuilder2.ToString();
		}
		if (obj is PySet)
		{
			StringBuilder stringBuilder3 = new StringBuilder("{");
			foreach (IPyObject item3 in (PySet)obj)
			{
				stringBuilder3.Append(ToNiceString(item3, depth + 1, loopDetector, isSequenceElement: true));
				stringBuilder3.Append(',');
			}
			if (stringBuilder3.Length > 1)
			{
				stringBuilder3[stringBuilder3.Length - 1] = '}';
			}
			else
			{
				stringBuilder3.Append('}');
			}
			return stringBuilder3.ToString();
		}
		if (obj is PyTuple)
		{
			StringBuilder stringBuilder4 = new StringBuilder("(");
			foreach (IPyObject item4 in (PyTuple)obj)
			{
				stringBuilder4.Append(ToNiceString(item4, depth + 1, loopDetector, isSequenceElement: true));
				stringBuilder4.Append(',');
			}
			if (stringBuilder4.Length > 1)
			{
				stringBuilder4[stringBuilder4.Length - 1] = ')';
			}
			else
			{
				stringBuilder4.Append(')');
			}
			return stringBuilder4.ToString();
		}
		if (obj is PyNumber pyNumber && !(pyNumber is PyBool))
		{
			return ((PyNumber)obj).num.ToString("0.##", CultureInfo.InvariantCulture);
		}
		if (obj is PyString && isSequenceElement)
		{
			return $"'{obj}'";
		}
		return Convert.ToString(obj, CultureInfo.InvariantCulture);
	}

	public static int DeepCompare(IPyObject obj1, IPyObject obj2, string op, ref int steps)
	{
		if (obj1 is PyNone && obj2 is PyNone)
		{
			return 0;
		}
		if (steps > 1000)
		{
			throw new ExecuteException("error_max_comparison_depth");
		}
		steps++;
		if (obj1 is PyNone || obj2 is PyNone)
		{
			throw new ExecuteException(LocalizeAndFormat("error_bad_bin_operator", op, obj1, obj2));
		}
		if (obj1 is PyNumber && obj2 is PyNumber)
		{
			double num = ((PyNumber)obj1).num;
			double num2 = ((PyNumber)obj2).num;
			if (num > num2)
			{
				return 1;
			}
			if (num < num2)
			{
				return -1;
			}
			return 0;
		}
		if (obj1 is PyString && obj2 is PyString)
		{
			return string.Compare(((PyString)obj1).str, ((PyString)obj2).str);
		}
		if (obj1 is IPySequence && obj2 is IPySequence)
		{
			IPySequence pySequence = (IPySequence)obj1;
			IPySequence pySequence2 = (IPySequence)obj2;
			for (int i = 0; i < pySequence.Count && i < pySequence2.Count; i++)
			{
				if (!DeepEquals(pySequence[i], pySequence2[i], ref steps))
				{
					return DeepCompare(pySequence[i], pySequence2[i], op, ref steps);
				}
			}
			if (pySequence.Count > pySequence2.Count)
			{
				return 1;
			}
			if (pySequence2.Count > pySequence.Count)
			{
				return -1;
			}
			return 0;
		}
		if (obj1.GetType() != obj2.GetType() || !(obj1 is IComparable))
		{
			throw new ExecuteException(LocalizeAndFormat("error_bad_bin_operator", op, obj1, obj2));
		}
		return ((IComparable)obj1).CompareTo(obj2);
	}

	public static bool DeepEquals(IPyObject obj1, IPyObject obj2, ref int steps)
	{
		if (steps > 1000)
		{
			throw new ExecuteException("error_max_comparison_depth");
		}
		steps++;
		if (obj1 is PyNone && obj2 is PyNone)
		{
			return true;
		}
		if (obj1 is PyNumber && obj2 is PyNumber)
		{
			return ((PyNumber)obj1).num == ((PyNumber)obj2).num;
		}
		if (obj1 is PyString || obj1 is PyRange)
		{
			return obj1.Equals(obj2);
		}
		if (obj1 is IPySequence && obj2 is IPySequence)
		{
			IPySequence pySequence = (IPySequence)obj1;
			IPySequence pySequence2 = (IPySequence)obj2;
			if (pySequence.Count != pySequence2.Count)
			{
				return false;
			}
			for (int i = 0; i < pySequence.Count; i++)
			{
				if (!DeepEquals(pySequence[i], pySequence2[i], ref steps))
				{
					return false;
				}
			}
			return true;
		}
		if (obj1 is PyDict && obj2 is PyDict)
		{
			PyDict pyDict = (PyDict)obj1;
			PyDict pyDict2 = (PyDict)obj2;
			if (pyDict.dict.Count != pyDict2.dict.Count)
			{
				return false;
			}
			foreach (IPyObject item in pyDict)
			{
				if (!pyDict2.dict.ContainsKey(item))
				{
					return false;
				}
				if (!DeepEquals(pyDict.dict[item].obj, pyDict2.dict[item].obj, ref steps))
				{
					return false;
				}
			}
			return true;
		}
		if (obj1 is PySet && obj2 is PySet)
		{
			PySet pySet = (PySet)obj1;
			PySet pySet2 = (PySet)obj2;
			if (pySet.set.Count != pySet2.set.Count)
			{
				return false;
			}
			foreach (IPyObject item2 in pySet)
			{
				if (!Enumerable.Contains<object>((IEnumerable<object>)pySet2.set, (object)item2))
				{
					return false;
				}
			}
			return true;
		}
		return obj1.Equals(obj2);
	}
}
