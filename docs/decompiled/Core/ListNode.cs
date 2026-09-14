using System;
using System.Collections.Generic;
using System.Linq;

public class ListNode : Node
{
	public override string NodeName => "list";

	public ListNode(CodeWindow func, int startIndex, int endIndex)
		: base(func, startIndex, endIndex)
	{
	}

	public ListNode(BoxedNodeParams boxedParams)
		: base(boxedParams)
	{
	}

	public override IEnumerable<double> Execute(ProgramState state, Execution execution, int depth)
	{
		ErrorsAndBreakpoints(state, execution, depth);
		foreach (double item in slots[0].Execute(state, execution, depth + 1))
		{
			yield return item;
		}
		Blink(state, execution);
		PyList pyList = (PyList)(state.ReturnValue = new PyList(Enumerable.ToList<IPyObject>((IEnumerable<IPyObject>)((InternalPySequence)state.ReturnValue).elements)));
		state.IsExpressionStatic = false;
		if (CheckIncrementOpCount(state, execution, Math.Max(1, pyList.Count)))
		{
			yield return 0.0;
		}
	}

	public override void CheckDependencies(ProgramState state, Execution execution)
	{
		state.currentDependencies.Add(("lists", boxedParams.wordStart, boxedParams.wordEnd));
	}

	public override Node DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.TryGetValue(this, out var value))
		{
			return (Node)value;
		}
		copies[this] = new ListNode(boxedParams)
		{
			slots = Enumerable.ToList<Node>(Enumerable.Select<Node, Node>((IEnumerable<Node>)slots, (Func<Node, Node>)((Node s) => s.DeepCopy(copies))))
		};
		return (Node)copies[this];
	}
}
