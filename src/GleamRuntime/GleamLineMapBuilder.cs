using System;
using System.Collections.Generic;
using Acornima;
using Acornima.Ast;

namespace GleamRuntime
{
    /// <summary>
    /// Builds a <see cref="GleamLineMap"/> for one compiled module by walking the compiled
    /// JavaScript with Acornima and zipping its step nodes (statements/declarations, in
    /// execution order) against the Gleam statement lines from
    /// <see cref="GleamStatementScanner"/>.
    ///
    /// The Gleam JS backend emits statements in source order (one per Gleam statement),
    /// surrounded by recognisable compiler glue: synthetic temporaries (`$`, `$1`,
    /// `_pipe`, `_block`, `loop$`), list/record destructuring (`let x = xs.head`), case
    /// subject hoists (`let $ = subject; if ($ …)`) and assert operands. Glue steps stay on
    /// the current line; real steps advance to the next scanner line. `case` clauses
    /// compile to `if`/`while` chains, which are mapped back to their case/clause lines.
    /// </summary>
    public static class GleamLineMapBuilder
    {
        public static GleamLineMap Build(string jsSource, List<GleamStatementLine> gleamLines)
        {
            var map = new Dictionary<(int, int), int>();
            if (gleamLines.Count == 0) return new GleamLineMap(map, 0);

            var functions = SplitByFunction(gleamLines);
            var functionsByName = new Dictionary<string, List<GleamStatementLine>>();
            foreach (var function in functions)
                if (function[0].FunctionName != null)
                    functionsByName[function[0].FunctionName!] = function;

            var module = new Parser(new ParserOptions { EcmaVersion = EcmaVersion.Latest })
                .ParseModule(jsSource);

            var walker = new Walker(map, functionsByName, gleamLines.Count > 0 ? gleamLines[0].Line : 0);
            walker.WalkModule(module);
            return new GleamLineMap(map, gleamLines.Count > 0 ? gleamLines[0].Line : 0);
        }

        /// <summary>Split the flat scanner output into per-function lists (sig line first).</summary>
        private static List<List<GleamStatementLine>> SplitByFunction(List<GleamStatementLine> lines)
        {
            var result = new List<List<GleamStatementLine>>();
            List<GleamStatementLine>? current = null;
            foreach (var line in lines)
            {
                if (line.IsFunctionSignature)
                {
                    if (current != null) result.Add(current);
                    current = new List<GleamStatementLine> { line };
                }
                else if (current == null)
                {
                    // Module-level statement before any function (rare) — own group.
                    current = new List<GleamStatementLine> { line };
                }
                else
                {
                    current.Add(line);
                }
            }
            if (current != null) result.Add(current);
            return result;
        }

        private enum Mode { Normal, Stay }

        /// <summary>
        /// Consumes one closure entry per anonymous closure entered while executing a
        /// statement (the entries come from that statement's scanner line).
        /// </summary>
        private sealed class StatementFrame
        {
            private readonly List<GleamClosure> _closures;
            private int _index;

            public StatementFrame(List<GleamClosure> closures) => _closures = closures;

            public GleamClosure? Next()
                => _index < _closures.Count ? _closures[_index++] : null;
        }

        private sealed class Walker
        {
            private readonly Dictionary<(int, int), int> _map;
            private readonly Dictionary<string, List<GleamStatementLine>> _functionsByName;
            private readonly int _moduleDefaultLine;

            private List<GleamStatementLine> _lines = new();
            private int _cursor;
            private int _currentLine;
            private GleamStatementLine? _lastEntry;

            public Walker(
                Dictionary<(int, int), int> map,
                Dictionary<string, List<GleamStatementLine>> functionsByName,
                int moduleDefaultLine)
            {
                _map = map;
                _functionsByName = functionsByName;
                _moduleDefaultLine = moduleDefaultLine;
                _currentLine = moduleDefaultLine;
            }

            private void Record(Node node)
            {
                var start = node.Location.Start;
                _map[(start.Line, start.Column)] = _currentLine;
            }

            /// <summary>Consume the next scanner line; returns its entry (for closures).</summary>
            private GleamStatementLine? Advance()
            {
                if (_cursor >= _lines.Count) return null;
                var entry = _lines[_cursor];
                _cursor++;
                _currentLine = entry.Line;
                _lastEntry = entry;
                return entry;
            }

            /// <summary>The entry consumed by the most recent <see cref="Advance"/> (for frames).</summary>
            private GleamStatementLine? LastEntry() => _lastEntry;

            // ---- module ----

            public void WalkModule(Program module)
            {
                foreach (var statement in module.Body)
                {
                    switch (statement)
                    {
                        case FunctionDeclaration fn:
                            WalkFunction(fn);
                            break;
                        case ExportNamedDeclaration { Declaration: FunctionDeclaration exportedFn }:
                            WalkFunction(exportedFn);
                            break;
                        case BlockStatement block:
                            WalkStatements(block.Body, Mode.Normal);
                            break;
                        default:
                            // Imports, class/type declarations, module constants, accessors: no line.
                            Record(statement);
                            WalkChildren(statement, null, Mode.Stay);
                            break;
                    }
                }
            }

            private void WalkFunction(FunctionDeclaration fn)
            {
                var name = fn.Id?.Name ?? string.Empty;
                if (_functionsByName.TryGetValue(name, out var lines))
                {
                    _lines = lines;
                    _cursor = 0;
                    Advance(); // consume the signature line
                    Record(fn);
                    Record(fn.Body);
                    WalkStatements(fn.Body.Body, Mode.Normal);
                }
                else
                {
                    WalkStatements(fn.Body.Body, Mode.Stay);
                }
            }

            // ---- statement sequences ----

            private void WalkStatements(NodeList<Statement> statements, Mode mode)
            {
                for (var i = 0; i < statements.Count; i++)
                {
                    var statement = statements[i];
                    ProcessStatement(statements, i, mode);
                }
            }

            private void ProcessStatement(NodeList<Statement> statements, int index, Mode mode)
            {
                var statement = statements[index];

                if (mode == Mode.Stay)
                {
                    Record(statement);
                    WalkChildren(statement, null, Mode.Stay);
                    return;
                }

                switch (statement)
                {
                    case VariableDeclaration declaration:
                        ProcessVariableDeclaration(statements, index, declaration);
                        break;
                    case ExpressionStatement expression:
                        if (IsGlueExpression(expression))
                        {
                            Record(expression);
                            WalkChildren(expression, null, Mode.Normal);
                        }
                        else
                        {
                            Advance();
                            Record(expression);
                            WalkExpression(expression.Expression, NewFrame(LastEntry()));
                        }
                        break;
                    case IfStatement ifStatement:
                        ProcessIf(ifStatement, firstOfChain: true, insideCaseWhile: false, mode);
                        break;
                    case WhileStatement whileStatement:
                        Advance();
                        Record(whileStatement);
                        WalkWhileBody(whileStatement.Body);
                        break;
                    case ReturnStatement:
                    case ThrowStatement:
                        Advance();
                        Record(statement);
                        WalkChildren(statement, NewFrame(LastEntry()), Mode.Normal);
                        break;
                    case BlockStatement block:
                        Record(block);
                        WalkStatements(block.Body, Mode.Normal);
                        break;
                    default:
                        Advance();
                        Record(statement);
                        WalkChildren(statement, NewFrame(LastEntry()), Mode.Normal);
                        break;
                }
            }

            private void ProcessVariableDeclaration(NodeList<Statement> statements, int index, VariableDeclaration declaration)
            {
                if (IsGlueDeclaration(declaration, statements, index))
                {
                    Record(declaration);
                    WalkChildren(declaration, null, Mode.Normal);
                    return;
                }

                Advance();
                Record(declaration);
                var frame = NewFrame(LastEntry());
                foreach (var declarator in declaration.Declarations)
                    WalkExpression(declarator.Init, frame);
            }

            private void ProcessIf(IfStatement node, bool firstOfChain, bool insideCaseWhile, Mode mode)
            {
                // In Stay mode this if is part of a clause body already mapped to its line.
                if (mode == Mode.Stay)
                {
                    Record(node);
                    WalkChildren(node, null, Mode.Stay);
                    return;
                }

                Advance(); // case line (first of chain) or clause line
                Record(node);

                var consequentAdvances = firstOfChain && !insideCaseWhile;
                WalkStatement(node.Consequent, consequentAdvances ? Mode.Normal : Mode.Stay);

                if (node.Alternate is IfStatement elseIf)
                {
                    ProcessIf(elseIf, firstOfChain: false, insideCaseWhile, Mode.Normal);
                }
                else if (node.Alternate is BlockStatement elseBlock)
                {
                    Advance(); // final clause line
                    Record(node.Alternate);
                    WalkStatements(elseBlock.Body, Mode.Stay);
                }
                else if (node.Alternate != null)
                {
                    Advance();
                    Record(node.Alternate);
                    WalkStatement(node.Alternate, Mode.Stay);
                }
            }

            private void WalkStatement(Statement statement, Mode mode)
            {
                if (statement is BlockStatement block)
                {
                    Record(block);
                    WalkStatements(block.Body, mode);
                }
                else if (mode == Mode.Stay)
                {
                    Record(statement);
                    WalkChildren(statement, null, Mode.Stay);
                }
                else
                {
                    // Single-statement blocks (rare) — treat as one boundary.
                    Advance();
                    Record(statement);
                    WalkChildren(statement, NewFrame(LastEntry()), Mode.Normal);
                }
            }

            private void WalkWhileBody(Statement body)
            {
                if (body is BlockStatement block)
                {
                    Record(block);
                    for (var i = 0; i < block.Body.Count; i++)
                    {
                        var statement = block.Body[i];
                        switch (statement)
                        {
                            case IfStatement ifStatement:
                                ProcessIf(ifStatement, firstOfChain: true, insideCaseWhile: true, Mode.Normal);
                                break;
                            case VariableDeclaration declaration:
                                ProcessVariableDeclaration(block.Body, i, declaration);
                                break;
                            default:
                                if (IsGlueExpression(statement))
                                {
                                    Record(statement);
                                    WalkChildren(statement, null, Mode.Normal);
                                }
                                else
                                {
                                    Advance();
                                    Record(statement);
                                    WalkChildren(statement, NewFrame(LastEntry()), Mode.Normal);
                                }
                                break;
                        }
                    }
                }
            }

            // ---- expressions ----

            private void WalkExpression(Expression? expression, StatementFrame? frame)
            {
                if (expression == null) return;
                if (expression is ArrowFunctionExpression or FunctionExpression)
                {
                    ProcessClosure(expression, frame, Mode.Normal);
                    return;
                }
                WalkChildren(expression, frame, Mode.Normal);
            }

            private void ProcessClosure(Node closure, StatementFrame? frame, Mode mode)
            {
                Record(closure);
                var entry = frame?.Next();
                var bodyMode = mode == Mode.Stay || entry is { Inline: true } ? Mode.Stay : Mode.Normal;

                if (closure is ArrowFunctionExpression arrow)
                {
                    if (arrow.Body is BlockStatement block)
                    {
                        Record(block);
                        WalkStatements(block.Body, bodyMode);
                    }
                    else
                    {
                        WalkExpression(arrow.Body as Expression, null);
                    }
                }
                else if (closure is FunctionExpression function)
                {
                    Record(function.Body);
                    WalkStatements(function.Body.Body, bodyMode);
                }
            }

            // ---- generic child walk (for closures nested inside expressions) ----

            private void WalkChildren(Node node, StatementFrame? frame, Mode mode)
            {
                foreach (var child in node.ChildNodes)
                {
                    if (child == null) continue;
                    switch (child)
                    {
                        case ArrowFunctionExpression or FunctionExpression:
                            ProcessClosure(child, frame, mode);
                            break;
                        case VariableDeclaration:
                        case ExpressionStatement:
                        case IfStatement:
                        case WhileStatement:
                        case ReturnStatement:
                        case ThrowStatement:
                        case BlockStatement:
                            // Nested statements (e.g. a block inside an expression) — walk as statements.
                            WalkChildStatement(child, mode);
                            break;
                        default:
                            WalkChildren(child, frame, mode);
                            break;
                    }
                }
            }

            private void WalkChildStatement(Node child, Mode mode)
            {
                switch (child)
                {
                    case BlockStatement block:
                        Record(block);
                        WalkStatements(block.Body, mode);
                        break;
                    default:
                        if (mode == Mode.Stay)
                        {
                            Record(child);
                            WalkChildren(child, null, Mode.Stay);
                        }
                        else
                        {
                            Advance();
                            Record(child);
                            WalkChildren(child, NewFrame(LastEntry()), Mode.Normal);
                        }
                        break;
                }
            }

            // ---- helpers ----

            private static StatementFrame? NewFrame(GleamStatementLine? entry)
                => entry == null ? null : new StatementFrame(entry.Closures);

            private static bool IsGlueExpression(Node statement)
            {
                if (statement is ExpressionStatement { Expression: AssignmentExpression assignment })
                    return NameOf(assignment.Left) is { } name && IsTempName(name);
                return false;
            }

            private static bool IsGlueDeclaration(
                VariableDeclaration declaration, NodeList<Statement> statements, int index)
            {
                if (declaration.Declarations.Count != 1) return false;
                var declarator = declaration.Declarations[0];
                if (declarator.Id is not Identifier id) return false;
                var name = id.Name;

                // Compiler temporaries: loop$…, _pipe, _block, $ / $1 / $2…
                if (IsTempName(name))
                {
                    // `let $N = expr` is glue only when the temp is later read (case subject
                    // hoist, assert operands); a `let _ = expr` discard is a real statement.
                    if (IsDollarTemp(name))
                        return TempIsReferencedBeforeBoundary(statements, index, name);
                    return true;
                }

                var init = declarator.Init;
                if (init == null) return false;

                // List/record pattern destructuring: let first = xs.head / xs.tail / xs[0].
                if (init is MemberExpression member)
                {
                    if (member.Object is Identifier && (member.Property as Identifier)?.Name is "head" or "tail")
                        return true;
                    if (member.Computed && member.Object is Identifier)
                        return true;
                }

                // Self-recursion rebind: let xs = loop$xs.
                if (ReferencesLoopTemp(init)) return true;

                // Case-clause pattern bind (`x if … -> …`): let x = <subject> right before the
                // case's if. (Legit `let x = n` followed by a case is rare and harmless.)
                if (init is Identifier && statements[index + 1] is IfStatement)
                    return true;

                return false;
            }

            private static bool TempIsReferencedBeforeBoundary(
                NodeList<Statement> statements, int index, string tempName)
            {
                for (var i = index + 1; i < statements.Count; i++)
                {
                    var next = statements[i];
                    var isGlueLike = next is VariableDeclaration { Declarations.Count: 1 } vd
                                     && vd.Declarations[0].Id is Identifier nextId
                                     && IsTempName(nextId.Name);
                    if (ReferencesName(next, tempName)) return true;
                    if (!isGlueLike) return false;
                }
                return false;
            }

            private static bool ReferencesName(Node node, string name)
            {
                foreach (var child in node.ChildNodes)
                {
                    if (child is Identifier id && id.Name == name) return true;
                    if (child != null && ReferencesName(child, name)) return true;
                }
                return false;
            }

            private static bool ReferencesLoopTemp(Node expression)
            {
                foreach (var child in expression.ChildNodes)
                {
                    if (child is Identifier id && id.Name.StartsWith("loop$", StringComparison.Ordinal))
                        return true;
                    if (child != null && ReferencesLoopTemp(child)) return true;
                }
                return false;
            }

            private static bool IsTempName(string name)
                => name.StartsWith("loop$", StringComparison.Ordinal)
                   || name.StartsWith("_pipe", StringComparison.Ordinal)
                   || name == "_block"
                   || IsDollarTemp(name);

            private static bool IsDollarTemp(string name)
            {
                if (name.Length == 0 || name[0] != '$') return false;
                for (var i = 1; i < name.Length; i++)
                    if (name[i] < '0' || name[i] > '9') return false;
                return true;
            }

            private static string? NameOf(Node? node) => (node as Identifier)?.Name;
        }
    }
}