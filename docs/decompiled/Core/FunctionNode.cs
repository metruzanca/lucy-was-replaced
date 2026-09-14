using System;
using System.Collections.Generic;
using System.Linq;

public class FunctionNode : Node
{
	private List<string> paramNames;

	private string funcName;

	public override string NodeName => "func";

	public string FuncName => funcName;

	public HashSet<string> Vars { get; set; }

	public List<IPyObject> Arguments { get; set; }

	public FunctionNode(List<string> paramNames, string funcName, bool global, CodeWindow codeWindow, int startIndex, int endIndex)
		: base(codeWindow, startIndex, endIndex)
	{
		this.paramNames = paramNames;
		this.funcName = funcName;
		if (global)
		{
			codeWindow.parsedFunctions[funcName] = this;
		}
	}

	public FunctionNode(List<string> paramNames, string funcName, bool global, BoxedNodeParams boxedParams)
		: base(boxedParams)
	{
		this.paramNames = paramNames;
		this.funcName = funcName;
		if (global)
		{
			boxedParams.codeWindow.parsedFunctions[funcName] = this;
		}
	}

	public override IEnumerable<double> Execute(ProgramState state, Execution execution, int depth)
	{
		int num = paramNames.Count - Arguments.Count;
		int numDefaultValues = slots.Count - 1;
		if (num < 0 || num > numDefaultValues)
		{
			PyTuple pyTuple = new PyTuple(Arguments);
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_wrong_number_args", funcName, paramNames.Count, pyTuple));
		}
		ErrorsAndBreakpoints(state, execution, depth);
		for (int i = numDefaultValues - num; i < numDefaultValues; i++)
		{
			foreach (double item in slots[i + 1].Execute(state, execution, depth + 1))
			{
				yield return item;
			}
			Arguments.Add(state.ReturnValue);
		}
		for (int j = 0; j < paramNames.Count; j++)
		{
			state.CurrentScope.SetVar(paramNames[j], Arguments[j]);
		}
		Blink(state, execution);
		IEnumerator<double> block = slots[0].Execute(state, execution, depth + 1).GetEnumerator();
		while (true)
		{
			double current;
			try
			{
				if (!block.MoveNext())
				{
					break;
				}
				current = block.Current;
				goto IL_0269;
			}
			catch (BreakStatement)
			{
				throw new ExecuteException("error_no_loop_to_break");
			}
			catch (ContinueStatement)
			{
				throw new ExecuteException("error_no_loop_to_continue");
			}
			catch (ReturnStatement)
			{
				state.PopScope();
				yield break;
			}
			IL_0269:
			yield return current;
		}
		state.ReturnValue = new PyNone();
		state.IsExpressionStatic = false;
		state.PopScope();
	}

	public string GetSignature()
	{
		return GetSubstringWithComments(boxedParams.codeWindow.CodeInput.text, boxedParams.wordStart, boxedParams.wordEnd);
	}

	private static string GetSubstringWithComments(string input, int index1, int index2)
	{
		string[] array = input.Split('\n');
		int lineIndexFromCharacterIndex = GetLineIndexFromCharacterIndex(array, index1);
		int lineIndexFromCharacterIndex2 = GetLineIndexFromCharacterIndex(array, index2);
		int num = lineIndexFromCharacterIndex;
		while (num > 0 && array[num - 1].StartsWith('#'))
		{
			num--;
		}
		int num2 = lineIndexFromCharacterIndex2;
		return string.Join(Environment.NewLine, array[num..(num2 + 1)]);
	}

	private static int GetLineIndexFromCharacterIndex(string[] lines, int characterIndex)
	{
		int num = 0;
		for (int i = 0; i < lines.Length; i++)
		{
			num += lines[i].Length + 1;
			if (num > characterIndex)
			{
				return i;
			}
		}
		return -1;
	}

	public override Node DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.TryGetValue(this, out var value))
		{
			return (Node)value;
		}
		copies[this] = new FunctionNode(paramNames, funcName, global: false, boxedParams)
		{
			slots = Enumerable.ToList<Node>(Enumerable.Select<Node, Node>((IEnumerable<Node>)slots, (Func<Node, Node>)((Node s) => s.DeepCopy(copies)))),
			Vars = Vars
		};
		return (Node)copies[this];
	}
}
