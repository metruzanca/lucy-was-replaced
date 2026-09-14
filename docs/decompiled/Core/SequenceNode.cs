using System;
using System.Collections.Generic;
using System.Linq;

public class SequenceNode : Node
{
	private InternalPySequence internalPySequence = new InternalPySequence(new List<IPyObject>());

	public override string NodeName => "seq";

	public SequenceNode(CodeWindow func)
		: base(func, 0, 0)
	{
	}

	public SequenceNode(BoxedNodeParams boxedParams)
		: base(boxedParams)
	{
	}

	public override IEnumerable<double> Execute(ProgramState state, Execution execution, int depth)
	{
		InternalPySequence pySequence;
		if (internalPySequence == null)
		{
			pySequence = new InternalPySequence(new List<IPyObject>());
		}
		else
		{
			pySequence = internalPySequence;
			pySequence.elements.Clear();
			internalPySequence = null;
		}
		foreach (Node slot in slots)
		{
			foreach (double item in slot.Execute(state, execution, depth + 1))
			{
				yield return item;
			}
			pySequence.elements.Add(state.ReturnValue);
		}
		state.ReturnValue = pySequence;
		if (internalPySequence == null)
		{
			internalPySequence = pySequence;
		}
	}

	public override Node DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.TryGetValue(this, out var value))
		{
			return (Node)value;
		}
		copies[this] = new SequenceNode(boxedParams)
		{
			slots = Enumerable.ToList<Node>(Enumerable.Select<Node, Node>((IEnumerable<Node>)slots, (Func<Node, Node>)((Node s) => s.DeepCopy(copies))))
		};
		return (Node)copies[this];
	}
}
