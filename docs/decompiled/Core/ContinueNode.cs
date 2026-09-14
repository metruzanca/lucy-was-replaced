using System.Collections.Generic;

public class ContinueNode : Node
{
	public override string NodeName => "continue";

	public ContinueNode(CodeWindow func, int startIndex, int endIndex)
		: base(func, startIndex, endIndex)
	{
	}

	public ContinueNode(BoxedNodeParams boxedParams)
		: base(boxedParams)
	{
	}

	public override IEnumerable<double> Execute(ProgramState state, Execution execution, int depth)
	{
		ErrorsAndBreakpoints(state, execution, depth);
		Blink(state, execution);
		if (CheckIncrementOpCount(state, execution, 0.0))
		{
			yield return 0.0;
		}
		throw new ContinueStatement(boxedParams.wordStart, boxedParams.wordEnd);
	}

	public override Node DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.TryGetValue(this, out var value))
		{
			return (Node)value;
		}
		copies[this] = new ContinueNode(boxedParams);
		return (Node)copies[this];
	}
}
