using System;
using System.Collections.Generic;
using System.Linq;

public class ForNode : Node
{
	public Node pattern;

	public override string NodeName => "for";

	public ForNode(Node pattern, CodeWindow func, int startIndex, int endIndex)
		: base(func, startIndex, endIndex)
	{
		this.pattern = pattern;
	}

	public ForNode(Node pattern, BoxedNodeParams boxedParams)
		: base(boxedParams)
	{
		this.pattern = pattern;
	}

	public override IEnumerable<double> Execute(ProgramState state, Execution execution, int depth)
	{
		ErrorsAndBreakpoints(state, execution, depth);
		foreach (double item in slots[0].Execute(state, execution, depth + 1))
		{
			yield return item;
		}
		if (state.ReturnValue is IEnumerable<IPyObject>)
		{
			IEnumerable<IPyObject> enumerable = (IEnumerable<IPyObject>)state.ReturnValue;
			IEnumerator<IPyObject> enumerator2 = enumerable.GetEnumerator();
			(enumerator2 as PyCollectionEnumerator)?.SetErrorIndices(boxedParams.wordStart, boxedParams.wordEnd);
			if (CheckIncrementOpCount(state, execution, 1.0))
			{
				yield return 0.0;
			}
			while (enumerator2.MoveNext())
			{
				IPyObject current = enumerator2.Current;
				foreach (double item2 in AssignmentNode.Assign(state, execution, depth, pattern, current, "=", boxedParams.codeWindow.fileName))
				{
					yield return item2;
				}
				Blink(state, execution);
				IEnumerator<double> enumerator4 = slots[1].Execute(state, execution, depth + 1).GetEnumerator();
				while (true)
				{
					double current2;
					try
					{
						if (!enumerator4.MoveNext())
						{
							break;
						}
						current2 = enumerator4.Current;
						goto IL_02a9;
					}
					catch (BreakStatement)
					{
						yield break;
					}
					catch (ContinueStatement)
					{
					}
					break;
					IL_02a9:
					yield return current2;
				}
				ErrorsAndBreakpoints(state, execution, depth);
			}
			yield break;
		}
		state.CurrentExecutingNode = this;
		throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_for_requires_iterable", state.ReturnValue));
	}

	public override void CheckDependencies(ProgramState state, Execution execution)
	{
		state.currentDependencies.Add(("for", boxedParams.wordStart, boxedParams.wordEnd));
	}

	public override Node DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.TryGetValue(this, out var value))
		{
			return (Node)value;
		}
		copies[this] = new ForNode(pattern.DeepCopy(copies), boxedParams)
		{
			slots = Enumerable.ToList<Node>(Enumerable.Select<Node, Node>((IEnumerable<Node>)slots, (Func<Node, Node>)((Node s) => s.DeepCopy(copies))))
		};
		return (Node)copies[this];
	}
}
