using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class ProgramState
{
	public ModuleState moduleState;

	public int awaitedDroneId = -1;

	public SideEffect currentSideEffect;

	public IPyObject currentSideEffectArgument;

	public object currentSideEffectArgument2;

	public bool hitBreakpoint;

	public bool hitStoppingPoint;

	public ExecuteException currentExecuteException;

	public List<(string, int, int)> currentDependencies = new List<(string, int, int)>();

	public System.Random randomRandom;

	private IEnumerator<double>[] executionStack = new IEnumerator<double>[1101];

	private int executionStackIndex = -1;

	public Scope CurrentScope
	{
		get
		{
			if (moduleState.callStack.Count <= 0)
			{
				return moduleState.globalScope;
			}
			return moduleState.callStack.Peek();
		}
	}

	public IPyObject ReturnValue
	{
		get
		{
			return moduleState.returnValue;
		}
		set
		{
			moduleState.returnValue = value;
		}
	}

	public bool IsExpressionStatic
	{
		get
		{
			return moduleState.isExpressionStatic;
		}
		set
		{
			moduleState.isExpressionStatic = value;
		}
	}

	public Node CurrentExecutingNode
	{
		get
		{
			return moduleState.currentExecutingNode;
		}
		set
		{
			moduleState.currentExecutingNode = value;
		}
	}

	public Deque<(IPyObject data, int droneId)> AllMessagesQueue { get; } = new Deque<(IPyObject, int)>();

	public Deque<IPyObject>[] MessageQueues { get; } = new Deque<IPyObject>[36];

	public Dictionary<string, PyModule> ModuleCache { get; set; } = new Dictionary<string, PyModule>();

	public double OpCount { get; set; }

	public double StartOpCount { get; private set; }

	private double LastConsumedOpCount { get; set; }

	public int DroneId { get; set; } = -1;

	public PyDroneHandle DroneHandle { get; set; }

	public double TargetOpCount { get; set; }

	public ProgramState(double opCount, System.Random random, int droneId)
	{
		moduleState = new ModuleState();
		moduleState.globalScope = new Scope(null, null, null, new HashSet<string>());
		moduleState.globalScope.ImportVar("__name__", new PyString("__main__"));
		StartOpCount = opCount;
		LastConsumedOpCount = opCount;
		OpCount = opCount;
		DroneId = droneId;
		for (int i = 0; i < MessageQueues.Length; i++)
		{
			MessageQueues[i] = new Deque<IPyObject>();
		}
		randomRandom = new System.Random(random.Next());
	}

	public void PushScope(Scope scope)
	{
		moduleState.callStack.Push(scope);
	}

	public void PopScope()
	{
		moduleState.callStack.Pop();
	}

	public void PerformExecutionStep(double targetOpCount, out bool activeDroneExecutedStep)
	{
		activeDroneExecutedStep = false;
		TargetOpCount = targetOpCount;
		if (executionStackIndex < 0)
		{
			currentSideEffect = SideEffect.Terminated;
			return;
		}
		if (awaitedDroneId >= 0)
		{
			OpCount += 1.0;
			LastConsumedOpCount += 1.0;
			return;
		}
		try
		{
			int num = 0;
			while (true)
			{
				if (executionStackIndex >= 0 && !executionStack[executionStackIndex].MoveNext())
				{
					executionStack[executionStackIndex] = null;
					executionStackIndex--;
					continue;
				}
				num++;
				if (executionStackIndex < 0 || !(OpCount <= targetOpCount) || currentSideEffect != 0 || hitBreakpoint || hitStoppingPoint || num >= 1000)
				{
					break;
				}
			}
			if (hitStoppingPoint || hitBreakpoint)
			{
				hitStoppingPoint = false;
				activeDroneExecutedStep = true;
			}
		}
		catch (Exception ex)
		{
			if (ex is BreakStatement)
			{
				currentExecuteException = new ExecuteException("error_no_loop_to_break");
			}
			else if (ex is ContinueStatement)
			{
				currentExecuteException = new ExecuteException("error_no_loop_to_continue");
			}
			else if (ex is ReturnStatement)
			{
				currentExecuteException = new ExecuteException("error_no_function_to_return_from");
			}
			else if (ex is ExecuteException)
			{
				currentExecuteException = ex as ExecuteException;
			}
			else
			{
				Debug.LogError($"Unexpected exception during execution: {ex}");
			}
			currentSideEffect = SideEffect.Error;
		}
	}

	public void AddAndConsumeOps(double ops)
	{
		OpCount += ops;
		LastConsumedOpCount += ops;
	}

	public double ConsumeOps()
	{
		double result = OpCount - LastConsumedOpCount;
		LastConsumedOpCount = OpCount;
		return result;
	}

	public void PushOntoExecutionStack(IEnumerable<double> exec)
	{
		if (executionStackIndex >= executionStack.Length - 1)
		{
			Achievements.UnlockAchievement("STACK_OVERFLOW");
			throw new ExecuteException("error_max_stack_size_reached");
		}
		executionStackIndex++;
		executionStack[executionStackIndex] = exec.GetEnumerator();
	}

	public bool EvaluateVarAlongCallstack(string varName, out IPyObject value)
	{
		try
		{
			foreach (Scope item in Enumerable.Concat<Scope>((IEnumerable<Scope>)moduleState.callStack, (IEnumerable<Scope>)new Scope[1] { moduleState.globalScope }))
			{
				if (item.HasVar(varName))
				{
					value = item.Evaluate(varName, "").val;
					return true;
				}
			}
		}
		catch (Exception)
		{
		}
		value = null;
		return false;
	}

	public string GetTrace()
	{
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = true;
		int num = 0;
		foreach (Scope item in Enumerable.Reverse<Scope>((IEnumerable<Scope>)moduleState.callStack))
		{
			if (flag)
			{
				flag = false;
			}
			else
			{
				stringBuilder.Append(" <- ");
			}
			stringBuilder.Append(item.functionNode.FuncName);
			num++;
			if (num >= 5)
			{
				stringBuilder.Append(" <- ...");
				break;
			}
		}
		stringBuilder.Append('\n');
		AppendLineWithIndicatorAtIndex(moduleState.currentExecutingNode.boxedParams.codeWindow.CodeInput.text, moduleState.currentExecutingNode.boxedParams.wordStart, moduleState.currentExecutingNode.boxedParams.wordEnd, stringBuilder);
		return stringBuilder.ToString();
	}

	public List<(FunctionNode func, Node callNode)> GetCallStack()
	{
		return Enumerable.ToList<(FunctionNode, Node)>(Enumerable.Select<Scope, (FunctionNode, Node)>((IEnumerable<Scope>)moduleState.callStack, (Func<Scope, (FunctionNode, Node)>)((Scope scope) => (functionNode: scope.functionNode, callNode: scope.callNode))));
	}

	public static void AppendLineWithIndicatorAtIndex(string text, int index1, int index2, StringBuilder output)
	{
		int num = text.LastIndexOf('\n', index1) + 1;
		int num2 = ((index1 < text.Length - 1) ? text.IndexOf('\n', index1 + 1) : (-1));
		if (num2 < 0)
		{
			num2 = text.Length;
		}
		string text2 = text.Substring(num, num2 - num);
		int num3 = text2.Length - text2.TrimStart(' ', '\t').Length;
		text2 = text2.TrimStart(' ', '\t');
		int num4 = Math.Min(index1, index2) - num - num3;
		int num5 = Math.Max(index1, index2) - num - num3;
		output.AppendLine(text2);
		for (int i = 0; i < num4; i++)
		{
			output.Append(' ');
		}
		for (int j = num4; j <= num5; j++)
		{
			output.Append('^');
		}
		for (int k = num5 + 1; k < text2.Length; k++)
		{
			output.Append(' ');
		}
	}
}
