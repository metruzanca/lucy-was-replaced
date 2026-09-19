using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GleamRuntime
{
    /// <summary>
    /// Generates in-game docs for the curated Gleam stdlib modules from their embedded
    /// `.gleam` sources (the `///`/`////` doc comments). Returns markdown with
    /// `## &lt;module&gt;` and `## &lt;module&gt;.&lt;function&gt;` sections, which is
    /// concatenated with the committed references and fed to <see cref="GleamDocs.Parse"/>.
    /// Undocumented `pub fn`s are omitted (their docs weren't written).
    /// </summary>
    public static class GleamStdlibDocs
    {
        /// <summary>Curated module short names shown in-game (excludes low-value modules).</summary>
        public static readonly string[] CuratedModules =
        {
            "bool", "dict", "float", "function", "int", "list",
            "option", "order", "pair", "result", "set", "string",
        };

        public static string Generate(IEnumerable<(string Name, string Code)> modules)
        {
            var byShortName = modules
                .Where(m => m.Name.StartsWith("gleam/", StringComparison.Ordinal))
                .ToDictionary(
                    m => m.Name.Substring("gleam/".Length),
                    m => m.Code,
                    StringComparer.Ordinal);

            var sb = new StringBuilder();
            foreach (var module in CuratedModules)
            {
                if (byShortName.TryGetValue(module, out var code))
                    WriteModule(sb, module, code);
            }
            return sb.ToString();
        }

        private static void WriteModule(StringBuilder sb, string module, string code)
        {
            var lines = code.Replace("\r\n", "\n").Split('\n');
            var i = 0;

            // Leading //// module doc.
            var moduleDoc = new List<string>();
            while (i < lines.Length)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("////", StringComparison.Ordinal))
                    moduleDoc.Add(trimmed.Substring(4).TrimStart());
                else if (string.IsNullOrWhiteSpace(lines[i]))
                    { }
                else
                    break;
                i++;
            }

            // Functions: a `///` block immediately before `pub fn` (attributes may sit
            // between the doc and the function, as with `@external`).
            var functions = new List<(string Name, string Signature, string Doc)>();
            var pendingDoc = new List<string>();
            for (; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.Trim();
                if (trimmed.StartsWith("///", StringComparison.Ordinal))
                {
                    pendingDoc.Add(trimmed.Substring(3).TrimStart());
                    continue;
                }
                if (trimmed.Length == 0 || trimmed.StartsWith("@", StringComparison.Ordinal))
                    continue;
                if (trimmed.StartsWith("pub fn ", StringComparison.Ordinal))
                {
                    if (pendingDoc.Count > 0)
                    {
                        var name = trimmed.Substring("pub fn ".Length).Split('(')[0].Trim();
                        var signature = ReadSignature(lines, ref i);
                        functions.Add((name, signature, string.Join("\n", pendingDoc)));
                    }
                    pendingDoc.Clear();
                    continue;
                }
                pendingDoc.Clear(); // doc belongs to a type/variant, not a function
            }

            // Module overview page.
            sb.Append("## ").Append(module).Append('\n').Append('\n');
            var overview = moduleDoc.Count > 0
                ? string.Join("\n", moduleDoc)
                : $"The `gleam/{module}` module.";
            sb.Append(Demote(overview)).Append('\n').Append('\n');
            sb.Append("### Functions").Append('\n').Append('\n');
            foreach (var (name, _, _) in functions)
            {
                sb.Append("- [`").Append(module).Append('.').Append(name)
                  .Append("`](functions/gleam_").Append(module).Append('_').Append(name).Append(")\n");
            }
            sb.Append('\n');

            // Function pages.
            foreach (var (name, signature, doc) in functions)
            {
                sb.Append("## ").Append(module).Append('.').Append(name).Append('\n').Append('\n');
                sb.Append('`').Append(module).Append('.').Append(signature).Append('`').Append('\n').Append('\n');
                sb.Append(Demote(doc)).Append('\n').Append('\n');
            }
        }

        /// <summary>
        /// Read the full signature line(s) from `pub fn` up to (and including) the
        /// opening `{`, advancing `i` past the consumed lines. Returns the signature
        /// without the `pub fn` prefix or the trailing `{`. Bodyless `@external`
        /// functions have no `{`; the scan stops at a blank line and leaves `i` alone.
        /// </summary>
        private static string ReadSignature(string[] lines, ref int i)
        {
            var raw = new StringBuilder(lines[i].Trim());
            if (!lines[i].Contains('{'))
            {
                var j = i + 1;
                while (j < lines.Length && lines[j].Trim().Length > 0 && !lines[j].Contains('{'))
                {
                    raw.Append(' ').Append(lines[j].Trim());
                    j++;
                }
                if (j < lines.Length && lines[j].Contains('{'))
                {
                    raw.Append(' ').Append(lines[j].Trim());
                    i = j; // consume the `{` line
                }
            }

            var signature = raw.ToString().Trim();
            if (signature.EndsWith("{")) signature = signature.Substring(0, signature.Length - 1).TrimEnd();
            if (signature.StartsWith("pub fn ", StringComparison.Ordinal))
                signature = signature.Substring("pub fn ".Length).Trim();
            return signature;
        }

        /// <summary>Demote nested `## ` headings to `### ` so the top-level section split stays intact.</summary>
        private static string Demote(string text) =>
            string.Join("\n", text.Split('\n').Select(line =>
                line.StartsWith("## ", StringComparison.Ordinal) ? "### " + line.Substring(3) : line));
    }
}