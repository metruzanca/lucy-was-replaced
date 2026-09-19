using System;
using System.Collections.Generic;
using System.Text;

namespace GleamRuntime
{
    /// <summary>
    /// A statement (or function signature) in a Gleam module, at the granularity the
    /// compiled JS steps on. <see cref="Line"/> is 1-based; <see cref="Depth"/> is the
    /// brace-nesting depth at the start of the statement (0 = module top level, 1 = a
    /// function body, 2 = inside a case block / closure body, …).
    /// </summary>
    public sealed class GleamStatementLine
    {
        public int Line { get; init; }
        public int Depth { get; init; }

        /// <summary>True when this line is a `fn name(…)` / `pub fn name(…)` signature.</summary>
        public bool IsFunctionSignature { get; init; }

        /// <summary>Function name for signature lines (used to match compiled JS functions).</summary>
        public string? FunctionName { get; init; }

        /// <summary>
        /// Anonymous `fn(…) {` closure blocks opened on this line, in source order. The JS
        /// walker consumes one entry per anonymous closure it enters while executing this
        /// statement; a missing entry means the closure is a `use` continuation (its body is
        /// the rest of the enclosing function, at the same depth).
        /// </summary>
        public List<GleamClosure> Closures { get; init; } = new();
    }

    /// <summary>An anonymous closure block opened within a statement line.</summary>
    public sealed class GleamClosure
    {
        /// <summary>Brace depth of the closure's body statements.</summary>
        public int Depth { get; init; }

        /// <summary>True when the closure body fits on the statement's own line (no separate lines).</summary>
        public bool Inline { get; init; }
    }

    /// <summary>
    /// Scans Gleam source and produces the ordered list of statement lines the compiler's
    /// JavaScript output steps on: function signatures, top-level statements, `case` subject
    /// and clause lines, and statements inside blocks. Multi-line statements count once
    /// (their first line); blank, comment-only and brace-only lines are skipped.
    /// </summary>
    public static class GleamStatementScanner
    {
        public static List<GleamStatementLine> Scan(string source)
        {
            var result = new List<GleamStatementLine>();
            var ctx = new ScanContext();
            var lines = source.Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNo = i + 1;
                var tokens = Tokenizer.TokenizeLine(line, ref ctx.StringState);
                if (tokens.Count == 0) continue; // blank / comment-only lines

                var isCloserLine = tokens[0].Kind == TokenKind.Symbol && tokens[0].Text == "}";

                if (!isCloserLine && ctx.TryStartStatement(lineNo, tokens, out var statement))
                    result.Add(statement);

                ctx.Process(tokens);
            }

            return result;
        }

        /// <summary>Per-scan state threaded across lines.</summary>
        private sealed class ScanContext
        {
            public int StringState;

            private int _parenDepth;
            private int _braceDepth;
            private readonly Stack<StatementState> _states = new();

            public ScanContext() => _states.Push(new StatementState { Depth = 0, ParenBase = 0 });

            public bool TryStartStatement(int lineNo, List<Token> tokens, out GleamStatementLine statement)
            {
                statement = null!;
                var top = _states.Peek();

                // Continuation of the open statement at this level (its parens are open, or the
                // previous line ended with an operator / empty clause arrow).
                if (top.Started && (_parenDepth > top.ParenBase || top.TrailingContinuation))
                    return false;

                if (top.Started) return false;

                var first = tokens[0];

                // Not a statement: dangling closers / commas, or pure structure.
                if (first.Kind == TokenKind.Symbol && first.Text is ")" or "]" or "," or "." or "->" or ":" or "|")
                    return false;

                // Declaration lines that don't execute as JS statements.
                if (first.Text == "import") return false;
                if (first.Text == "type") return false;
                if (first.Text == "pub")
                {
                    if (tokens.Count > 1 && tokens[1].Text == "type") return false;
                    if (tokens.Count > 1 && tokens[1].Text == "const") return false;
                }
                if (first.Text == "const") return false;

                // Function signature?  fn name(…)  |  pub fn name(…)
                string? fnName = null;
                if (first.Text == "fn" && tokens.Count > 1 && tokens[1].Kind == TokenKind.Identifier)
                    fnName = tokens[1].Text;
                else if (first.Text == "pub" && tokens.Count > 2
                         && tokens[1].Text == "fn" && tokens[2].Kind == TokenKind.Identifier)
                    fnName = tokens[2].Text;

                statement = new GleamStatementLine
                {
                    Line = lineNo,
                    Depth = top.Depth,
                    IsFunctionSignature = fnName != null,
                    FunctionName = fnName,
                    Closures = CollectClosures(tokens, top.Depth),
                };

                top.Started = true;
                top.ParenBase = _parenDepth;
                return true;
            }

            public void Process(List<Token> tokens)
            {
                // Consume the pending continuation state from the previous line.
                if (_states.Count > 0) _states.Peek().TrailingContinuation = false;

                foreach (var token in tokens)
                {
                    switch (token.Kind)
                    {
                        case TokenKind.Identifier:
                        case TokenKind.Number:
                            break;
                        case TokenKind.Symbol:
                            switch (token.Text)
                            {
                                case "(":
                                case "[":
                                    _parenDepth++;
                                    break;
                                case ")":
                                case "]":
                                    _parenDepth = Math.Max(0, _parenDepth - 1);
                                    break;
                                case "{":
                                    _braceDepth++;
                                    _states.Push(new StatementState
                                    {
                                        Depth = _braceDepth,
                                        ParenBase = _parenDepth,
                                    });
                                    break;
                                case "}":
                                    _braceDepth = Math.Max(0, _braceDepth - 1);
                                    if (_states.Count > 1) _states.Pop();
                                    break;
                            }
                            break;
                    }
                }

                var last = tokens[tokens.Count - 1];
                var trailing = last.Kind == TokenKind.Symbol && IsContinuation(last.Text);

                var top = _states.Peek();
                top.TrailingContinuation = trailing;

                // The statement at this level completes once its parens/brackets are balanced,
                // its blocks are closed, and nothing dangles onto the next line.
                if (top.Started && !trailing && _parenDepth <= top.ParenBase && _braceDepth <= top.Depth)
                    top.Started = false;
            }

            private static bool IsContinuation(string text) => text switch
            {
                "=" or "+" or "-" or "*" or "/" or "%" or ">" or "<" or "!" or "&" or "|" or "," or "."
                    or "->" or "<-" or "<>" or "&&" or "||" or "==" or "!=" or ">=" or "<=" or "=>"
                    or ">." or "<." or "=." or ">=." or "<=." or "==." or "!=." => true,
                _ => false,
            };

            private static List<GleamClosure> CollectClosures(List<Token> tokens, int statementDepth)
            {
                var closures = new List<GleamClosure>();
                var openBraces = 0;
                for (var i = 0; i < tokens.Count; i++)
                {
                    if (tokens[i].Text == "fn" && i + 1 < tokens.Count && tokens[i + 1].Text == "(")
                    {
                        // Find the matching ')' of the fn's parameter list.
                        var j = i + 1;
                        var depth = 0;
                        while (j < tokens.Count)
                        {
                            if (tokens[j].Text == "(") depth++;
                            else if (tokens[j].Text == ")")
                            {
                                depth--;
                                if (depth == 0) break;
                            }
                            j++;
                        }
                        if (j < tokens.Count && j + 1 < tokens.Count && tokens[j + 1].Text == "{")
                        {
                            var openIdx = j + 1;
                            var bodyDepth = statementDepth + openBraces + 1;
                            var inline = MatchingBraceOnSameLine(tokens, openIdx);
                            closures.Add(new GleamClosure { Depth = bodyDepth, Inline = inline });
                        }
                        continue;
                    }

                    if (tokens[i].Text == "{") openBraces++;
                    else if (tokens[i].Text == "}")
                        openBraces = Math.Max(0, openBraces - 1);
                }
                return closures;
            }

            private static bool MatchingBraceOnSameLine(List<Token> tokens, int openIdx)
            {
                var depth = 0;
                for (var i = openIdx; i < tokens.Count; i++)
                {
                    if (tokens[i].Text == "{") depth++;
                    else if (tokens[i].Text == "}")
                    {
                        depth--;
                        if (depth == 0) return true;
                    }
                }
                return false;
            }
        }

        private enum TokenKind { Identifier, Number, Symbol }

        private readonly struct Token
        {
            public Token(TokenKind kind, string text) { Kind = kind; Text = text; }
            public TokenKind Kind { get; }
            public string Text { get; }
        }

        /// <summary>
        /// A minimal Gleam lexer: identifiers, numbers, multi-char operators, strings
        /// (including triple-quoted) and line comments. Braces/parens inside strings or
        /// comments are ignored, so the depth scan stays correct.
        /// </summary>
        private static class Tokenizer
        {
            public static List<Token> TokenizeLine(string line, ref int stringState)
            {
                var tokens = new List<Token>();
                var i = 0;

                while (i < line.Length)
                {
                    var c = line[i];

                    if (stringState != 0)
                    {
                        if (stringState == 1)
                        {
                            if (c == '\\') { i += 2; continue; }
                            if (c == '"') stringState = 0;
                            i++;
                            continue;
                        }
                        // triple-quoted string body
                        if (c == '"' && i + 2 < line.Length && line[i + 1] == '"' && line[i + 2] == '"')
                        {
                            stringState = 0;
                            i += 3;
                            continue;
                        }
                        i++;
                        continue;
                    }

                    if (c == '"')
                    {
                        if (i + 2 < line.Length && line[i + 1] == '"' && line[i + 2] == '"')
                        {
                            stringState = 2;
                            i += 3;
                        }
                        else
                        {
                            stringState = 1;
                            i++;
                        }
                        continue;
                    }

                    if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
                        break; // line comment

                    if (char.IsWhiteSpace(c)) { i++; continue; }

                    if (char.IsDigit(c))
                    {
                        var start = i;
                        while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_' || line[i] == '.'))
                            i++;
                        tokens.Add(new Token(TokenKind.Number, line.Substring(start, i - start)));
                        continue;
                    }

                    if (char.IsLetter(c) || c == '_')
                    {
                        var start = i;
                        while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_'))
                            i++;
                        tokens.Add(new Token(TokenKind.Identifier, line.Substring(start, i - start)));
                        continue;
                    }

                    // Multi-char operators first.
                    if (i + 2 < line.Length && line.Substring(i, 3) is ">=." or "<=." or "==." or "!=.")
                    {
                        tokens.Add(new Token(TokenKind.Symbol, line.Substring(i, 3)));
                        i += 3;
                        continue;
                    }
                    if (i + 1 < line.Length && line.Substring(i, 2) is "->" or "<-" or "<>" or "&&" or "||"
                        or "==" or "!=" or ">=" or "<=" or "=>" or ">." or "<." or "=.")
                    {
                        tokens.Add(new Token(TokenKind.Symbol, line.Substring(i, 2)));
                        i += 2;
                        continue;
                    }

                    tokens.Add(new Token(TokenKind.Symbol, c.ToString()));
                    i++;
                }

                return tokens;
            }
        }

        private sealed class StatementState
        {
            public int Depth;
            public int ParenBase;
            public bool Started;
            public bool TrailingContinuation;
        }
    }
}