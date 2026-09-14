using System;
using System.Collections.Generic;
using System.Linq;

public class ImportNode : Node
{
	private List<string> moduleNames;

	private bool unpack;

	private bool unpackAll;

	private List<string> varsToUnpack;

	private bool isStatic;

	public override string NodeName => "import";

	public ImportNode(CodeWindow func, int startIndex, int endIndex, List<string> moduleNames, bool unpack, bool unpackAll, List<string> varsToUnpack, bool isStatic)
		: base(func, startIndex, endIndex)
	{
		this.moduleNames = moduleNames;
		this.unpack = unpack;
		this.unpackAll = unpackAll;
		this.varsToUnpack = varsToUnpack;
		this.isStatic = isStatic;
	}

	public ImportNode(BoxedNodeParams boxedParams, List<string> moduleNames, bool unpack, bool unpackAll, List<string> varsToUnpack, bool isStatic)
		: base(boxedParams)
	{
		this.moduleNames = moduleNames;
		this.unpack = unpack;
		this.unpackAll = unpackAll;
		this.varsToUnpack = varsToUnpack;
		this.isStatic = isStatic;
	}

	public override IEnumerable<double> Execute(ProgramState state, Execution execution, int depth)
	{
		ErrorsAndBreakpoints(state, execution, depth);
		Blink(state, execution);
		if (CheckIncrementOpCount(state, execution, 0.0))
		{
			yield return 0.0;
		}
		foreach (string name in moduleNames)
		{
			if (name == "__builtins__" || name == "builtins")
			{
				continue;
			}
			if (!state.ModuleCache.ContainsKey(name))
			{
				if (!MainSim.Inst.workspace.codeWindows.TryGetValue(name, out var value))
				{
					throw new ExecuteException("error_module_not_found");
				}
				Node node = value.Parse();
				if (state.DroneId > 0)
				{
					Dictionary<object, object> copies = new Dictionary<object, object>();
					node = node.DeepCopy(copies);
				}
				if (node == null)
				{
					throw new ExecuteException("error_syntax_error_in_import");
				}
				Dictionary<string, (IPyObject, bool)> dictionary = new Dictionary<string, (IPyObject, bool)>();
				dictionary["__name__"] = (new PyString(name), false);
				state.ModuleCache[name] = new PyModule(dictionary, name);
				Scope scope = new Scope(null, null, null, dictionary);
				foreach (PyFunction item in Enumerable.Concat<PyFunction>((IEnumerable<PyFunction>)BuiltinFunctions.Functions.Values, (IEnumerable<PyFunction>)BuiltinFunctions.Methods.Values))
				{
					scope.SetVar(item.functionName, item, checkShadow: false, isStatic: true);
				}
				ModuleState moduleState = new ModuleState
				{
					globalScope = scope,
					currentExecutingNode = node
				};
				ModuleState oldModuleState = state.moduleState;
				state.moduleState = moduleState;
				foreach (double item2 in node.Execute(state, execution, depth))
				{
					yield return item2;
				}
				state.moduleState = oldModuleState;
				state.ModuleCache[name].fullyInitialized = true;
			}
			else if (execution.sim.leaderboardType == LeaderboardType.none && !state.ModuleCache[name].fullyInitialized)
			{
				Achievements.UnlockAchievement("CIRCULAR_IMPORT");
			}
			if (unpack)
			{
				if (unpackAll)
				{
					try
					{
						foreach (KeyValuePair<string, (IPyObject, bool)> element in state.ModuleCache[name].elements)
						{
							if (!element.Key.StartsWith('_'))
							{
								state.CurrentScope.ImportVar(element.Key, element.Value.Item1, element.Value.Item2);
							}
						}
					}
					catch (InvalidOperationException)
					{
					}
					continue;
				}
				foreach (string item3 in varsToUnpack)
				{
					(IPyObject, bool) tuple = state.ModuleCache[name].Export(item3, boxedParams.wordStart, boxedParams.wordEnd);
					state.CurrentScope.ImportVar(item3, tuple.Item1, tuple.Item2);
				}
			}
			else
			{
				state.CurrentScope.ImportVar(name, state.ModuleCache[name], isStatic);
			}
		}
	}

	public override void CheckDependencies(ProgramState state, Execution execution)
	{
		if (moduleNames.Count != 1 || moduleNames[0] != "__builtins__")
		{
			state.currentDependencies.Add(("import", boxedParams.wordStart, boxedParams.wordEnd));
		}
	}

	public override Node DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.TryGetValue(this, out var value))
		{
			return (Node)value;
		}
		copies[this] = new ImportNode(boxedParams, moduleNames, unpack, unpackAll, varsToUnpack, isStatic)
		{
			slots = Enumerable.ToList<Node>(Enumerable.Select<Node, Node>((IEnumerable<Node>)slots, (Func<Node, Node>)((Node s) => s.DeepCopy(copies))))
		};
		return (Node)copies[this];
	}
}
