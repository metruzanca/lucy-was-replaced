using System;
using System.Collections.Generic;
using System.Linq;

public class CallNode : Node
{
	public override string NodeName => "call";

	public CallNode(CodeWindow func, int startIndex, int endIndex)
		: base(func, startIndex, endIndex)
	{
	}

	public CallNode(BoxedNodeParams boxedParams)
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
		IPyObject returnValue = state.ReturnValue;
		if (!(returnValue is PyFunction))
		{
			state.CurrentExecutingNode = this;
			throw new ExecuteException(Localizer.Localize("error_not_a_function"));
		}
		PyFunction func = (PyFunction)returnValue;
		bool isStatic = state.IsExpressionStatic;
		foreach (double item2 in slots[1].Execute(state, execution, depth + 1))
		{
			yield return item2;
		}
		List<IPyObject> parameters = ((InternalPySequence)state.ReturnValue).elements;
		Blink(state, execution);
		if (func.methodObject != null)
		{
			parameters.Insert(0, func.methodObject);
		}
		if (execution.sim.leaderboardType == LeaderboardType.none && Enumerable.Any<IPyObject>((IEnumerable<IPyObject>)parameters, (Func<IPyObject, bool>)((IPyObject p) => p is PyFunction)))
		{
			Achievements.UnlockAchievement("HIGHER_ORDER_PROGRAMMING");
		}
		state.CurrentExecutingNode = this;
		if (func.syntaxTree != null)
		{
			if (CheckIncrementOpCount(state, execution, (!isStatic) ? 1 : 0))
			{
				yield return 0.0;
			}
			FunctionNode functionNode = (FunctionNode)func.syntaxTree;
			functionNode.Arguments = parameters;
			state.PushScope(new Scope(functionNode, state.CurrentExecutingNode, func.parentScope, functionNode.Vars));
			state.PushOntoExecutionStack(func.syntaxTree.Execute(state, execution, 0));
			yield return 0.0;
			yield break;
		}
		state.currentDependencies.Add((func.functionName, boxedParams.wordStart, boxedParams.wordEnd));
		if (func.binding == null)
		{
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_name_not_defined", func.functionName));
		}
		double ops = func.binding(parameters, execution.sim, execution, state.DroneId);
		if (CheckIncrementOpCount(state, execution, ops))
		{
			yield return 0.0;
		}
	}

	public override Node DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.TryGetValue(this, out var value))
		{
			return (Node)value;
		}
		copies[this] = new CallNode(boxedParams)
		{
			slots = Enumerable.ToList<Node>(Enumerable.Select<Node, Node>((IEnumerable<Node>)slots, (Func<Node, Node>)((Node s) => s.DeepCopy(copies))))
		};
		return (Node)copies[this];
	}
}
