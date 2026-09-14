using System;
using System.Collections.Generic;
using System.Linq;

public class BranchNode : Node
{
	private bool looping;

	public override string NodeName
	{
		get
		{
			if (!looping)
			{
				return "if";
			}
			return "while";
		}
	}

	public BranchNode(CodeWindow func, int startIndex, int endIndex, bool looping = false)
		: base(func, startIndex, endIndex)
	{
		this.looping = looping;
	}

	public BranchNode(BoxedNodeParams boxedParams, bool looping = false)
		: base(boxedParams)
	{
		this.looping = looping;
	}

	public override IEnumerable<double> Execute(ProgramState state, Execution execution, int depth)
	{
		ErrorsAndBreakpoints(state, execution, depth);
		bool firstLoop = true;
		while (true)
		{
			bool condition;
			if (IsLiteralNode(slots[0], out var literal))
			{
				condition = Scope.IsTrueValue(literal.value);
				if (execution.sim.leaderboardType == LeaderboardType.none)
				{
					Achievements.UnlockAchievement("INFINITE_LOOP");
				}
			}
			else
			{
				foreach (double item in slots[0].Execute(state, execution, depth + 1))
				{
					yield return item;
				}
				condition = Scope.IsTrueValue(state.ReturnValue);
			}
			Blink(state, execution);
			if (firstLoop)
			{
				if (CheckIncrementOpCount(state, execution, 1.0))
				{
					yield return 0.0;
				}
				firstLoop = false;
			}
			if (!condition)
			{
				break;
			}
			IEnumerator<double> enumerator2 = slots[1].Execute(state, execution, depth + 1).GetEnumerator();
			while (true)
			{
				double current;
				try
				{
					if (!enumerator2.MoveNext())
					{
						break;
					}
					current = enumerator2.Current;
					goto IL_0218;
				}
				catch (BreakStatement breakStatement)
				{
					if (!looping)
					{
						throw breakStatement;
					}
					yield break;
				}
				catch (ContinueStatement continueStatement)
				{
					if (!looping)
					{
						throw continueStatement;
					}
				}
				break;
				IL_0218:
				yield return current;
			}
			if (!looping)
			{
				state.ReturnValue = new PyNone();
				yield break;
			}
			ErrorsAndBreakpoints(state, execution, depth);
			yield return 0.0;
		}
		if (slots.Count > 2)
		{
			foreach (double item2 in slots[2].Execute(state, execution, depth + 1))
			{
				yield return item2;
			}
		}
		state.ReturnValue = new PyNone();
	}

	private bool IsLiteralNode(Node node, out LiteralNode literal)
	{
		while (node is BracketNode bracketNode && bracketNode.slots.Count > 0)
		{
			node = bracketNode.slots[0];
		}
		if (node is LiteralNode literalNode)
		{
			literal = literalNode;
			return true;
		}
		literal = null;
		return false;
	}

	public override void CheckDependencies(ProgramState state, Execution execution)
	{
		state.currentDependencies.Add((looping ? "while" : "if", boxedParams.wordStart, boxedParams.wordEnd));
	}

	public override Node DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.TryGetValue(this, out var value))
		{
			return (Node)value;
		}
		copies[this] = new BranchNode(boxedParams, looping)
		{
			slots = Enumerable.ToList<Node>(Enumerable.Select<Node, Node>((IEnumerable<Node>)slots, (Func<Node, Node>)((Node s) => s.DeepCopy(copies))))
		};
		return (Node)copies[this];
	}

	public override void CheckForNonsensicalCode()
	{
		base.CheckForNonsensicalCode();
		CheckWeirdAlwaysTrueConditions();
	}

	private void CheckWeirdAlwaysTrueConditions()
	{
		if (slots[0] is LiteralNode literalNode)
		{
			if (literalNode.value is ItemSO)
			{
				throw new ParseException(CodeUtilities.LocalizeAndFormat("error_item_condition", literalNode.value), literalNode.boxedParams.wordStart, literalNode.boxedParams.wordEnd);
			}
			if (literalNode.value is FarmObjectSO { isGround: false })
			{
				throw new ParseException(CodeUtilities.LocalizeAndFormat("error_entity_condition", literalNode.value), literalNode.boxedParams.wordStart, literalNode.boxedParams.wordEnd);
			}
			if (literalNode.value is FarmObjectSO { isGround: not false })
			{
				throw new ParseException(CodeUtilities.LocalizeAndFormat("error_ground_condition", literalNode.value), literalNode.boxedParams.wordStart, literalNode.boxedParams.wordEnd);
			}
			if (literalNode.value is UnlockSO)
			{
				throw new ParseException(CodeUtilities.LocalizeAndFormat("error_unlock_condition", literalNode.value), literalNode.boxedParams.wordStart, literalNode.boxedParams.wordEnd);
			}
		}
	}
}
