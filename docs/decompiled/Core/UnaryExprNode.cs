using System;
using System.Collections.Generic;
using System.Linq;

public class UnaryExprNode : Node
{
	private string op;

	public override string NodeName => "unary";

	public UnaryExprNode(string op, CodeWindow func, int startIndex, int endIndex)
		: base(func, startIndex, endIndex)
	{
		this.op = op;
	}

	public UnaryExprNode(string op, BoxedNodeParams boxedParams)
		: base(boxedParams)
	{
		this.op = op;
	}

	public override IEnumerable<double> Execute(ProgramState state, Execution execution, int depth)
	{
		ErrorsAndBreakpoints(state, execution, depth);
		foreach (double item in slots[0].Execute(state, execution, depth + 1))
		{
			yield return item;
		}
		IPyObject returnValue = state.ReturnValue;
		state.CurrentExecutingNode = this;
		if (returnValue is PyFunction && op != "not")
		{
			PyFunction pyFunction = (PyFunction)returnValue;
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_function_in_operator", pyFunction.functionName, op));
		}
		switch (op)
		{
		case "+":
			if (!(returnValue is PyNumber))
			{
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_bad_unary_operator", op, returnValue));
			}
			state.ReturnValue = returnValue;
			break;
		case "-":
			if (!(returnValue is PyNumber))
			{
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_bad_unary_operator", op, returnValue));
			}
			state.ReturnValue = new PyNumber(0.0 - (double)(PyNumber)returnValue);
			break;
		case "not":
			state.ReturnValue = new PyBool(!Scope.IsTrueValue(returnValue));
			break;
		}
		Blink(state, execution);
	}

	public override void CheckDependencies(ProgramState state, Execution execution)
	{
		state.currentDependencies.Add(("operators", boxedParams.wordStart, boxedParams.wordEnd));
	}

	public override Node DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.TryGetValue(this, out var value))
		{
			return (Node)value;
		}
		copies[this] = new UnaryExprNode(op, boxedParams)
		{
			slots = Enumerable.ToList<Node>(Enumerable.Select<Node, Node>((IEnumerable<Node>)slots, (Func<Node, Node>)((Node s) => s.DeepCopy(copies))))
		};
		return (Node)copies[this];
	}
}
