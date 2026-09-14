using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BinaryExprNode : Node
{
	public string op;

	public TokenType opTokenType;

	public override string NodeName => "binary";

	public BinaryExprNode(string op, TokenType opTokenType, CodeWindow func, int startIndex, int endIndex)
		: base(func, startIndex, endIndex)
	{
		this.op = op;
		this.opTokenType = opTokenType;
	}

	public BinaryExprNode(string op, TokenType opTokenType, BoxedNodeParams boxedParams)
		: base(boxedParams)
	{
		this.op = op;
		this.opTokenType = opTokenType;
	}

	public override IEnumerable<double> Execute(ProgramState state, Execution execution, int depth)
	{
		ErrorsAndBreakpoints(state, execution, depth);
		foreach (double item in slots[0].Execute(state, execution, depth + 1))
		{
			yield return item;
		}
		IPyObject lhs = state.ReturnValue;
		if (op == "and" && !Scope.IsTrueValue(lhs))
		{
			Blink(state, execution);
			state.ReturnValue = lhs;
			if (CheckIncrementOpCount(state, execution, 1.0))
			{
				yield return 0.0;
			}
			yield break;
		}
		if (op == "or" && Scope.IsTrueValue(lhs))
		{
			Blink(state, execution);
			state.ReturnValue = lhs;
			if (CheckIncrementOpCount(state, execution, 1.0))
			{
				yield return 0.0;
			}
			yield break;
		}
		if (op == "." && lhs is IPyNameSpace nameSpace)
		{
			if (!(slots[1] is ValueNode))
			{
				throw new ExecuteException("error_invalid_const2", slots[1].boxedParams.wordStart, slots[1].boxedParams.wordEnd);
			}
			Blink(state, execution);
			yield return (!state.IsExpressionStatic) ? 1 : 0;
			bool flag;
			(state.ReturnValue, flag) = nameSpace.Evaluate(((ValueNode)slots[1]).value, slots[1].boxedParams.wordStart, slots[1].boxedParams.wordEnd);
			state.IsExpressionStatic &= flag;
			yield break;
		}
		foreach (double item2 in slots[1].Execute(state, execution, depth + 1))
		{
			yield return item2;
		}
		IPyObject returnValue = state.ReturnValue;
		Blink(state, execution);
		state.CurrentExecutingNode = this;
		if ((lhs is PyFunction || returnValue is PyFunction) && ((op != "and" && op != "or" && op != "." && op != "==" && op != "!=" && op != "in" && op != "not in") || ((op == "==" || op == "!=" || op == "in" || op == "not in") && OptionHolder.GetString("error forgot call") == "enabled")))
		{
			PyFunction pyFunction = (PyFunction)((lhs is PyFunction) ? lhs : returnValue);
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_function_in_operator", pyFunction.functionName, op));
		}
		if (!(op == "==") && !(op == "!=") && !(op == ">=") && !(op == "<=") && !(op == ">") && !(op == "<") && !(op == "and") && !(op == "or"))
		{
			if (op == "[]")
			{
				if (!(lhs is IPyIndexable))
				{
					throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_index_on_non_indexable", lhs));
				}
			}
			else if (op == "+")
			{
				if ((!(lhs is PyNumber) || !(returnValue is PyNumber)) && (!(lhs is PyList) || !(returnValue is IPySequence)) && (!(lhs is PyTuple) || !(returnValue is PyTuple)) && (!(lhs is PyString) || !(returnValue is PyString)))
				{
					throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_bad_bin_operator", op, lhs, returnValue));
				}
			}
			else
			{
				if ((op == "/" || op == "//" || op == "%") && returnValue is PyNumber pyNumber && (double)pyNumber == 0.0)
				{
					throw new ExecuteException("error_division_by_zero");
				}
				if (op == "in" || op == "not in")
				{
					if (!(returnValue is PyDict) && !(returnValue is PySet) && !(returnValue is IPySequence))
					{
						throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_bad_unary_operator", op, returnValue));
					}
				}
				else if (op == ".")
				{
					if (!(returnValue is PyFunction))
					{
						throw new ExecuteException("error_unknown_method");
					}
				}
				else if (!(lhs is PyNumber) || !(returnValue is PyNumber))
				{
					throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_bad_bin_operator", op, lhs, returnValue));
				}
			}
		}
		int steps = 0;
		switch (op)
		{
		case "+":
			if (lhs is PyNumber)
			{
				state.ReturnValue = new PyNumber((double)(PyNumber)lhs + (double)(PyNumber)returnValue);
				steps = 1;
			}
			else if (lhs is PyList)
			{
				state.ReturnValue = new PyList(Enumerable.ToList<IPyObject>(Enumerable.Concat<IPyObject>((IEnumerable<IPyObject>)(PyList)lhs, (IEnumerable<IPyObject>)(IPySequence)returnValue)));
				steps = Mathf.Max(1, ((PyList)lhs).Count + ((IPySequence)returnValue).Count);
			}
			else if (lhs is PyString)
			{
				state.ReturnValue = new PyString(((PyString)lhs).str + ((PyString)returnValue).str);
				steps = Mathf.Max(1, ((PyString)lhs).Count + ((PyString)returnValue).Count);
			}
			else if (lhs is PyTuple)
			{
				state.ReturnValue = new PyTuple(Enumerable.ToList<IPyObject>(Enumerable.Concat<IPyObject>((IEnumerable<IPyObject>)(PyTuple)lhs, (IEnumerable<IPyObject>)(PyTuple)returnValue)));
				steps = Mathf.Max(1, ((PyTuple)lhs).Count + ((PyTuple)returnValue).Count);
			}
			break;
		case "-":
			state.ReturnValue = new PyNumber((double)(PyNumber)lhs - (double)(PyNumber)returnValue);
			steps = 1;
			break;
		case "*":
			state.ReturnValue = new PyNumber((double)(PyNumber)lhs * (double)(PyNumber)returnValue);
			steps = 1;
			break;
		case "/":
			state.ReturnValue = new PyNumber((double)(PyNumber)lhs / (double)(PyNumber)returnValue);
			steps = 1;
			break;
		case "//":
			state.ReturnValue = PyNumber.FloorDivision((PyNumber)lhs, (PyNumber)returnValue);
			steps = 1;
			break;
		case "%":
			state.ReturnValue = PyNumber.Modulo((PyNumber)lhs, (PyNumber)returnValue);
			steps = 1;
			break;
		case "**":
			state.ReturnValue = new PyNumber(Math.Pow((PyNumber)lhs, (PyNumber)returnValue));
			steps = 1;
			break;
		case "==":
			state.ReturnValue = new PyBool(CodeUtilities.DeepEquals(lhs, returnValue, ref steps));
			break;
		case "!=":
			state.ReturnValue = new PyBool(!CodeUtilities.DeepEquals(lhs, returnValue, ref steps));
			break;
		case ">=":
			state.ReturnValue = new PyBool(CodeUtilities.DeepCompare(lhs, returnValue, op, ref steps) >= 0);
			break;
		case "<=":
			state.ReturnValue = new PyBool(CodeUtilities.DeepCompare(lhs, returnValue, op, ref steps) <= 0);
			break;
		case ">":
			state.ReturnValue = new PyBool(CodeUtilities.DeepCompare(lhs, returnValue, op, ref steps) > 0);
			break;
		case "<":
			state.ReturnValue = new PyBool(CodeUtilities.DeepCompare(lhs, returnValue, op, ref steps) < 0);
			break;
		case "and":
			state.ReturnValue = returnValue;
			steps = 1;
			break;
		case "or":
			state.ReturnValue = returnValue;
			steps = 1;
			break;
		case "[]":
			state.ReturnValue = ((IPyIndexable)lhs).At((InternalPySequence)returnValue, ref steps);
			break;
		case "in":
			if (returnValue is IPySequence)
			{
				IPySequence pySequence2 = (IPySequence)returnValue;
				state.ReturnValue = new PyBool(pySequence2.Contains(lhs, ref steps));
			}
			else if (returnValue is PyDict)
			{
				PyDict pyDict2 = (PyDict)returnValue;
				state.ReturnValue = new PyBool(lhs != null && pyDict2.dict.ContainsKey(lhs));
				steps = lhs.Size();
			}
			else if (returnValue is PySet)
			{
				PySet pySet2 = (PySet)returnValue;
				state.ReturnValue = new PyBool(pySet2.Contains(lhs));
				steps = lhs.Size();
			}
			break;
		case "not in":
			if (returnValue is IPySequence)
			{
				IPySequence pySequence = (IPySequence)returnValue;
				state.ReturnValue = new PyBool(!pySequence.Contains(lhs, ref steps));
			}
			else if (returnValue is PyDict)
			{
				PyDict pyDict = (PyDict)returnValue;
				state.ReturnValue = new PyBool(lhs == null || !pyDict.dict.ContainsKey(lhs));
				steps = lhs.Size();
			}
			else if (returnValue is PySet)
			{
				PySet pySet = (PySet)returnValue;
				state.ReturnValue = new PyBool(!pySet.Contains(lhs));
				steps = lhs.Size();
			}
			break;
		case ".":
		{
			PyFunction pyFunction2 = (PyFunction)returnValue;
			state.ReturnValue = new PyFunction(pyFunction2.functionName, pyFunction2.syntaxTree, pyFunction2.parentScope, pyFunction2.binding, lhs, pyFunction2.isFree);
			steps = 0;
			break;
		}
		}
		if (CheckIncrementOpCount(state, execution, steps))
		{
			yield return 0.0;
		}
	}

	public override void CheckDependencies(ProgramState state, Execution execution)
	{
		if (op != ".")
		{
			state.currentDependencies.Add(("operators", boxedParams.wordStart, boxedParams.wordEnd));
		}
	}

	public override Node DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.TryGetValue(this, out var value))
		{
			return (Node)value;
		}
		copies[this] = new BinaryExprNode(op, opTokenType, boxedParams)
		{
			slots = Enumerable.ToList<Node>(Enumerable.Select<Node, Node>((IEnumerable<Node>)slots, (Func<Node, Node>)((Node s) => s.DeepCopy(copies))))
		};
		return (Node)copies[this];
	}

	public override void CheckForNonsensicalCode()
	{
		base.CheckForNonsensicalCode();
		CheckTautologicalOrExpressions();
	}

	private void CheckTautologicalOrExpressions()
	{
		if (op == "or" && slots[0] is ComparisonNode && slots[1] is LiteralNode literalNode)
		{
			throw new ParseException(CodeUtilities.LocalizeAndFormat("error_nonsensical_or", literalNode.value), boxedParams.wordStart, boxedParams.wordEnd);
		}
	}
}
