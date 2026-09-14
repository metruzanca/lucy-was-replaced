using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class Parser
{
	private static List<(List<TokenType> types, Func<TokenStream, CodeWindow, int, bool, TokenType, bool, Node> evaluationFunc)> operators = new List<(List<TokenType>, Func<TokenStream, CodeWindow, int, bool, TokenType, bool, Node>)>
	{
		(new List<TokenType> { TokenType.OR }, BinaryExpression),
		(new List<TokenType> { TokenType.AND }, BinaryExpression),
		(new List<TokenType> { TokenType.NOT }, UnaryExpression),
		(new List<TokenType>
		{
			TokenType.COMPARE,
			TokenType.IN
		}, BinaryExpression),
		(new List<TokenType> { TokenType.ADD }, BinaryExpression),
		(new List<TokenType> { TokenType.MULT }, BinaryExpression),
		(new List<TokenType> { TokenType.ADD }, UnaryExpression),
		(new List<TokenType> { TokenType.EXP }, BinaryExpression),
		(new List<TokenType>
		{
			TokenType.BRACKET_OPEN,
			TokenType.SQUARE_BRACKET_OPEN,
			TokenType.DOT
		}, IndexDotOrCallExpression)
	};

	public static Program Parse(TokenStream stream, CodeWindow f)
	{
		HashSet<string> allVars = new HashSet<string>();
		HashSet<string> importedModules = new HashSet<string>();
		HashSet<string> hashSet = new HashSet<string>();
		Node syntaxTree = Block(stream, f, -1, hashSet, hashSet, allVars, importedModules, global: true, isStatic: true);
		if (stream.Current != null)
		{
			throw new ParseException("error_code_after_block", stream.CurrentStringStartIndex, stream.CurrentStringEndIndex);
		}
		return new Program(syntaxTree, hashSet, allVars, importedModules);
	}

	private static DefNode Function(TokenStream stream, CodeWindow f, int indentation, HashSet<string> allVars, HashSet<string> importedModules, bool global = false, bool isStatic = false)
	{
		Token token = stream.Consume(TokenType.DEF);
		Token token2 = stream.Consume(TokenType.IDENTIFIER);
		OptionalGenericTypeAnnotation(stream);
		stream.Consume(TokenType.BRACKET_OPEN);
		LineBreaks(stream);
		HashSet<string> hashSet = new HashSet<string>();
		List<string> list = new List<string>();
		List<Node> list2 = new List<Node>();
		bool flag = false;
		while (true)
		{
			Token current = stream.Current;
			if (current != null && current.type == TokenType.BRACKET_CLOSE)
			{
				break;
			}
			string value = stream.Consume(TokenType.IDENTIFIER).value;
			list.Add(value);
			hashSet.Add(value);
			allVars.Add(value);
			OptionalVariableTypeAnnotation(stream);
			LineBreaks(stream);
			Token current2 = stream.Current;
			if (current2 != null && current2.type == TokenType.ASSIGN)
			{
				stream.Consume(TokenType.ASSIGN);
				LineBreaks(stream);
				list2.Add(Expression(stream, f, 0, canLineBreak: true));
				flag = true;
			}
			else if (flag)
			{
				throw new ParseException("error_missing_default_parameter", token.startIndex, stream.CurrentStringEndIndex);
			}
			Token current3 = stream.Current;
			if (current3 == null || current3.type != TokenType.COMMA)
			{
				break;
			}
			stream.Consume(TokenType.COMMA);
			LineBreaks(stream);
		}
		stream.Consume(TokenType.BRACKET_CLOSE);
		OptionalReturnTypeAnnotation(stream);
		Token token3 = ConsumeColon(stream);
		FunctionNode functionNode = new FunctionNode(list, token2.value, global, f, token.startIndex, token3.startIndex + token3.value.Length);
		functionNode.boxedParams.codeWindow = f;
		functionNode.slots.Add(Block(stream, f, indentation, hashSet, new HashSet<string>(), allVars, importedModules, global: false, isStatic: true));
		functionNode.Vars = hashSet;
		foreach (Node item in list2)
		{
			functionNode.slots.Add(item);
		}
		return new DefNode(token2.value, isStatic, f, token.startIndex, token.startIndex + token.value.Length)
		{
			slots = { (Node)functionNode }
		};
	}

	private static Node Block(TokenStream stream, CodeWindow f, int prevIndentation, HashSet<string> vars, HashSet<string> globalVars, HashSet<string> allVars, HashSet<string> importedModules, bool global = false, bool isStatic = false)
	{
		if (global && stream.Current == null)
		{
			return new SequenceNode(f);
		}
		Token current = stream.Current;
		if (current == null || current.type != TokenType.NEW_LINE)
		{
			throw new ParseException("error_no_statements", stream.CurrentStringStartIndex, stream.CurrentStringEndIndex);
		}
		int startIndex = stream.CurrentStringStartIndex + 1;
		int currentStringEndIndex = stream.CurrentStringEndIndex;
		int indentation = GetIndentation(stream);
		if (indentation <= prevIndentation)
		{
			throw new ParseException("error_not_enough_indentation", startIndex, currentStringEndIndex);
		}
		List<Node> list = new List<Node>();
		int num;
		for (num = indentation; num == indentation; num = GetIndentation(stream))
		{
			stream.Consume(TokenType.NEW_LINE);
			list.Add(Statement(stream, f, indentation, vars, globalVars, allVars, importedModules, global, isStatic));
			Token current2 = stream.Current;
			if (current2 == null || current2.type != TokenType.NEW_LINE)
			{
				break;
			}
		}
		startIndex = stream.CurrentStringStartIndex + 1;
		currentStringEndIndex = stream.CurrentStringEndIndex;
		if (num > indentation)
		{
			throw new ParseException("error_too_much_indentation", startIndex, currentStringEndIndex);
		}
		if (!global && list.Count == 0)
		{
			throw new ParseException("error_no_statements", startIndex, currentStringEndIndex);
		}
		return new SequenceNode(f)
		{
			slots = list
		};
	}

	private static Node Statement(TokenStream stream, CodeWindow f, int indentation, HashSet<string> vars, HashSet<string> globalVars, HashSet<string> allVars, HashSet<string> importedModules, bool global = false, bool isStatic = false)
	{
		Token current = stream.Current;
		if (current != null && current.type == TokenType.DEF)
		{
			DefNode defNode = Function(stream, f, indentation, allVars, importedModules, global, isStatic);
			if (!globalVars.Contains(defNode.funcName))
			{
				vars.Add(defNode.funcName);
			}
			allVars.Add(defNode.funcName);
			return defNode;
		}
		Token current2 = stream.Current;
		if (current2 != null && current2.type == TokenType.PASS)
		{
			Token token = stream.Consume(TokenType.PASS);
			return new PassNode(f, token.startIndex, token.startIndex + token.value.Length);
		}
		Token current3 = stream.Current;
		if (current3 != null && current3.type == TokenType.BREAK)
		{
			Token token2 = stream.Consume(TokenType.BREAK);
			return new BreakNode(f, token2.startIndex, token2.startIndex + token2.value.Length);
		}
		Token current4 = stream.Current;
		if (current4 != null && current4.type == TokenType.CONTINUE)
		{
			Token token3 = stream.Consume(TokenType.CONTINUE);
			return new ContinueNode(f, token3.startIndex, token3.startIndex + token3.value.Length);
		}
		Token current5 = stream.Current;
		if (current5 != null && current5.type == TokenType.RETURN)
		{
			Token token4 = stream.Consume(TokenType.RETURN);
			Node node = new ReturnNode(f, token4.startIndex, token4.startIndex + token4.value.Length);
			if (stream.Current != null)
			{
				Token current6 = stream.Current;
				if (current6 == null || current6.type != TokenType.NEW_LINE)
				{
					node.slots.Add(TupleOrExpression(stream, f));
				}
			}
			return node;
		}
		Token current7 = stream.Current;
		if (current7 != null && current7.type == TokenType.GLOBAL)
		{
			stream.Consume(TokenType.GLOBAL);
			Token token5 = stream.Consume(TokenType.IDENTIFIER);
			OptionalVariableTypeAnnotation(stream);
			if (vars.Contains(token5.value))
			{
				throw new ParseException(CodeUtilities.LocalizeAndFormat("error_assign_before_global", token5.value), token5.startIndex, token5.startIndex + token5.value.Length);
			}
			globalVars.Add(token5.value);
			return new NoOpNode(f, token5.startIndex, token5.startIndex + token5.value.Length);
		}
		Token current8 = stream.Current;
		if (current8 != null && current8.type == TokenType.IMPORT)
		{
			Token token6 = stream.Consume(TokenType.IMPORT);
			List<string> list = SequenceOfIdentifiers(stream, "error_invalid_import");
			foreach (string item in list)
			{
				if (!globalVars.Contains(item))
				{
					vars.Add(item);
				}
				allVars.Add(item);
			}
			return new ImportNode(f, token6.startIndex, stream.CurrentStringStartIndex - 1, list, unpack: false, unpackAll: false, null, isStatic);
		}
		Token current10 = stream.Current;
		if (current10 != null && current10.type == TokenType.FROM)
		{
			Token token7 = stream.Consume(TokenType.FROM);
			Token token8 = stream.Consume(TokenType.IDENTIFIER, "error_invalid_import");
			stream.Consume(TokenType.IMPORT, "error_invalid_import");
			if (stream.Current?.value == "*")
			{
				if (!global)
				{
					throw new ParseException("error_wildcard_imports_not_allowed_in_function", stream.CurrentStringStartIndex, stream.CurrentStringEndIndex);
				}
				stream.Consume(TokenType.MULT);
				importedModules.Add(token8.value);
				return new ImportNode(f, token7.startIndex, stream.CurrentStringStartIndex, new List<string> { token8.value }, unpack: true, unpackAll: true, null, isStatic);
			}
			List<string> list2 = SequenceOfIdentifiers(stream);
			foreach (string item2 in list2)
			{
				if (!globalVars.Contains(item2))
				{
					vars.Add(item2);
				}
				allVars.Add(item2);
			}
			return new ImportNode(f, token7.startIndex, stream.CurrentStringStartIndex, new List<string> { token8.value }, unpack: true, unpackAll: false, list2, isStatic);
		}
		Token current12 = stream.Current;
		if (current12 == null || current12.type != TokenType.WHILE)
		{
			Token current13 = stream.Current;
			if (current13 == null || current13.type != TokenType.FOR)
			{
				Token current14 = stream.Current;
				if (current14 == null || current14.type != TokenType.IF)
				{
					Node node2 = TupleOrExpression(stream, f, canLineBreak: false, TokenType.ASSIGN, null, isLeftmostExpression: true);
					Token current15 = stream.Current;
					if (current15 != null && current15.type == TokenType.ASSIGN)
					{
						return Assignment(stream, f, node2, vars, globalVars, allVars);
					}
					if (!(node2 is FunctionNode) && !(node2 is CallNode))
					{
						if (node2 is ValueNode)
						{
							string value = ((ValueNode)node2).value;
							if (BuiltinFunctions.Functions.ContainsKey(value) || BuiltinFunctions.Methods.ContainsKey(value))
							{
								throw new ParseException(CodeUtilities.LocalizeAndFormat("error_not_a_statement2", value + "()"), node2.boxedParams.wordStart, node2.boxedParams.wordEnd);
							}
						}
						throw new ParseException("error_not_a_statement", node2.boxedParams.wordStart, node2.boxedParams.wordEnd);
					}
					return node2;
				}
			}
		}
		return FlowControll(stream, f, indentation, vars, globalVars, allVars, importedModules);
	}

	private static List<string> SequenceOfIdentifiers(TokenStream stream, string error = null)
	{
		List<string> list = new List<string>();
		Token current = stream.Current;
		if (current != null && current.type == TokenType.BRACKET_OPEN)
		{
			stream.Consume(TokenType.BRACKET_OPEN);
			LineBreaks(stream);
			list.Add(stream.Consume(TokenType.IDENTIFIER, error).value);
			LineBreaks(stream);
			while (true)
			{
				Token current2 = stream.Current;
				if (current2 != null && current2.type == TokenType.COMMA)
				{
					stream.Consume(TokenType.COMMA);
					LineBreaks(stream);
					Token current3 = stream.Current;
					if (current3 != null && current3.type == TokenType.BRACKET_CLOSE)
					{
						stream.Consume(TokenType.BRACKET_CLOSE);
						break;
					}
					list.Add(stream.Consume(TokenType.IDENTIFIER, error).value);
					LineBreaks(stream);
					continue;
				}
				stream.Consume(TokenType.BRACKET_CLOSE, error);
				break;
			}
		}
		else
		{
			list.Add(stream.Consume(TokenType.IDENTIFIER, error).value);
			while (true)
			{
				Token current4 = stream.Current;
				if (current4 == null || current4.type != TokenType.COMMA)
				{
					break;
				}
				stream.Consume(TokenType.COMMA);
				list.Add(stream.Consume(TokenType.IDENTIFIER, error).value);
			}
			if (error != null && stream.Current != null)
			{
				Token current5 = stream.Current;
				if (current5 == null || current5.type != TokenType.NEW_LINE)
				{
					throw new ParseException(error, stream.CurrentStringStartIndex, stream.CurrentStringEndIndex);
				}
			}
		}
		return list;
	}

	private static Node Assignment(TokenStream stream, CodeWindow f, Node lhs, HashSet<string> vars, HashSet<string> globalVars, HashSet<string> allVars)
	{
		Token token = stream.Consume(TokenType.ASSIGN);
		Node item = TupleOrExpression(stream, f);
		CheckAssignmentLhsRec(lhs, f, vars, globalVars, allVars);
		return new AssignmentNode(token.value, f, token.startIndex, token.startIndex + token.value.Length)
		{
			slots = { lhs, item }
		};
	}

	private static void CheckAssignmentLhsRec(Node lhs, CodeWindow f, HashSet<string> vars, HashSet<string> globalVars, HashSet<string> allVars)
	{
		if (lhs is ValueNode)
		{
			ValueNode valueNode = (ValueNode)lhs;
			string value = valueNode.value;
			if (Farm.allKeyWords.Contains(value) || Scope.IsConstant(value))
			{
				throw new ParseException(CodeUtilities.LocalizeAndFormat("error_reserved_keyword", value), valueNode.boxedParams.wordStart, valueNode.boxedParams.wordEnd);
			}
			if (double.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var _) || value.StartsWith('"') || value.StartsWith('\''))
			{
				throw new ParseException(CodeUtilities.LocalizeAndFormat("error_invalid_name", value), valueNode.boxedParams.wordStart, valueNode.boxedParams.wordEnd);
			}
			if (!globalVars.Contains(value))
			{
				vars.Add(value);
			}
			allVars.Add(value);
			return;
		}
		if (lhs is TupleNode || lhs is ListNode)
		{
			foreach (Node slot in lhs.slots[0].slots)
			{
				CheckAssignmentLhsRec(slot, f, vars, globalVars, allVars);
			}
			return;
		}
		if (lhs is BracketNode)
		{
			CheckAssignmentLhsRec(lhs.slots[0], f, vars, globalVars, allVars);
		}
		else if (!(lhs is BinaryExprNode binaryExprNode) || (!(binaryExprNode.op == ".") && (!(binaryExprNode.op == "[]") || binaryExprNode.slots[1].slots.Count != 1)))
		{
			throw new ParseException("error_invalid_assign_expr", lhs.boxedParams.wordStart, lhs.boxedParams.wordEnd);
		}
	}

	private static Node FlowControll(TokenStream stream, CodeWindow f, int indentation, HashSet<string> vars, HashSet<string> globalVars, HashSet<string> allVars, HashSet<string> importedModules, bool isElif = false)
	{
		bool flag = false;
		Token current = stream.Current;
		Node node;
		if (current != null && current.type == TokenType.WHILE)
		{
			Token token = stream.Consume(TokenType.WHILE);
			node = new BranchNode(f, token.startIndex, token.startIndex + token.value.Length, looping: true);
		}
		else
		{
			Token current2 = stream.Current;
			if (current2 != null && current2.type == TokenType.FOR)
			{
				Token token2 = stream.Consume(TokenType.FOR);
				Node node2 = TupleOrExpression(stream, f, canLineBreak: false, TokenType.IN);
				CheckAssignmentLhsRec(node2, f, vars, globalVars, allVars);
				ForNode forNode = new ForNode(node2, f, token2.startIndex, token2.startIndex + token2.value.Length);
				stream.Consume(TokenType.IN, "error_invalid_for_syntax");
				node = forNode;
			}
			else
			{
				Token token3 = stream.Consume(isElif ? TokenType.ELIF : TokenType.IF);
				node = new BranchNode(f, token3.startIndex, token3.startIndex + token3.value.Length);
				flag = true;
			}
		}
		Node item = TupleOrExpression(stream, f);
		Token current3 = stream.Current;
		if (current3 != null && current3.type == TokenType.ASSIGN)
		{
			throw new ParseException("error_unexpected_assign", stream.CurrentStringStartIndex, stream.CurrentStringEndIndex);
		}
		ConsumeColon(stream);
		Node item2 = Block(stream, f, indentation, vars, globalVars, allVars, importedModules);
		node.slots.Add(item);
		node.slots.Add(item2);
		node.boxedParams.codeWindow = f;
		if (flag)
		{
			Token lookAhead = stream.LookAhead;
			if (lookAhead != null && lookAhead.type == TokenType.ELSE && GetIndentation(stream) == indentation)
			{
				stream.Consume(TokenType.NEW_LINE, "error_new_line_expected");
				stream.Consume(TokenType.ELSE);
				ConsumeColon(stream);
				node.slots.Add(Block(stream, f, indentation, vars, globalVars, allVars, importedModules));
				goto IL_021f;
			}
		}
		if (flag)
		{
			Token lookAhead2 = stream.LookAhead;
			if (lookAhead2 != null && lookAhead2.type == TokenType.ELIF && GetIndentation(stream) == indentation)
			{
				stream.Consume(TokenType.NEW_LINE, "error_new_line_expected");
				node.slots.Add(FlowControll(stream, f, indentation, vars, globalVars, allVars, importedModules, isElif: true));
			}
		}
		goto IL_021f;
		IL_021f:
		return node;
	}

	private static Node Sequence(TokenStream stream, CodeWindow f, TokenType endToken, out bool keyValuePairs, bool allowKeyValuePairs = false, bool lineBreaks = true)
	{
		keyValuePairs = true;
		Node node = new SequenceNode(f);
		if (lineBreaks)
		{
			LineBreaks(stream);
		}
		Token current = stream.Current;
		if (current == null || current.type != endToken)
		{
			Token current2 = stream.Current;
			if ((current2 == null || current2.type != TokenType.NEW_LINE || lineBreaks) && (stream.Current != null || lineBreaks))
			{
				while (true)
				{
					Node item = Expression(stream, f, 0, lineBreaks, endToken);
					if (lineBreaks)
					{
						LineBreaks(stream);
					}
					Token current3 = stream.Current;
					if (current3 != null && current3.type == TokenType.COLON)
					{
						if (!keyValuePairs || !allowKeyValuePairs)
						{
							throw new ParseException("error_invalid_expression", stream.CurrentStringStartIndex, stream.CurrentStringEndIndex);
						}
						stream.Consume(TokenType.COLON, "error_wrong_dict_literal");
						if (lineBreaks)
						{
							LineBreaks(stream);
						}
						Node item2 = Expression(stream, f, 0, lineBreaks, endToken);
						node.slots.Add(item);
						node.slots.Add(item2);
					}
					else
					{
						if (keyValuePairs && node.slots.Count > 0)
						{
							throw new ParseException("error_invalid_expression", stream.CurrentStringStartIndex, stream.CurrentStringEndIndex);
						}
						node.slots.Add(item);
						keyValuePairs = false;
					}
					if (lineBreaks)
					{
						LineBreaks(stream);
					}
					Token current4 = stream.Current;
					bool flag = current4 != null && current4.type == TokenType.COMMA;
					if (flag)
					{
						stream.Consume(TokenType.COMMA);
						if (lineBreaks)
						{
							LineBreaks(stream);
						}
					}
					Token current5 = stream.Current;
					int num;
					if (current5 == null || current5.type != endToken)
					{
						Token current6 = stream.Current;
						if (current6 == null || current6.type != TokenType.NEW_LINE || lineBreaks)
						{
							num = ((stream.Current == null && !lineBreaks) ? 1 : 0);
							goto IL_019c;
						}
					}
					num = 1;
					goto IL_019c;
					IL_019c:
					bool flag2 = (byte)num != 0;
					if (flag2)
					{
						break;
					}
					if (!flag && !flag2)
					{
						if (lineBreaks)
						{
							throw new ParseException("error_expected_close_token", stream.CurrentStringStartIndex, stream.CurrentStringEndIndex);
						}
						throw new ParseException("error_invalid_expression", stream.CurrentStringStartIndex, stream.CurrentStringEndIndex);
					}
				}
				return node;
			}
		}
		return node;
	}

	private static Node TupleOrExpression(TokenStream stream, CodeWindow f, bool canLineBreak = false, TokenType endOfTupleToken = TokenType.BRACKET_CLOSE, Node startExpr = null, bool isLeftmostExpression = false)
	{
		if (startExpr == null)
		{
			Token current = stream.Current;
			if (current != null && current.type == endOfTupleToken)
			{
				Node item = new SequenceNode(f);
				return new TupleNode(f, stream.CurrentStringStartIndex, stream.CurrentStringStartIndex)
				{
					slots = { item }
				};
			}
		}
		Node node = ((startExpr != null) ? startExpr : Expression(stream, f, 0, canLineBreak, endOfTupleToken, isLeftmostExpression));
		Token current2 = stream.Current;
		if (current2 != null && current2.type == TokenType.COMMA)
		{
			stream.Consume(TokenType.COMMA);
			bool keyValuePairs;
			Node node2 = Sequence(stream, f, endOfTupleToken, out keyValuePairs, allowKeyValuePairs: false, canLineBreak);
			node2.slots.Insert(0, node);
			return new TupleNode(f, stream.CurrentStringStartIndex, stream.CurrentStringStartIndex)
			{
				slots = { node2 }
			};
		}
		return node;
	}

	private static Node Expression(TokenStream stream, CodeWindow f, int opIndex = 0, bool canLineBreak = false, TokenType excludeToken = TokenType.NO_TOKEN, bool isLeftmost = false)
	{
		if (canLineBreak)
		{
			LineBreaks(stream);
		}
		if (opIndex < operators.Count && operators[opIndex].types.Contains(excludeToken))
		{
			opIndex++;
		}
		if (opIndex < operators.Count)
		{
			return operators[opIndex].evaluationFunc(stream, f, opIndex, canLineBreak, excludeToken, isLeftmost);
		}
		return AtomicExpression(stream, f, isLeftmost);
	}

	private static void LineBreaks(TokenStream stream)
	{
		while (true)
		{
			Token current = stream.Current;
			if (current != null && current.type == TokenType.NEW_LINE)
			{
				stream.Consume(TokenType.NEW_LINE);
				continue;
			}
			break;
		}
	}

	public static void OptionalVariableTypeAnnotation(TokenStream stream)
	{
		Token current = stream.Current;
		if (current == null || current.type != TokenType.COLON)
		{
			return;
		}
		Token lookAhead = stream.LookAhead;
		if (lookAhead == null || lookAhead.type != TokenType.STRING)
		{
			Token lookAhead2 = stream.LookAhead;
			if (lookAhead2 == null || lookAhead2.type != TokenType.IDENTIFIER)
			{
				return;
			}
		}
		stream.Consume(TokenType.COLON);
		TypeExpression(stream);
	}

	public static void OptionalReturnTypeAnnotation(TokenStream stream)
	{
		Token current = stream.Current;
		if (current != null && current.type == TokenType.ARROW)
		{
			stream.Consume(TokenType.ARROW);
			TypeExpression(stream);
		}
	}

	public static void OptionalGenericTypeAnnotation(TokenStream stream)
	{
		Token current = stream.Current;
		if (current != null && current.type == TokenType.SQUARE_BRACKET_OPEN)
		{
			TypeExpressionList(stream);
		}
	}

	public static void TypeExpression(TokenStream stream, bool identifierIsOptional = false)
	{
		Token current = stream.Current;
		if (current != null && current.type == TokenType.STRING)
		{
			stream.Consume(TokenType.STRING);
			return;
		}
		if (identifierIsOptional)
		{
			Token current2 = stream.Current;
			if (current2 != null && current2.type == TokenType.IDENTIFIER)
			{
				stream.Consume(TokenType.IDENTIFIER);
			}
		}
		else
		{
			stream.Consume(TokenType.IDENTIFIER);
		}
		Token current3 = stream.Current;
		if (current3 != null && current3.type == TokenType.SQUARE_BRACKET_OPEN)
		{
			TypeExpressionList(stream);
		}
		Token current4 = stream.Current;
		if (current4 != null && current4.type == TokenType.UNION)
		{
			stream.Consume(TokenType.UNION);
			TypeExpression(stream);
		}
	}

	public static void TypeExpressionList(TokenStream stream)
	{
		stream.Consume(TokenType.SQUARE_BRACKET_OPEN);
		Token current = stream.Current;
		if (current != null && current.type == TokenType.BRACKET_OPEN)
		{
			EmptyTuple(stream);
		}
		else
		{
			TypeExpression(stream, identifierIsOptional: true);
			while (true)
			{
				Token current2 = stream.Current;
				if (current2 == null || current2.type != TokenType.COMMA)
				{
					break;
				}
				stream.Consume(TokenType.COMMA);
				Token current3 = stream.Current;
				if (current3 != null && current3.type == TokenType.SQUARE_BRACKET_CLOSE)
				{
					break;
				}
				TypeExpression(stream, identifierIsOptional: true);
			}
		}
		stream.Consume(TokenType.SQUARE_BRACKET_CLOSE);
	}

	private static void EmptyTuple(TokenStream stream)
	{
		stream.Consume(TokenType.BRACKET_OPEN);
		stream.Consume(TokenType.BRACKET_CLOSE);
	}

	private static Node BracketExpression(TokenStream stream, CodeWindow f)
	{
		stream.Consume(TokenType.BRACKET_OPEN);
		Node node = TupleOrExpression(stream, f, canLineBreak: true);
		LineBreaks(stream);
		stream.Consume(TokenType.BRACKET_CLOSE);
		return new BracketNode(f, node.boxedParams.wordStart, node.boxedParams.wordEnd)
		{
			slots = { node }
		};
	}

	private static Node BinaryExpression(TokenStream stream, CodeWindow f, int opIndex, bool canLineBreak, TokenType excludeToken, bool isLeftmost = false)
	{
		if (canLineBreak)
		{
			LineBreaks(stream);
		}
		Node node = Expression(stream, f, opIndex + 1, canLineBreak, excludeToken, isLeftmost);
		if (canLineBreak)
		{
			LineBreaks(stream);
		}
		foreach (TokenType item in operators[opIndex].types)
		{
			Token current2 = stream.Current;
			if (current2 == null || current2.type != item)
			{
				continue;
			}
			if (item == TokenType.COMPARE || item == TokenType.IN)
			{
				List<Node> list = new List<Node> { node };
				List<string> list2 = new List<string>();
				while (true)
				{
					Token current3 = stream.Current;
					if (current3 == null || current3.type != TokenType.COMPARE)
					{
						Token current4 = stream.Current;
						if (current4 == null || current4.type != TokenType.IN)
						{
							break;
						}
					}
					Token token = stream.Consume();
					list.Add(BinaryExpression(stream, f, opIndex + 1, canLineBreak, excludeToken));
					list2.Add(token.value);
				}
				return new ComparisonNode(list2, f, node.boxedParams.wordStart, stream.CurrentStringEndIndex)
				{
					slots = list
				};
			}
			Token token2 = stream.Consume(item);
			Node node2 = BinaryExpression(stream, f, opIndex, canLineBreak, excludeToken);
			Node node3 = new BinaryExprNode(token2.value, token2.type, f, token2.startIndex, token2.startIndex + token2.value.Length);
			node3.slots.Add(node);
			if (!(node2 is BinaryExprNode) || !operators[opIndex].types.Contains(((BinaryExprNode)node2).opTokenType))
			{
				node3.slots.Add(node2);
				return node3;
			}
			Node node4 = node2;
			while (node4.slots[0] is BinaryExprNode && operators[opIndex].types.Contains(((BinaryExprNode)node4.slots[0]).opTokenType))
			{
				node4 = node4.slots[0];
			}
			node3.slots.Add(node4.slots[0]);
			node4.slots[0] = node3;
			return node2;
		}
		return node;
	}

	private static Node UnaryExpression(TokenStream stream, CodeWindow f, int opIndex, bool canLineBreak, TokenType excludeToken, bool isLeftmost = false)
	{
		if (canLineBreak)
		{
			LineBreaks(stream);
		}
		foreach (TokenType item2 in operators[opIndex].types)
		{
			Token current2 = stream.Current;
			if (current2 != null && current2.type == item2)
			{
				Token token = stream.Consume(item2);
				Node item = Expression(stream, f, opIndex, canLineBreak);
				return new UnaryExprNode(token.value, f, token.startIndex, token.startIndex + token.value.Length)
				{
					slots = { item }
				};
			}
		}
		return Expression(stream, f, opIndex + 1, canLineBreak, excludeToken, isLeftmost);
	}

	private static Node IndexDotOrCallExpression(TokenStream stream, CodeWindow f, int opIndex, bool canLineBreak, TokenType excludeToken, bool isLeftmost = false)
	{
		if (canLineBreak)
		{
			LineBreaks(stream);
		}
		Node node = Expression(stream, f, opIndex + 1, canLineBreak, excludeToken, isLeftmost);
		if (canLineBreak)
		{
			LineBreaks(stream);
		}
		while (true)
		{
			Token current = stream.Current;
			if (current == null || current.type != TokenType.SQUARE_BRACKET_OPEN)
			{
				Token current2 = stream.Current;
				if (current2 == null || current2.type != TokenType.BRACKET_OPEN)
				{
					Token current3 = stream.Current;
					if (current3 == null || current3.type != TokenType.DOT)
					{
						break;
					}
				}
			}
			Token current4 = stream.Current;
			if (current4 != null && current4.type == TokenType.SQUARE_BRACKET_OPEN)
			{
				Token token = stream.Consume(TokenType.SQUARE_BRACKET_OPEN);
				Node node2 = new SequenceNode(f);
				node2.slots.Add(SliceIndex(stream, f));
				Token current5 = stream.Current;
				if (current5 != null && current5.type == TokenType.COLON)
				{
					stream.Consume(TokenType.COLON);
					node2.slots.Add(SliceIndex(stream, f, couldBeLastSlice: true));
					Token current6 = stream.Current;
					if (current6 != null && current6.type == TokenType.COLON)
					{
						stream.Consume(TokenType.COLON);
						LineBreaks(stream);
						Token current7 = stream.Current;
						if (current7 == null || current7.type != TokenType.SQUARE_BRACKET_CLOSE)
						{
							node2.slots.Add(Expression(stream, f, 0, canLineBreak: true));
						}
					}
				}
				LineBreaks(stream);
				stream.Consume(TokenType.SQUARE_BRACKET_CLOSE);
				node = new BinaryExprNode("[]", TokenType.BRACKET_OPEN, f, token.startIndex, token.startIndex + token.value.Length)
				{
					slots = { node, node2 }
				};
				continue;
			}
			Token current8 = stream.Current;
			if (current8 != null && current8.type == TokenType.BRACKET_OPEN)
			{
				Token token2 = stream.Consume(TokenType.BRACKET_OPEN);
				bool keyValuePairs;
				Node item = Sequence(stream, f, TokenType.BRACKET_CLOSE, out keyValuePairs);
				stream.Consume(TokenType.BRACKET_CLOSE);
				node = new CallNode(f, token2.startIndex, token2.startIndex + token2.value.Length)
				{
					slots = { node, item }
				};
				continue;
			}
			Token current9 = stream.Current;
			if (current9 == null || current9.type != TokenType.DOT)
			{
				continue;
			}
			Token token3 = stream.Consume(TokenType.DOT);
			Token token4 = stream.Consume(TokenType.IDENTIFIER);
			if (node is LiteralNode { value: PyConstBag value })
			{
				try
				{
					node = new LiteralNode(value.Evaluate(token4.value, node.boxedParams.wordStart, node.boxedParams.wordEnd).val, f, node.boxedParams.wordStart, token4.startIndex + token4.value.Length);
				}
				catch (ExecuteException ex)
				{
					throw new ParseException(ex.Message, ex.startIndex, ex.endIndex);
				}
			}
			else
			{
				Node item2 = new ValueNode(token4.value, f, token4.startIndex, token4.startIndex + token4.value.Length);
				node = new BinaryExprNode(".", TokenType.DOT, f, token3.startIndex, token3.startIndex + token3.value.Length)
				{
					slots = { node, item2 }
				};
			}
		}
		return node;
	}

	private static Node SliceIndex(TokenStream stream, CodeWindow f, bool couldBeLastSlice = false)
	{
		LineBreaks(stream);
		Token current = stream.Current;
		Node node;
		if (current == null || current.type != TokenType.COLON)
		{
			Token current2 = stream.Current;
			if (!(current2 != null && current2.type == TokenType.SQUARE_BRACKET_CLOSE && couldBeLastSlice))
			{
				node = Expression(stream, f, 0, canLineBreak: true);
				Token current3 = stream.Current;
				if (current3 == null || current3.type != TokenType.COLON)
				{
					node = TupleOrExpression(stream, f, canLineBreak: true, TokenType.SQUARE_BRACKET_CLOSE, node);
				}
				goto IL_0089;
			}
		}
		node = new LiteralNode(new PyNone(), f, stream.CurrentStringStartIndex, stream.CurrentStringStartIndex);
		goto IL_0089;
		IL_0089:
		LineBreaks(stream);
		return node;
	}

	private static Node AtomicExpression(TokenStream stream, CodeWindow f, bool canBeTypeAnnotated)
	{
		Token current = stream.Current;
		if (current != null && current.type == TokenType.BRACKET_OPEN)
		{
			return BracketExpression(stream, f);
		}
		Token current2 = stream.Current;
		if (current2 != null && current2.type == TokenType.SQUARE_BRACKET_OPEN)
		{
			int currentStringStartIndex = stream.CurrentStringStartIndex;
			int currentStringEndIndex = stream.CurrentStringEndIndex;
			stream.Consume(TokenType.SQUARE_BRACKET_OPEN);
			bool keyValuePairs;
			Node item = Sequence(stream, f, TokenType.SQUARE_BRACKET_CLOSE, out keyValuePairs);
			stream.Consume(TokenType.SQUARE_BRACKET_CLOSE);
			return new ListNode(f, currentStringStartIndex, currentStringEndIndex)
			{
				slots = { item }
			};
		}
		Token current3 = stream.Current;
		if (current3 != null && current3.type == TokenType.CURL_BRACE_OPEN)
		{
			int currentStringStartIndex2 = stream.CurrentStringStartIndex;
			int currentStringEndIndex2 = stream.CurrentStringEndIndex;
			stream.Consume(TokenType.CURL_BRACE_OPEN);
			bool keyValuePairs2;
			Node item2 = Sequence(stream, f, TokenType.CURL_BRACE_CLOSE, out keyValuePairs2, allowKeyValuePairs: true);
			stream.Consume(TokenType.CURL_BRACE_CLOSE);
			if (keyValuePairs2)
			{
				return new DictNode(f, currentStringStartIndex2, currentStringEndIndex2)
				{
					slots = { item2 }
				};
			}
			return new SetNode(f, currentStringStartIndex2, currentStringEndIndex2)
			{
				slots = { item2 }
			};
		}
		Token current4 = stream.Current;
		if (current4 != null && current4.type == TokenType.NUM)
		{
			Token token = stream.Consume(TokenType.NUM);
			double num;
			try
			{
				num = double.Parse(token.value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
			}
			catch (OverflowException)
			{
				num = double.PositiveInfinity;
			}
			catch (FormatException)
			{
				throw new ParseException("error_invalid_number_format", token.startIndex, token.startIndex + token.value.Length);
			}
			return new LiteralNode(new PyNumber(num), f, token.startIndex, token.startIndex + token.value.Length);
		}
		Token current5 = stream.Current;
		if (current5 != null && current5.type == TokenType.STRING)
		{
			Token token2 = stream.Consume(TokenType.STRING);
			return new LiteralNode(new PyString(token2.value.Substring(1, token2.value.Length - 2)), f, token2.startIndex, token2.startIndex + token2.value.Length);
		}
		Token current6 = stream.Current;
		if (current6 != null && current6.type == TokenType.IDENTIFIER)
		{
			Token token3 = stream.Consume(TokenType.IDENTIFIER);
			if (canBeTypeAnnotated)
			{
				OptionalVariableTypeAnnotation(stream);
			}
			IPyObject pyObject = Scope.EvaluateConstant(token3.value);
			if (pyObject != null)
			{
				return new LiteralNode(pyObject, f, token3.startIndex, token3.startIndex + token3.value.Length);
			}
			return new ValueNode(token3.value, f, token3.startIndex, token3.startIndex + token3.value.Length);
		}
		throw new ParseException("error_invalid_expression", stream.CurrentStringStartIndex, stream.CurrentStringEndIndex);
	}

	private static void ProcessChainedComparisons(Node tree)
	{
		List<(Node, Node, int)> list = new List<(Node, Node, int)>();
		list.Add((tree, null, 0));
		while (list.Count > 0)
		{
			var (node, node2, _) = list[list.Count - 1];
			list.RemoveAt(list.Count - 1);
			if (node2 != null && node is BinaryExprNode { opTokenType: TokenType.COMPARE } binaryExprNode && binaryExprNode.slots[0] is BinaryExprNode binaryExprNode2)
			{
				_ = binaryExprNode2.opTokenType;
				_ = 26;
			}
		}
	}

	private static int GetIndentation(TokenStream stream)
	{
		Token current = stream.Current;
		if (current == null || current.type != TokenType.NEW_LINE)
		{
			throw new ParseException("error_new_line_expected", stream.CurrentStringStartIndex, stream.CurrentStringEndIndex);
		}
		while (true)
		{
			Token lookAhead = stream.LookAhead;
			if (lookAhead == null || lookAhead.type != TokenType.NEW_LINE)
			{
				break;
			}
			stream.Consume(TokenType.NEW_LINE);
		}
		string value = stream.Current.value;
		if (value.Contains('\t') && value.Contains(' '))
		{
			throw new ParseException("error_mixed_indentation", stream.CurrentStringStartIndex + 1, stream.CurrentStringEndIndex);
		}
		return Enumerable.Count<char>((IEnumerable<char>)value, (Func<char, bool>)((char c) => c == ' ')) + Enumerable.Count<char>((IEnumerable<char>)value, (Func<char, bool>)((char c) => c == '\t')) * 4;
	}

	private static Token ConsumeColon(TokenStream stream)
	{
		Token current = stream.Current;
		if (current != null && current.type == TokenType.NEW_LINE)
		{
			stream.Consume(TokenType.COLON, "error_missing_colon", moveErrorBack: true);
		}
		return stream.Consume(TokenType.COLON);
	}
}
