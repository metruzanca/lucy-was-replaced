using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Scope
{
	public FunctionNode functionNode;

	public Node callNode;

	private Scope parentScope;

	private Dictionary<string, (IPyObject val, bool isStatic)> vars = new Dictionary<string, (IPyObject, bool)>();

	public Scope(FunctionNode functionNode, Node callNode, Scope parentScope, HashSet<string> variableNames)
	{
		this.functionNode = functionNode;
		this.parentScope = parentScope;
		this.callNode = callNode;
		foreach (string variableName in variableNames)
		{
			vars.Add(variableName, (new PyUnassigned(), false));
		}
	}

	public Scope(FunctionNode functionNode, Node callNode, Scope parentScope, Dictionary<string, (IPyObject val, bool isStatic)> vars)
	{
		this.functionNode = functionNode;
		this.parentScope = parentScope;
		this.callNode = callNode;
		this.vars = vars;
	}

	public void SetVar(string varName, IPyObject value, bool checkShadow = true, bool isStatic = false)
	{
		if (vars.ContainsKey(varName))
		{
			vars[varName] = (value, isStatic);
		}
		else if (parentScope != null)
		{
			parentScope.SetVar(varName, value, checkShadow, isStatic);
		}
		else
		{
			vars[varName] = (value, isStatic);
		}
	}

	public void ImportVar(string varName, IPyObject value, bool isStatic = false)
	{
		vars[varName] = (value, isStatic);
	}

	public bool HasVar(string varName)
	{
		if (vars.ContainsKey(varName))
		{
			return true;
		}
		if (parentScope != null)
		{
			return parentScope.HasVar(varName);
		}
		return false;
	}

	public Scope DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.ContainsKey(this))
		{
			return (Scope)copies[this];
		}
		Scope scope2 = (Scope)(copies[this] = new Scope(functionNode, callNode, null, new HashSet<string>()));
		scope2.parentScope = parentScope?.DeepCopy(copies);
		foreach (KeyValuePair<string, (IPyObject, bool)> var in vars)
		{
			scope2.vars.Add(var.Key, (var.Value.Item1.DeepCopy(copies), var.Value.Item2));
		}
		return scope2;
	}

	public (IPyObject val, bool isStatic) Evaluate(string s, string currentFileName)
	{
		if (vars.TryGetValue(s, out (IPyObject, bool) value))
		{
			if (value.Item1 is PyUnassigned)
			{
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_use_before_assign", s));
			}
			return value;
		}
		if (parentScope != null)
		{
			return parentScope.Evaluate(s, currentFileName);
		}
		foreach (KeyValuePair<string, CodeWindow> codeWindow in MainSim.Inst.workspace.codeWindows)
		{
			if (codeWindow.Value.parsedFunctions.ContainsKey(s))
			{
				if (codeWindow.Key == currentFileName)
				{
					throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_call_before_def", s, codeWindow.Key));
				}
				if (HasVar(codeWindow.Key) && Evaluate(codeWindow.Key, currentFileName).val is PyModule)
				{
					throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_missing_module_access", s, codeWindow.Key));
				}
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_missing_import", s, codeWindow.Key));
			}
		}
		throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_name_not_defined", s));
	}

	public static IPyObject EvaluateConstant(string s)
	{
		return s switch
		{
			"True" => new PyBool(b: true), 
			"False" => new PyBool(b: false), 
			"None" => new PyNone(), 
			"Items" => new PyConstBag(Enumerable.ToDictionary<ItemSO, string, IPyObject>(Enumerable.Where<ItemSO>(ResourceManager.GetAllItems(), (Func<ItemSO, bool>)((ItemSO i) => i.enabled)), (Func<ItemSO, string>)((ItemSO i) => i.itemName), (Func<ItemSO, IPyObject>)((ItemSO i) => i)), "Items"), 
			"Entities" => new PyConstBag(Enumerable.ToDictionary<FarmObjectSO, string, IPyObject>(Enumerable.Where<FarmObjectSO>(ResourceManager.GetAllFarmObjects(), (Func<FarmObjectSO, bool>)((FarmObjectSO f) => !f.isGround)), (Func<FarmObjectSO, string>)((FarmObjectSO i) => i.objectName), (Func<FarmObjectSO, IPyObject>)((FarmObjectSO i) => i)), "Entities"), 
			"Grounds" => new PyConstBag(Enumerable.ToDictionary<FarmObjectSO, string, IPyObject>(Enumerable.Where<FarmObjectSO>(ResourceManager.GetAllFarmObjects(), (Func<FarmObjectSO, bool>)((FarmObjectSO f) => f.isGround)), (Func<FarmObjectSO, string>)((FarmObjectSO i) => i.objectName), (Func<FarmObjectSO, IPyObject>)((FarmObjectSO i) => i)), "Grounds"), 
			"Unlocks" => new PyConstBag(Enumerable.ToDictionary<UnlockSO, string, IPyObject>(Enumerable.Where<UnlockSO>(ResourceManager.GetAllUnlocks(), (Func<UnlockSO, bool>)((UnlockSO u) => u.enabled)), (Func<UnlockSO, string>)((UnlockSO u) => u.unlockName), (Func<UnlockSO, IPyObject>)((UnlockSO i) => i)), "Unlocks"), 
			"Leaderboards" => new PyConstBag(Enumerable.ToDictionary<LeaderboardSO, string, IPyObject>(ResourceManager.GetAllLeaderboards(), (Func<LeaderboardSO, string>)((LeaderboardSO u) => u.leaderboardName), (Func<LeaderboardSO, IPyObject>)((LeaderboardSO i) => i)), "Leaderboards"), 
			"Hats" => new PyConstBag(Enumerable.ToDictionary<HatSO, string, IPyObject>(Enumerable.Where<HatSO>(ResourceManager.GetAllHats(), (Func<HatSO, bool>)((HatSO h) => !h.hidden)), (Func<HatSO, string>)((HatSO i) => i.hatName), (Func<HatSO, IPyObject>)((HatSO i) => i)), "Hats")
			{
				hiddenElements = Enumerable.ToDictionary<HatSO, string, IPyObject>(Enumerable.Where<HatSO>(ResourceManager.GetAllHats(), (Func<HatSO, bool>)((HatSO h) => h.hidden)), (Func<HatSO, string>)((HatSO i) => i.hatName), (Func<HatSO, IPyObject>)((HatSO i) => i))
			}, 
			"North" => new PyGridDirection(GridDirection.North), 
			"East" => new PyGridDirection(GridDirection.East), 
			"South" => new PyGridDirection(GridDirection.South), 
			"West" => new PyGridDirection(GridDirection.West), 
			_ => null, 
		};
	}

	public static bool IsConstant(string s)
	{
		if (s == "None")
		{
			return true;
		}
		try
		{
			return EvaluateConstant(s) != null;
		}
		catch (ExecuteException)
		{
			return false;
		}
	}

	public static bool IsTrueValue(IPyObject o)
	{
		if (o is PyNone)
		{
			return false;
		}
		if (o is PyNumber)
		{
			return (double)(PyNumber)o != 0.0;
		}
		if (o is IEnumerable)
		{
			return ((IEnumerable)o).GetEnumerator().MoveNext();
		}
		if (o is PyFunction && OptionHolder.GetString("error forgot call") == "enabled")
		{
			PyFunction pyFunction = (PyFunction)o;
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_function_as_condition", pyFunction.functionName));
		}
		return true;
	}
}
