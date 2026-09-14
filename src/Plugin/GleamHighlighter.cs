using System.Text.RegularExpressions;

namespace GleamFarmer
{
    /// <summary>
    /// Gleam-aware syntax highlighter, mirroring the shape of the game's
    /// <c>CodeUtilities.SyntaxColor2</c> (single regex pass + optional search marks).
    /// </summary>
    public static class GleamHighlighter
    {
        private static readonly string Pattern = BuildPattern();

        private static string BuildPattern()
        {
            const string q = "\"";
            const string tq = "\"\"\"";
            var tripleString = tq + "(?:\\\\.|[^\"])*?" + tq;
            var singleString = q + "(?:\\\\.|[^\"\\\\])*" + q;

            return
                "(?<comment>//.*)|" +
                "(?<string>" + tripleString + "|" + singleString + ")|" +
                @"(?<number>\b\d[\d_]*(?:\.[\d_]+)?\b|0x[0-9a-fA-F_]+|0b[01_]+|0o[0-7_]+)|" +
                @"(?<type>\b[A-Z][A-Za-z0-9_]*\b)|" +
                @"(?<keyword>\b(?:pub|fn|let|case|if|else|import|const|type|opaque|use|assert|panic|todo|try|as|echo)\b)|" +
                @"(?<function>\b[a-z_][a-z0-9_]*(?=\())";
        }

        public static string Color(string code, ColorTheme colorTheme, string searchWord = "", int searchIndex = -1)
        {
            var pattern = string.IsNullOrEmpty(searchWord)
                ? Pattern
                : "(?<search>(?i:" + Regex.Escape(searchWord) + ")(?-i))|" + Pattern;

            return Regex.Replace(code, pattern, m =>
            {
                if (!string.IsNullOrEmpty(searchWord) && m.Groups["search"].Success)
                {
                    var color = searchIndex >= 0 && m.Index == searchIndex
                        ? colorTheme.ui.search_match_current
                        : colorTheme.ui.search_match;
                    return "<mark=" + color + " from_search>" + m.Value + "</mark>";
                }
                if (m.Groups["comment"].Success)
                    return "<color=" + colorTheme.code.comment + ">" + m.Value + "</color>";
                if (m.Groups["string"].Success)
                    return "<color=" + colorTheme.code.@string + ">" + m.Value + "</color>";
                if (m.Groups["number"].Success)
                    return "<color=" + colorTheme.code.number + ">" + m.Value + "</color>";
                if (m.Groups["keyword"].Success)
                    return "<color=" + colorTheme.code.keyword + ">" + m.Value + "</color>";
                if (m.Groups["function"].Success)
                    return "<color=" + colorTheme.code.function + ">" + m.Value + "</color>";
                if (m.Groups["type"].Success)
                    return "<color=" + colorTheme.code.builtin + ">" + m.Value + "</color>";
                return m.Value;
            });
        }
    }
}