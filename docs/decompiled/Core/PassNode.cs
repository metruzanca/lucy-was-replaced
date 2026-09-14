using System.Collections.Generic;

public class PassNode : Node
{
	public override string NodeName => "pass";

	public PassNode(CodeWindow func, int startIndex, int endIndex)
		: base(func, startIndex, endIndex)
	{
	}

	public PassNode(BoxedNodeParams boxedParams)
		: base(boxedParams)
	{
	}

	public override IEnumerable<double> Execute(ProgramState state, Execution execution, int depth)
	{
		ErrorsAndBreakpoints(state, execution, depth);
		Blink(state, execution);
		if (CheckIncrementOpCount(state, execution, 1.0))
		{
			yield return 0.0;
		}
	}

	public override Node DeepCopy(Dictionary<object, object> copies)
	{
		return new PassNode(boxedParams);
	}
}
