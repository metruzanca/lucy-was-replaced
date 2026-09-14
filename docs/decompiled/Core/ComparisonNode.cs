using System;
using System.Collections.Generic;
using System.Linq;

public class ComparisonNode : Node
{
	public List<string> ops;

	public override string NodeName => "comparison";

	public ComparisonNode(List<string> ops, CodeWindow func, int startIndex, int endIndex)
		: base(func, startIndex, endIndex)
	{
		this.ops = ops;
	}

	public ComparisonNode(List<string> ops, BoxedNodeParams boxedParams)
		: base(boxedParams)
	{
		this.ops = ops;
	}

	public override IEnumerable<double> Execute(ProgramState state, Execution execution, int depth)
	{
		ErrorsAndBreakpoints(state, execution, depth);
		List<IPyObject> operands = new List<IPyObject>();
		foreach (Node slot in slots)
		{
			foreach (double item in slot.Execute(state, execution, depth + 1))
			{
				yield return item;
			}
			operands.Add(state.ReturnValue);
		}
		Blink(state, execution);
		state.CurrentExecutingNode = this;
		int steps = 0;
		bool flag = true;
		for (int i = 0; i < ops.Count; i++)
		{
			string text = ops[i];
			IPyObject pyObject = operands[i];
			IPyObject pyObject2 = operands[i + 1];
			if ((pyObject is PyFunction || pyObject2 is PyFunction) && ((text != "==" && text != "!=" && text != "in" && text != "not in") || OptionHolder.GetString("error forgot call") == "enabled"))
			{
				PyFunction pyFunction = (PyFunction)((pyObject is PyFunction) ? pyObject : pyObject2);
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_function_in_operator", pyFunction.functionName, text));
			}
			switch (text)
			{
			case "in":
			case "not in":
				if (!(pyObject2 is PyDict) && !(pyObject2 is PySet) && !(pyObject2 is IPySequence))
				{
					throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_bad_unary_operator", text, pyObject2));
				}
				break;
			default:
				if (!(pyObject is PyNumber) || !(pyObject2 is PyNumber))
				{
					throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_bad_bin_operator", text, pyObject, pyObject2));
				}
				break;
			case "==":
			case "!=":
			case ">=":
			case "<=":
			case ">":
			case "<":
				break;
			}
			bool flag2 = true;
			switch (text)
			{
			case "==":
				flag2 = CodeUtilities.DeepEquals(pyObject, pyObject2, ref steps);
				break;
			case "!=":
				flag2 = !CodeUtilities.DeepEquals(pyObject, pyObject2, ref steps);
				break;
			case ">=":
				flag2 = CodeUtilities.DeepCompare(pyObject, pyObject2, text, ref steps) >= 0;
				break;
			case "<=":
				flag2 = CodeUtilities.DeepCompare(pyObject, pyObject2, text, ref steps) <= 0;
				break;
			case ">":
				flag2 = CodeUtilities.DeepCompare(pyObject, pyObject2, text, ref steps) > 0;
				break;
			case "<":
				flag2 = CodeUtilities.DeepCompare(pyObject, pyObject2, text, ref steps) < 0;
				break;
			case "in":
				if (pyObject2 is IPySequence)
				{
					flag2 = ((IPySequence)pyObject2).Contains(pyObject, ref steps);
				}
				else if (pyObject2 is PyDict)
				{
					PyDict pyDict2 = (PyDict)pyObject2;
					flag2 = pyObject != null && pyDict2.dict.ContainsKey(pyObject);
					steps += pyObject.Size();
				}
				else if (pyObject2 is PySet)
				{
					flag2 = ((PySet)pyObject2).Contains(pyObject);
					steps += pyObject.Size();
				}
				break;
			case "not in":
				if (pyObject2 is IPySequence)
				{
					flag2 = !((IPySequence)pyObject2).Contains(pyObject, ref steps);
				}
				else if (pyObject2 is PyDict)
				{
					PyDict pyDict = (PyDict)pyObject2;
					flag2 = pyObject == null || !pyDict.dict.ContainsKey(pyObject);
					steps += pyObject.Size();
				}
				else if (pyObject2 is PySet)
				{
					flag2 = !((PySet)pyObject2).Contains(pyObject);
					steps += pyObject.Size();
				}
				break;
			}
			flag = flag && flag2;
			if (!flag)
			{
				break;
			}
		}
		state.ReturnValue = new PyBool(flag);
		if (CheckIncrementOpCount(state, execution, steps))
		{
			yield return 0.0;
		}
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
		copies[this] = new ComparisonNode(ops, boxedParams)
		{
			slots = Enumerable.ToList<Node>(Enumerable.Select<Node, Node>((IEnumerable<Node>)slots, (Func<Node, Node>)((Node s) => s.DeepCopy(copies))))
		};
		return (Node)copies[this];
	}

	public override void CheckForNonsensicalCode()
	{
		base.CheckForNonsensicalCode();
		CheckFarmLiteralsComparedWithNumbers();
	}

	private void CheckFarmLiteralsComparedWithNumbers()
	{
		for (int i = 0; i < ops.Count; i++)
		{
			string text = ops[i];
			Node node = slots[i];
			Node node2 = slots[i + 1];
			LiteralNode literalNode2;
			Node node3;
			if (node is LiteralNode literalNode && IsFarmLiteral(literalNode.value))
			{
				literalNode2 = literalNode;
				node3 = node2;
			}
			else
			{
				if (!(node2 is LiteralNode literalNode3) || !IsFarmLiteral(literalNode3.value))
				{
					break;
				}
				literalNode2 = literalNode3;
				node3 = node;
			}
			switch (text)
			{
			case "==":
			case "!=":
			case "in":
			case "not in":
				if (!(node3 is LiteralNode literalNode4) || !(literalNode4.value is PyNumber))
				{
					return;
				}
				break;
			}
			if (literalNode2.value is ItemSO)
			{
				throw new ParseException(CodeUtilities.LocalizeAndFormat("error_compared_item_with_number", literalNode2.value), boxedParams.wordStart, boxedParams.wordEnd);
			}
			if (literalNode2.value is FarmObjectSO)
			{
				throw new ParseException(CodeUtilities.LocalizeAndFormat("error_compared_entity_with_number", literalNode2.value), boxedParams.wordStart, boxedParams.wordEnd);
			}
			if (literalNode2.value is UnlockSO)
			{
				throw new ParseException(CodeUtilities.LocalizeAndFormat("error_compared_unlock_with_number", literalNode2.value), boxedParams.wordStart, boxedParams.wordEnd);
			}
		}
	}

	private bool IsFarmLiteral(IPyObject obj)
	{
		if (!(obj is ItemSO) && !(obj is FarmObjectSO))
		{
			return obj is UnlockSO;
		}
		return true;
	}
}
