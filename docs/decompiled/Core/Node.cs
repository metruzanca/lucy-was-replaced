using System.Collections.Generic;
using System.Linq;

public abstract class Node
{
	public List<Node> slots = new List<Node>();

	public BoxedNodeParams boxedParams;

	public const int TICKS_PER_OP = 1;

	public virtual string NodeName => "";

	public bool blink => boxedParams.wordEnd > boxedParams.wordStart;

	public Node(CodeWindow codeWindow, int startIndex, int endIndex)
	{
		boxedParams = new BoxedNodeParams();
		boxedParams.codeWindow = codeWindow;
		boxedParams.wordStart = startIndex;
		boxedParams.wordEnd = endIndex;
	}

	public Node(BoxedNodeParams boxedParams)
	{
		this.boxedParams = boxedParams;
	}

	public abstract IEnumerable<double> Execute(ProgramState state, Execution execution, int depth);

	protected void ErrorsAndBreakpoints(ProgramState state, Execution execution, int depth)
	{
		if (depth > 1100)
		{
			if (execution.sim.leaderboardType == LeaderboardType.none)
			{
				Achievements.UnlockAchievement("STACK_OVERFLOW");
			}
			throw new ExecuteException("error_max_stack_size_reached");
		}
		if (boxedParams.executionId != execution.Id)
		{
			boxedParams.executionId = execution.Id;
			if (OptionHolder.GetString("allow locked features") != "enabled")
			{
				CheckDependencies(state, execution);
			}
		}
		if (boxedParams.isBreakpoint && !execution.sim.stepByStepMode && execution.sim.mainSim != null)
		{
			Blink(state, execution);
			state.hitBreakpoint = true;
		}
		if (execution.sim.stepByStepMode && execution.MainState == state && !IsOnSameLine(state.CurrentExecutingNode))
		{
			Blink(state, execution);
			state.hitStoppingPoint = true;
		}
		state.CurrentExecutingNode = this;
	}

	public bool CheckIncrementOpCount(ProgramState state, Execution execution, double ops)
	{
		state.OpCount += ops;
		if (!(state.OpCount >= state.TargetOpCount) && !state.hitBreakpoint && !state.hitStoppingPoint)
		{
			return state.currentSideEffect != SideEffect.None;
		}
		return true;
	}

	public void Blink(ProgramState state, Execution execution)
	{
		if (state == execution.MainState && execution.sim.mainSim != null)
		{
			execution.sim.mainSim.BlinkEffect(this);
		}
	}

	public virtual void CheckForNonsensicalCode()
	{
		foreach (Node slot in slots)
		{
			slot?.CheckForNonsensicalCode();
		}
	}

	public virtual void CheckDependencies(ProgramState state, Execution execution)
	{
	}

	public void InsertBreakpointsRec(HashSet<int> breakpointCharPositions)
	{
		boxedParams.isBreakpoint = false;
		foreach (int item in Enumerable.ToList<int>((IEnumerable<int>)breakpointCharPositions))
		{
			if (item <= boxedParams.wordStart)
			{
				boxedParams.isBreakpoint = true;
				breakpointCharPositions.Remove(item);
			}
		}
		foreach (Node slot in slots)
		{
			slot?.InsertBreakpointsRec(breakpointCharPositions);
		}
	}

	public abstract Node DeepCopy(Dictionary<object, object> copies);

	public override string ToString()
	{
		string text = NodeName;
		if (slots.Count > 0)
		{
			text += " (";
			foreach (Node slot in slots)
			{
				text += slot;
				text += ", ";
			}
			text += ")";
		}
		return text;
	}

	private bool IsOnSameLine(Node other)
	{
		CodeWindow codeWindow = boxedParams.codeWindow;
		if (other == null || codeWindow != other.boxedParams.codeWindow)
		{
			return false;
		}
		int lineNumber = codeWindow.CodeInput.textComponent.textInfo.characterInfo[boxedParams.wordStart].lineNumber;
		int lineNumber2 = codeWindow.CodeInput.textComponent.textInfo.characterInfo[other.boxedParams.wordStart].lineNumber;
		return lineNumber == lineNumber2;
	}
}
