using System.Collections.Generic;

public class NoOpNode : Node
{
	public override string NodeName => "noop";

	public NoOpNode(CodeWindow func, int startIndex, int endIndex)
		: base(func, startIndex, endIndex)
	{
	}

	public override Node DeepCopy(Dictionary<object, object> copies)
	{
		return this;
	}

	public override IEnumerable<double> Execute(ProgramState state, Execution execution, int depth)
	{
		yield break;
	}
}
