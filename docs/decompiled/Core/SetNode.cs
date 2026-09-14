using System;
using System.Collections.Generic;
using System.Linq;

public class SetNode : Node
{
	public override string NodeName => "set";

	public SetNode(CodeWindow func, int startIndex, int endIndex)
		: base(func, startIndex, endIndex)
	{
	}

	public SetNode(BoxedNodeParams boxedParams)
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
		List<IPyObject> elements = ((InternalPySequence)state.ReturnValue).elements;
		HashSet<IPyObject> hashSet = new HashSet<IPyObject>();
		for (int i = 0; i < elements.Count; i++)
		{
			IPyObject pyObject = elements[i];
			if (pyObject is PyList || pyObject is PyDict || pyObject is PySet || pyObject is PyModule)
			{
				state.CurrentExecutingNode = this;
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_bad_key", elements[i]));
			}
			hashSet.Add(elements[i]);
		}
		state.ReturnValue = new PySet(hashSet);
		state.IsExpressionStatic = false;
		Blink(state, execution);
		if (CheckIncrementOpCount(state, execution, Math.Max(1, state.ReturnValue.Size())))
		{
			yield return 0.0;
		}
	}

	public override void CheckDependencies(ProgramState state, Execution execution)
	{
		state.currentDependencies.Add(("sets", boxedParams.wordStart, boxedParams.wordEnd));
	}

	public override Node DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.TryGetValue(this, out var value))
		{
			return (Node)value;
		}
		copies[this] = new SetNode(boxedParams)
		{
			slots = Enumerable.ToList<Node>(Enumerable.Select<Node, Node>((IEnumerable<Node>)slots, (Func<Node, Node>)((Node s) => s.DeepCopy(copies))))
		};
		return (Node)copies[this];
	}
}
