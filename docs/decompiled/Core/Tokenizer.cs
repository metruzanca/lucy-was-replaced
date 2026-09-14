using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class Tokenizer
{
	private static readonly (string Text, bool needsWordBoundary, TokenType Type)[] constantTokens = new(string, bool, TokenType)[45]
	{
		("if", true, TokenType.IF),
		("else", true, TokenType.ELSE),
		("for", true, TokenType.FOR),
		("or", true, TokenType.OR),
		("and", true, TokenType.AND),
		("return", true, TokenType.RETURN),
		("def", true, TokenType.DEF),
		("while", true, TokenType.WHILE),
		("elif", true, TokenType.ELIF),
		("break", true, TokenType.BREAK),
		("continue", true, TokenType.CONTINUE),
		("pass", true, TokenType.PASS),
		("**", false, TokenType.EXP),
		("==", false, TokenType.COMPARE),
		("=", false, TokenType.ASSIGN),
		("!=", false, TokenType.COMPARE),
		("<=", false, TokenType.COMPARE),
		(">=", false, TokenType.COMPARE),
		("<", false, TokenType.COMPARE),
		(">", false, TokenType.COMPARE),
		("(", false, TokenType.BRACKET_OPEN),
		(")", false, TokenType.BRACKET_CLOSE),
		("+=", false, TokenType.ASSIGN),
		("+", false, TokenType.ADD),
		("-=", false, TokenType.ASSIGN),
		("->", false, TokenType.ARROW),
		("-", false, TokenType.ADD),
		("*=", false, TokenType.ASSIGN),
		("//=", false, TokenType.ASSIGN),
		("//", false, TokenType.MULT),
		("/=", false, TokenType.ASSIGN),
		("%=", false, TokenType.ASSIGN),
		("*", false, TokenType.MULT),
		("/", false, TokenType.MULT),
		("%", false, TokenType.MULT),
		("[", false, TokenType.SQUARE_BRACKET_OPEN),
		("]", false, TokenType.SQUARE_BRACKET_CLOSE),
		("{", false, TokenType.CURL_BRACE_OPEN),
		("}", false, TokenType.CURL_BRACE_CLOSE),
		(",", false, TokenType.COMMA),
		(":", false, TokenType.COLON),
		("|", false, TokenType.UNION),
		("global", true, TokenType.GLOBAL),
		("import", true, TokenType.IMPORT),
		("from", true, TokenType.FROM)
	};

	private static readonly List<(Regex, TokenType)> regexTokens = new List<(Regex, TokenType)>
	{
		(new Regex("\\G(\\d*\\.)?\\d+\\b"), TokenType.NUM),
		(new Regex("\\G(?:in|not\\s+in)\\b"), TokenType.IN),
		(new Regex("\\Gnot\\b"), TokenType.NOT),
		(new Regex("\\G[a-zA-Z_]\\w*"), TokenType.IDENTIFIER),
		(new Regex("\\G(['\"])(.*?)\\1"), TokenType.STRING),
		(new Regex("\\G\\n( |\\t)*"), TokenType.NEW_LINE),
		(new Regex("\\G[\\s-[\\n]]+"), TokenType.IGNORE),
		(new Regex("\\G#.*"), TokenType.IGNORE),
		(new Regex("\\G\\."), TokenType.DOT),
		(new Regex("\\G\\S+"), TokenType.UNKNOWN)
	};

	public static bool Tokenize(string code, out TokenStream stream)
	{
		stream = new TokenStream();
		stream.Add(new Token(TokenType.NEW_LINE, "\n", 0));
		bool flag = false;
		string text = code.Replace("\v", "\n");
		int num = 0;
		while (num < text.Length)
		{
			bool flag2 = false;
			(string, bool, TokenType)[] array = constantTokens;
			for (int i = 0; i < array.Length; i++)
			{
				var (text2, flag3, type) = array[i];
				if (num + text2.Length > text.Length || !MemoryExtensions.AsSpan(text, num, text2.Length).SequenceEqual(text2))
				{
					continue;
				}
				if (flag3 && num + text2.Length < text.Length)
				{
					char c = text[num + text2.Length];
					if (char.IsLetterOrDigit(c) || c == '_')
					{
						continue;
					}
				}
				stream.Add(new Token(type, text2, num));
				num += text2.Length;
				flag2 = true;
				break;
			}
			if (flag2)
			{
				continue;
			}
			foreach (var regexToken in regexTokens)
			{
				Regex item = regexToken.Item1;
				TokenType item2 = regexToken.Item2;
				Match match = item.Match(text, num);
				if (match.Success)
				{
					if (item2 != TokenType.IGNORE)
					{
						stream.Add(new Token(item2, match.Value, num));
					}
					flag = flag || item2 == TokenType.UNKNOWN;
					num += match.Length;
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				throw new Exception("nothing matched, not even UNKNOWN. this should never happen");
			}
		}
		while (true)
		{
			Token last = stream.Last;
			if (last == null || last.type != TokenType.NEW_LINE)
			{
				break;
			}
			stream.RemoveLast();
		}
		return flag;
	}
}
