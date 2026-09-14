using System;
using System.Collections.Generic;
using System.Linq;

public class DictNode : Node
{
	public override string NodeName => "dict";

	public DictNode(CodeWindow func, int startIndex, int endIndex)
		: base(func, startIndex, endIndex)
	{
	}

	public DictNode(BoxedNodeParams boxedParams)
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
		Dictionary<IPyObject, PyObjectBox> dictionary = new Dictionary<IPyObject, PyObjectBox>();
		for (int i = 0; i < elements.Count; i += 2)
		{
			IPyObject pyObject = elements[i];
			if (pyObject is PyList || pyObject is PyDict || pyObject is PySet || pyObject is PyModule)
			{
				state.CurrentExecutingNode = this;
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_bad_key", elements[i]));
			}
			dictionary[elements[i]] = new PyObjectBox(elements[i + 1]);
		}
		state.ReturnValue = new PyDict(dictionary);
		state.IsExpressionStatic = false;
		Blink(state, execution);
		if (CheckIncrementOpCount(state, execution, 1 + dictionary.Count))
		{
			yield return 0.0;
		}
	}

	public override void CheckDependencies(ProgramState state, Execution execution)
	{
		state.currentDependencies.Add(("dicts", boxedParams.wordStart, boxedParams.wordEnd));
	}

	public override Node DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.TryGetValue(this, out var value))
		{
			return (Node)value;
		}
		copies[this] = new DictNode(boxedParams)
		{
			slots = Enumerable.ToList<Node>(Enumerable.Select<Node, Node>((IEnumerable<Node>)slots, (Func<Node, Node>)((Node s) => s.DeepCopy(copies))))
		};
		return (Node)copies[this];
	}
}
