using System;
using System.Collections.Generic;
using System.Collections.Generic.Integrations;
using System.Linq;
using UnityEngine;

public static class BuiltinFunctions
{
	private static List<PyFunction> functionList = new List<PyFunction>
	{
		new PyFunction("harvest", Harvest),
		new PyFunction("can_harvest", CanHarvest),
		new PyFunction("swap", Swap),
		new PyFunction("range", Range),
		new PyFunction("plant", Plant),
		new PyFunction("move", Move),
		new PyFunction("can_move", CanMove),
		new PyFunction("till", Till),
		new PyFunction("get_pos_x", GetPosX),
		new PyFunction("get_pos_y", GetPosY),
		new PyFunction("get_world_size", GetWorldSize),
		new PyFunction("get_entity_type", GetEntityType),
		new PyFunction("get_ground_type", GetGroundType),
		new PyFunction("get_time", GetTime, null, isFree: true),
		new PyFunction("get_tick_count", GetTickCount, null, isFree: true),
		new PyFunction("use_item", UseItem),
		new PyFunction("get_water", GetWater),
		new PyFunction("do_a_flip", DoAFlip),
		new PyFunction("change_hat", ChangeHat),
		new PyFunction("print", Print),
		new PyFunction("quick_print", QuickPrint, null, isFree: true),
		new PyFunction("len", Len),
		new PyFunction("num_items", NumItems),
		new PyFunction("get_cost", GetCost),
		new PyFunction("clear", Clear),
		new PyFunction("get_companion", GetCompanion),
		new PyFunction("unlock", Unlock),
		new PyFunction("num_unlocked", NumUnlocked),
		new PyFunction("leaderboard_run", LeaderboardRun),
		new PyFunction("measure", Measure),
		new PyFunction("min", Min),
		new PyFunction("max", Max),
		new PyFunction("abs", Abs),
		new PyFunction("str", StringConstructor),
		new PyFunction("random", Random),
		new PyFunction("simulate", Simulate),
		new PyFunction("list", ListConstructor),
		new PyFunction("set", SetConstructor),
		new PyFunction("dict", DictConstructor),
		new PyFunction("set_execution_speed", SetExecutionSpeed),
		new PyFunction("set_world_size", SetWorldSize),
		new PyFunction("spawn_drone", SpawnDrone),
		new PyFunction("num_drones", NumDrones),
		new PyFunction("max_drones", MaxDrones),
		new PyFunction("wait_for", Await),
		new PyFunction("has_finished", HasFinished),
		new PyFunction("pet_the_piggy", PetThePiggy),
		new PyFunction("tap", IncrementTapTapLootCounter)
	};

	private static List<PyFunction> methodList = new List<PyFunction>
	{
		new PyFunction("append", Append),
		new PyFunction("add", Add),
		new PyFunction("remove", Remove),
		new PyFunction("pop", Pop),
		new PyFunction("insert", Insert)
	};

	private static Dictionary<string, PyFunction> functions;

	private static Dictionary<string, PyFunction> methods;

	private static List<string> resetUnlocks = new List<string>
	{
		"loops", "for", "range", "if", "else", "elif", "debug", "debug_2", "timing", "lists",
		"dictionaries", "costs", "auto_unlock", "operators", "variables", "Items", "clear", "senses", "utilities", "can_harvest",
		"Entities", "get_world_size", "till", "use_item", "trade", "move", "multi_trade", "get_active_power", "North", "East",
		"South", "West", "functions", "import", "simulation", "get_water", "measure", "max_drones", "num_drones", "get_drone_id",
		"spawn_drone", "send", "receive"
	};

	public static Dictionary<string, PyFunction> Functions
	{
		get
		{
			if (functions == null)
			{
				functions = Enumerable.ToDictionary<PyFunction, string>((IEnumerable<PyFunction>)functionList, (Func<PyFunction, string>)((PyFunction f) => f.functionName));
			}
			return functions;
		}
	}

	public static Dictionary<string, PyFunction> Methods
	{
		get
		{
			if (methods == null)
			{
				methods = Enumerable.ToDictionary<PyFunction, string>((IEnumerable<PyFunction>)methodList, (Func<PyFunction, string>)((PyFunction f) => f.functionName));
			}
			return methods;
		}
	}

	public static IPyObject ItemsToNewDict(ItemBlock items)
	{
		if (items == null)
		{
			return new PyNone();
		}
		Dictionary<IPyObject, PyObjectBox> dictionary = new Dictionary<IPyObject, PyObjectBox>();
		for (int i = 0; i < items.items.Length; i++)
		{
			if (items.items[i] > 0.0)
			{
				dictionary.Add(ResourceManager.GetItem(i), new PyObjectBox(new PyNumber(items.items[i])));
			}
		}
		return new PyDict(dictionary);
	}

	private static void CorrectParams(List<IPyObject> parameters, List<Type> types, string function)
	{
		if (parameters.Count != types.Count)
		{
			PyTuple pyTuple = new PyTuple(parameters);
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_wrong_number_args", function + "()", types.Count, pyTuple));
		}
		for (int i = 0; i < parameters.Count; i++)
		{
			if (!types[i].IsInstanceOfType(parameters[i]))
			{
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_wrong_args", i + 1, function + "()", parameters[i]));
			}
		}
	}

	private static void NoParams(List<IPyObject> parameters, string function)
	{
		if (parameters != null && parameters.Count > 0)
		{
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_expected_no_args", function + "()"));
		}
	}

	private static double DoAFlip(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "do_a_flip");
		exec.States[droneId].currentSideEffect = SideEffect.DoAFlip;
		return 0.0;
	}

	private static double PetThePiggy(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "pet_the_piggy");
		exec.States[droneId].currentSideEffect = SideEffect.PetThePiggy;
		return 0.0;
	}

	private static double IncrementTapTapLootCounter(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "tap");
		if (sim.leaderboardType == LeaderboardType.none)
		{
			Ipc.Instance.Increment();
		}
		return Math.Floor(0.1 / sim.OpDuration.Seconds);
	}

	private static double Print(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		if (parameters.Count == 0)
		{
			throw new ExecuteException("error_empty_print");
		}
		string s = string.Join(' ', Enumerable.Select<IPyObject, string>((IEnumerable<IPyObject>)parameters, (Func<IPyObject, string>)((IPyObject p) => CodeUtilities.ToNiceString(p))));
		ProgramState programState = exec.States[droneId];
		programState.currentSideEffect = SideEffect.Print;
		programState.currentSideEffectArgument = new PyString(s);
		return 0.0;
	}

	private static double QuickPrint(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		if (parameters.Count == 0)
		{
			throw new ExecuteException("error_empty_print");
		}
		Logger.Log(string.Join(' ', Enumerable.Select<IPyObject, string>((IEnumerable<IPyObject>)parameters, (Func<IPyObject, string>)((IPyObject p) => CodeUtilities.ToNiceString(p)))));
		exec.States[droneId].ReturnValue = new PyNone();
		return 0.0;
	}

	private static double ChangeHat(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(parameters, new List<Type> { typeof(HatSO) }, "change_hat");
		ProgramState programState = exec.States[droneId];
		programState.currentSideEffect = SideEffect.ChangeHat;
		programState.currentSideEffectArgument = parameters[0];
		return 0.0;
	}

	private static double Min(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		return MinOrMax(parameters, isMax: false, exec, droneId);
	}

	private static double Max(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		return MinOrMax(parameters, isMax: true, exec, droneId);
	}

	private static double MinOrMax(List<IPyObject> parameters, bool isMax, Execution exec, int droneId)
	{
		if (parameters.Count == 0)
		{
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat(isMax ? "error_wrong_use_of_max" : "error_wrong_use_of_min", new PyList(new List<IPyObject>())));
		}
		IEnumerable<IPyObject> enumerable = parameters;
		if (parameters.Count == 1)
		{
			CorrectParams(parameters, new List<Type> { typeof(IEnumerable<IPyObject>) }, isMax ? "max" : "min");
			enumerable = (IEnumerable<IPyObject>)parameters[0];
		}
		if (!Enumerable.Any<IPyObject>(enumerable))
		{
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat(isMax ? "error_wrong_use_of_max" : "error_wrong_use_of_min", parameters[0]));
		}
		int steps = 0;
		IPyObject pyObject = Enumerable.First<IPyObject>(enumerable);
		foreach (IPyObject item in enumerable)
		{
			if (CodeUtilities.DeepCompare(item, pyObject, isMax ? "max" : "min", ref steps) * (isMax ? 1 : (-1)) > 0)
			{
				pyObject = item;
			}
		}
		exec.States[droneId].ReturnValue = pyObject;
		return 1.0 * (double)Mathf.Max(steps, 1);
	}

	private static double Abs(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(parameters, new List<Type> { typeof(PyNumber) }, "abs");
		exec.States[droneId].ReturnValue = new PyNumber(Math.Abs((PyNumber)parameters[0]));
		return 1.0;
	}

	private static double Swap(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(parameters, new List<Type> { typeof(PyGridDirection) }, "swap");
		ProgramState programState = exec.States[droneId];
		programState.currentSideEffect = SideEffect.Swap;
		programState.currentSideEffectArgument = parameters[0];
		return 0.0;
	}

	private static double Harvest(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "harvest");
		exec.States[droneId].currentSideEffect = SideEffect.Harvest;
		return 0.0;
	}

	private static double CanHarvest(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "can_harvest");
		exec.States[droneId].currentSideEffect = SideEffect.CanHarvest;
		return 0.0;
	}

	private static double Plant(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(parameters, new List<Type> { typeof(FarmObjectSO) }, "plant");
		ProgramState programState = exec.States[droneId];
		programState.currentSideEffect = SideEffect.Plant;
		programState.currentSideEffectArgument = parameters[0];
		return 0.0;
	}

	private static double Till(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "till");
		exec.States[droneId].currentSideEffect = SideEffect.Till;
		return 0.0;
	}

	private static double UseItem(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		int num;
		if (parameters.Count == 1)
		{
			CorrectParams(parameters, new List<Type> { typeof(ItemSO) }, "use_item");
			num = 1;
		}
		else
		{
			CorrectParams(parameters, new List<Type>
			{
				typeof(ItemSO),
				typeof(PyNumber)
			}, "use_item");
			num = (int)(double)(PyNumber)parameters[1];
		}
		ItemSO currentSideEffectArgument = (ItemSO)parameters[0];
		if (num < 1)
		{
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_negative_use_item", num));
		}
		ProgramState programState = exec.States[droneId];
		programState.currentSideEffect = SideEffect.UseItem;
		programState.currentSideEffectArgument = currentSideEffectArgument;
		programState.currentSideEffectArgument2 = new PyNumber(num);
		return 0.0;
	}

	private static double GetWater(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "get_water");
		exec.States[droneId].currentSideEffect = SideEffect.GetWater;
		return 0.0;
	}

	private static double Move(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(parameters, new List<Type> { typeof(PyGridDirection) }, "move");
		ProgramState programState = exec.States[droneId];
		programState.currentSideEffect = SideEffect.Move;
		programState.currentSideEffectArgument = parameters[0];
		return 0.0;
	}

	private static double CanMove(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(parameters, new List<Type> { typeof(PyGridDirection) }, "can_move");
		ProgramState programState = exec.States[droneId];
		programState.currentSideEffect = SideEffect.CanMove;
		programState.currentSideEffectArgument = parameters[0];
		return 0.0;
	}

	private static double GetEntityType(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "get_entity_type");
		exec.States[droneId].currentSideEffect = SideEffect.GetEntityType;
		return 0.0;
	}

	private static double GetGroundType(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "get_ground_type");
		exec.States[droneId].currentSideEffect = SideEffect.GetGroundType;
		return 0.0;
	}

	private static double GetTime(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "get_time");
		exec.States[droneId].currentSideEffect = SideEffect.GetTime;
		return 0.0;
	}

	private static double GetTickCount(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "get_tick_count");
		exec.States[droneId].ReturnValue = new PyNumber(exec.States[droneId].OpCount - exec.States[droneId].StartOpCount);
		return 0.0;
	}

	private static double Random(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "random");
		exec.States[droneId].ReturnValue = new PyNumber(exec.States[droneId].randomRandom.NextDouble());
		return 1.0;
	}

	private static double GetPosX(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "get_pos_x");
		exec.States[droneId].currentSideEffect = SideEffect.GetPosX;
		return 0.0;
	}

	private static double GetPosY(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "get_pos_y");
		exec.States[droneId].currentSideEffect = SideEffect.GetPosY;
		return 0.0;
	}

	private static double GetWorldSize(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "get_world_size");
		exec.States[droneId].currentSideEffect = SideEffect.GetWorldSize;
		return 0.0;
	}

	private static double Range(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		if (parameters.Count >= 3)
		{
			CorrectParams(parameters, new List<Type>
			{
				typeof(PyNumber),
				typeof(PyNumber),
				typeof(PyNumber)
			}, "range");
			exec.States[droneId].ReturnValue = new PyRange((PyNumber)parameters[0], (PyNumber)parameters[1], (PyNumber)parameters[2]);
		}
		else if (parameters.Count == 2)
		{
			CorrectParams(parameters, new List<Type>
			{
				typeof(PyNumber),
				typeof(PyNumber)
			}, "range");
			exec.States[droneId].ReturnValue = new PyRange((PyNumber)parameters[0], (PyNumber)parameters[1]);
		}
		else if (parameters.Count <= 1)
		{
			CorrectParams(parameters, new List<Type> { typeof(PyNumber) }, "range");
			exec.States[droneId].ReturnValue = new PyRange((PyNumber)parameters[0]);
		}
		return 1.0;
	}

	private static double Len(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		if (parameters.Count > 0 && parameters[0] is ICollection<IPyObject>)
		{
			exec.States[droneId].ReturnValue = new PyNumber(((ICollection<IPyObject>)parameters[0]).Count);
		}
		else
		{
			CorrectParams(parameters, new List<Type> { typeof(IReadOnlyCollection<IPyObject>) }, "len");
			exec.States[droneId].ReturnValue = new PyNumber(((IReadOnlyCollection<IPyObject>)parameters[0]).Count);
		}
		return 1.0;
	}

	private static double NumItems(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(parameters, new List<Type> { typeof(ItemSO) }, "num_items");
		ProgramState programState = exec.States[droneId];
		programState.currentSideEffect = SideEffect.NumItems;
		programState.currentSideEffectArgument = parameters[0];
		return 0.0;
	}

	private static double GetCost(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		if (parameters.Count >= 1 && parameters[0] is UnlockSO)
		{
			ProgramState programState = exec.States[droneId];
			programState.currentSideEffect = SideEffect.GetCost;
			programState.currentSideEffectArgument = parameters[0];
			if (parameters.Count == 1)
			{
				CorrectParams(parameters, new List<Type> { typeof(UnlockSO) }, "get_cost");
				programState.currentSideEffectArgument2 = null;
			}
			else
			{
				CorrectParams(parameters, new List<Type>
				{
					typeof(UnlockSO),
					typeof(PyNumber)
				}, "get_cost");
				programState.currentSideEffectArgument2 = parameters[1];
			}
		}
		else
		{
			CorrectParams(parameters, new List<Type> { typeof(FarmObjectSO) }, "get_cost");
			ProgramState programState2 = exec.States[droneId];
			programState2.currentSideEffect = SideEffect.GetCost;
			programState2.currentSideEffectArgument = parameters[0];
		}
		return 0.0;
	}

	private static double GetCompanion(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "get_companion");
		exec.States[droneId].currentSideEffect = SideEffect.GetCompanion;
		return 0.0;
	}

	private static double Measure(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		ProgramState programState = exec.States[droneId];
		programState.currentSideEffect = SideEffect.Measure;
		if (parameters.Count == 0 || (parameters.Count == 1 && parameters[0] is PyNone))
		{
			programState.currentSideEffectArgument = null;
		}
		else
		{
			CorrectParams(parameters, new List<Type> { typeof(PyGridDirection) }, "measure");
			programState.currentSideEffectArgument = parameters[0];
		}
		return 0.0;
	}

	private static double Clear(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "clear");
		exec.States[droneId].currentSideEffect = SideEffect.Clear;
		return 0.0;
	}

	private static double Unlock(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(parameters, new List<Type> { typeof(UnlockSO) }, "unlock");
		ProgramState programState = exec.States[droneId];
		programState.currentSideEffect = SideEffect.Unlock;
		programState.currentSideEffectArgument = parameters[0];
		return 0.0;
	}

	private static double NumUnlocked(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		ProgramState programState = exec.States[droneId];
		programState.currentSideEffect = SideEffect.NumUnlocked;
		if (parameters.Count == 1 && parameters[0] is UnlockSO)
		{
			CorrectParams(parameters, new List<Type> { typeof(UnlockSO) }, "num_unlocked");
			programState.currentSideEffectArgument = parameters[0];
		}
		else if (parameters.Count == 1 && parameters[0] is ItemSO)
		{
			CorrectParams(parameters, new List<Type> { typeof(ItemSO) }, "num_unlocked");
			programState.currentSideEffectArgument = parameters[0];
		}
		else if (parameters.Count == 1 && parameters[0] is HatSO)
		{
			CorrectParams(parameters, new List<Type> { typeof(HatSO) }, "num_unlocked");
			programState.currentSideEffectArgument = parameters[0];
		}
		else
		{
			CorrectParams(parameters, new List<Type> { typeof(FarmObjectSO) }, "num_unlocked");
			programState.currentSideEffectArgument = parameters[0];
		}
		return 0.0;
	}

	private static double LeaderboardRun(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(parameters, new List<Type>
		{
			typeof(LeaderboardSO),
			typeof(PyString),
			typeof(PyNumber)
		}, "leaderboard_run");
		LeaderboardSO leaderboardSO = (LeaderboardSO)parameters[0];
		PyString obj = (PyString)parameters[1];
		PyNumber currentSideEffectArgument = (PyNumber)parameters[2];
		MainSim.LeaderboardStartArgs currentSideEffectArgument2 = new MainSim.LeaderboardStartArgs(unlocks: leaderboardSO.everythingUnlocked ? GetUnlockStrings(ResourceManager.GetAllUnlocks(), leaderboardSO.singleDrone) : resetUnlocks, items: new ItemBlock(leaderboardSO.startItems), fileName: obj.str, globals: new List<KeyValuePair<string, IPyObject>>(), leaderboardName: leaderboardSO.leaderboardName, steamLeaderboardName: leaderboardSO.steamLeaderboardName, leaderboardType: leaderboardSO.leaderboardType, seed: -1, singleDrone: leaderboardSO.singleDrone);
		ProgramState programState = exec.States[droneId];
		programState.currentSideEffect = SideEffect.RunLeaderboard;
		programState.currentSideEffectArgument = currentSideEffectArgument;
		programState.currentSideEffectArgument2 = currentSideEffectArgument2;
		return 0.0;
	}

	private static List<string> GetUnlockStrings(IEnumerable<IPyObject> unlocks, bool isSingleDrone = false)
	{
		List<string> list = new List<string>(resetUnlocks);
		if (unlocks is PyDict pyDict)
		{
			foreach (KeyValuePair<IPyObject, PyObjectBox> item in pyDict.dict)
			{
				if (item.Key is UnlockSO unlockSO && item.Value.obj is PyNumber { num: var num })
				{
					if (num < 0.0 || num > (double)unlockSO.maxUnlockLevel)
					{
						num = unlockSO.maxUnlockLevel;
					}
					else if (num < 1.0)
					{
						continue;
					}
					list.Add(unlockSO.unlockName);
					if (unlockSO.IsMultiUnlock)
					{
						list.Add($"{unlockSO.unlockName}_{num}");
					}
					continue;
				}
				throw new ExecuteException("error_invalid_sim_unlocks");
			}
		}
		else
		{
			foreach (IPyObject unlock in unlocks)
			{
				if (unlock is UnlockSO unlockSO2)
				{
					if (!isSingleDrone || unlockSO2.unlockName != "megafarm")
					{
						list.Add(unlockSO2.unlockName);
					}
					if (unlockSO2.IsMultiUnlock)
					{
						if (isSingleDrone && unlockSO2.unlockName == "expand")
						{
							list.Add($"{unlockSO2.unlockName}_{5}");
						}
						else if (!isSingleDrone || unlockSO2.unlockName != "megafarm")
						{
							list.Add($"{unlockSO2.unlockName}_{unlockSO2.maxUnlockLevel}");
						}
					}
				}
				else
				{
					if (!(unlock is PyTuple pyTuple))
					{
						throw new ExecuteException("error_invalid_sim_unlocks");
					}
					if (pyTuple.Count != 2 || !(pyTuple[0] is UnlockSO unlockSO3) || !(pyTuple[1] is PyNumber pyNumber2) || !((double)pyNumber2 >= 1.0) || !((double)pyNumber2 <= (double)unlockSO3.maxUnlockLevel))
					{
						throw new ExecuteException("error_invalid_sim_unlocks");
					}
					list.Add(unlockSO3.unlockName);
					list.Add($"{unlockSO3.unlockName}_{(int)(double)pyNumber2}");
				}
			}
		}
		return list;
	}

	private static ItemBlock GetItemBlock(PyDict dict)
	{
		ItemBlock itemBlock = ItemBlock.CreateEmpty();
		foreach (KeyValuePair<IPyObject, PyObjectBox> item in dict.dict)
		{
			if (item.Key is ItemSO itemSO && item.Value.obj is PyNumber pyNumber)
			{
				itemBlock.AddItem(itemSO.itemId, Math.Max(0.0, pyNumber));
				continue;
			}
			throw new ExecuteException("error_invalid_sim_items");
		}
		return itemBlock;
	}

	private static List<KeyValuePair<string, IPyObject>> GetGlobals(PyDict dict)
	{
		List<KeyValuePair<string, IPyObject>> list = new List<KeyValuePair<string, IPyObject>>();
		foreach (KeyValuePair<IPyObject, PyObjectBox> item in dict.dict)
		{
			if (item.Key is PyString pyString)
			{
				IPyObject obj = item.Value.obj;
				if (obj != null)
				{
					list.Add(new KeyValuePair<string, IPyObject>(pyString.str, obj));
					continue;
				}
			}
			throw new ExecuteException("error_invalid_sim_globals");
		}
		return list;
	}

	private static double Simulate(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		if (exec.States[droneId] != exec.MainState)
		{
			throw new ExecuteException("simulate can only be called from the main execution");
		}
		CorrectParams(parameters, new List<Type>
		{
			typeof(PyString),
			typeof(IEnumerable<IPyObject>),
			typeof(PyDict),
			typeof(PyDict),
			typeof(PyNumber),
			typeof(PyNumber)
		}, "simulate");
		PyString obj = (PyString)parameters[0];
		List<string> unlockStrings = GetUnlockStrings((IEnumerable<IPyObject>)parameters[1]);
		ItemBlock itemBlock = GetItemBlock((PyDict)parameters[2]);
		List<KeyValuePair<string, IPyObject>> globals = GetGlobals((PyDict)parameters[3]);
		PyNumber pyNumber = (PyNumber)parameters[4];
		PyNumber currentSideEffectArgument = (PyNumber)parameters[5];
		MainSim.LeaderboardStartArgs currentSideEffectArgument2 = new MainSim.LeaderboardStartArgs(obj.str, unlockStrings, itemBlock, globals, "", "", LeaderboardType.simulation, (int)(double)pyNumber);
		ProgramState programState = exec.States[droneId];
		programState.currentSideEffect = SideEffect.Simulate;
		programState.currentSideEffectArgument = currentSideEffectArgument;
		programState.currentSideEffectArgument2 = currentSideEffectArgument2;
		return 0.0;
	}

	private static double SimulateAsync(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		return 200.0;
	}

	private static double SpawnDrone(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(Enumerable.ToList<IPyObject>(Enumerable.Take<IPyObject>((IEnumerable<IPyObject>)parameters, 1)), new List<Type> { typeof(PyFunction) }, "spawn_drone");
		ProgramState programState = exec.States[droneId];
		if (((PyFunction)parameters[0]).syntaxTree == null)
		{
			throw new ExecuteException("error_spawn_drone_builtin");
		}
		programState.currentSideEffect = SideEffect.SpawnDrone;
		programState.currentSideEffectArgument = new PyList(parameters);
		return 0.0;
	}

	private static double Await(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(parameters, new List<Type> { typeof(PyDroneHandle) }, "wait_for");
		ProgramState programState = exec.States[droneId];
		programState.currentSideEffect = SideEffect.Await;
		programState.currentSideEffectArgument = parameters[0];
		return 0.0;
	}

	private static double HasFinished(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(parameters, new List<Type> { typeof(PyDroneHandle) }, "has_finished");
		ProgramState programState = exec.States[droneId];
		programState.currentSideEffect = SideEffect.HasFinished;
		programState.currentSideEffectArgument = parameters[0];
		return 0.0;
	}

	private static double GetDroneId(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "get_drone_id");
		exec.States[droneId].currentSideEffect = SideEffect.GetDroneId;
		return 0.0;
	}

	private static double NumDrones(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "num_drones");
		exec.States[droneId].currentSideEffect = SideEffect.NumDrones;
		return 0.0;
	}

	private static double MaxDrones(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		NoParams(parameters, "max_drones");
		exec.States[droneId].currentSideEffect = SideEffect.MaxDrones;
		return 0.0;
	}

	private static double Send(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(parameters, new List<Type>
		{
			typeof(IPyObject),
			typeof(PyNumber)
		}, "send");
		IPyObject pyObject = parameters[0];
		int num = (int)(double)(PyNumber)parameters[1];
		if (num < 0 || num >= sim.farm.drones.Count)
		{
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_invalid_drone_id", num));
		}
		double num2 = pyObject.Size();
		exec.MessageChannel.Enqueue((pyObject, droneId, num), exec.States[droneId].OpCount + num2);
		exec.States[droneId].ReturnValue = new PyNone();
		return Math.Max(1.0, num2);
	}

	private static double Receive(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		int num = -1;
		if (parameters.Count > 0)
		{
			CorrectParams(parameters, new List<Type> { typeof(PyNumber) }, "receive");
			num = (int)(double)(PyNumber)parameters[0];
		}
		if (num >= sim.farm.drones.Count)
		{
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_invalid_drone_id", num));
		}
		IPyObject item2;
		if (num < 0)
		{
			if (exec.States[droneId].AllMessagesQueue.TryDequeue(out (IPyObject, int) item))
			{
				exec.States[droneId].ReturnValue = item.Item1;
				exec.States[droneId].MessageQueues[item.Item2].Dequeue();
			}
			else
			{
				exec.States[droneId].ReturnValue = new PyNone();
			}
		}
		else if (exec.States[droneId].MessageQueues[num].TryDequeue(out item2))
		{
			exec.States[droneId].ReturnValue = item2;
			exec.States[droneId].AllMessagesQueue.Remove((item2, num));
		}
		else
		{
			exec.States[droneId].ReturnValue = new PyNone();
		}
		return 1.0;
	}

	private static double ListConstructor(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		if (parameters.Count == 0)
		{
			exec.States[droneId].ReturnValue = new PyList(new List<IPyObject>());
			return 1.0;
		}
		CorrectParams(parameters, new List<Type> { typeof(IEnumerable<IPyObject>) }, "list");
		if (parameters[0] is IPySequence && ((IPySequence)parameters[0]).Count > 100000)
		{
			throw new ExecuteException("error_sequence_too_large");
		}
		PyList pyList = new PyList(Enumerable.ToList<IPyObject>((IEnumerable<IPyObject>)parameters[0]));
		exec.States[droneId].ReturnValue = pyList;
		return 1.0 * (double)(1 + pyList.Count);
	}

	private static double SetConstructor(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		if (parameters.Count == 0)
		{
			exec.States[droneId].ReturnValue = new PySet(new HashSet<IPyObject>());
			return 1.0;
		}
		CorrectParams(parameters, new List<Type> { typeof(IEnumerable<IPyObject>) }, "set");
		if (parameters[0] is IPySequence && ((IPySequence)parameters[0]).Count > 100000)
		{
			throw new ExecuteException("error_sequence_too_large");
		}
		PySet pySet = new PySet(Enumerable.ToHashSet<IPyObject>((IEnumerable<IPyObject>)parameters[0]));
		foreach (IPyObject item in pySet)
		{
			if (item is PyDict || item is PyList || item is PySet || item is PyModule)
			{
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_bad_key", item));
			}
		}
		exec.States[droneId].ReturnValue = pySet;
		return 1.0 * (double)(1 + pySet.Count);
	}

	private static double DictConstructor(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		if (parameters.Count == 0)
		{
			exec.States[droneId].ReturnValue = new PyDict(new Dictionary<IPyObject, PyObjectBox>());
			return 1.0;
		}
		CorrectParams(parameters, new List<Type> { typeof(PyDict) }, "dict");
		PyDict pyDict = new PyDict(Enumerable.ToDictionary<KeyValuePair<IPyObject, PyObjectBox>, IPyObject, PyObjectBox>((IEnumerable<KeyValuePair<IPyObject, PyObjectBox>>)((PyDict)parameters[0]).dict, (Func<KeyValuePair<IPyObject, PyObjectBox>, IPyObject>)((KeyValuePair<IPyObject, PyObjectBox> pair) => pair.Key), (Func<KeyValuePair<IPyObject, PyObjectBox>, PyObjectBox>)((KeyValuePair<IPyObject, PyObjectBox> pair) => new PyObjectBox(pair.Value.obj))));
		exec.States[droneId].ReturnValue = pyDict;
		return 1.0 * (double)(1 + 2 * pyDict.Count);
	}

	private static double StringConstructor(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		if (parameters.Count == 0)
		{
			exec.States[droneId].ReturnValue = new PyString("");
			return 1.0;
		}
		CorrectParams(parameters, new List<Type> { typeof(IPyObject) }, "str");
		PyString returnValue = new PyString(CodeUtilities.ToNiceString(parameters[0]));
		exec.States[droneId].ReturnValue = returnValue;
		return 1.0;
	}

	private static double SetExecutionSpeed(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(parameters, new List<Type> { typeof(PyNumber) }, "set_execution_speed");
		ProgramState programState = exec.States[droneId];
		programState.currentSideEffect = SideEffect.SetExecutionSpeed;
		programState.currentSideEffectArgument = parameters[0];
		return 0.0;
	}

	private static double SetWorldSize(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(parameters, new List<Type> { typeof(PyNumber) }, "set_world_size");
		ProgramState programState = exec.States[droneId];
		programState.currentSideEffect = SideEffect.SetWorldSize;
		programState.currentSideEffectArgument = parameters[0];
		return 0.0;
	}

	private static double Append(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(parameters, new List<Type>
		{
			typeof(PyList),
			typeof(IPyObject)
		}, "append");
		((PyList)parameters[0]).Add(parameters[1]);
		exec.States[droneId].ReturnValue = new PyNone();
		return 1.0;
	}

	private static double Add(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(parameters, new List<Type>
		{
			typeof(PySet),
			typeof(IPyObject)
		}, "add");
		IPyObject pyObject = parameters[1];
		if (pyObject is PyList || pyObject is PySet || pyObject is PyDict || pyObject is PyModule)
		{
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_bad_key", parameters[1]));
		}
		((PySet)parameters[0]).set.Add(parameters[1]);
		exec.States[droneId].ReturnValue = new PyNone();
		return 1.0 * (double)parameters[1].Size();
	}

	private static double Remove(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		if (parameters.Count > 0 && parameters[0] is PyList)
		{
			CorrectParams(parameters, new List<Type>
			{
				typeof(PyList),
				typeof(IPyObject)
			}, "remove");
			PyList pyList = (PyList)parameters[0];
			int steps = 0;
			bool flag = false;
			for (int i = 0; i < pyList.Count; i++)
			{
				if (CodeUtilities.DeepEquals(pyList[i], parameters[1], ref steps))
				{
					pyList.RemoveAt(i);
					flag = true;
					steps += pyList.Count - i;
					break;
				}
			}
			if (!flag)
			{
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_list_element_not_found", parameters[1]));
			}
			exec.States[droneId].ReturnValue = new PyNone();
			return 1.0 * (double)steps;
		}
		if (parameters.Count > 0 && parameters[0] is PySet)
		{
			CorrectParams(parameters, new List<Type>
			{
				typeof(PySet),
				typeof(IPyObject)
			}, "remove");
			PySet pySet = (PySet)parameters[0];
			if (!pySet.Contains(parameters[1]))
			{
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_set_element_not_found", parameters[1]));
			}
			pySet.set.Remove(parameters[1]);
			exec.States[droneId].ReturnValue = new PyNone();
			return 1.0 * (double)parameters[1].Size();
		}
		object[] array = new object[1];
		IPyObject pyObject2;
		if (parameters.Count <= 0)
		{
			IPyObject pyObject = new PyNone();
			pyObject2 = pyObject;
		}
		else
		{
			pyObject2 = parameters[0];
		}
		array[0] = pyObject2;
		throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_remove_on_non_list_or_set", array));
	}

	private static double Pop(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		if (parameters.Count > 0 && parameters[0] is PyList)
		{
			PyList pyList = (PyList)parameters[0];
			int num = -1;
			if (parameters.Count > 1)
			{
				CorrectParams(parameters, new List<Type>
				{
					typeof(PyList),
					typeof(PyNumber)
				}, "pop");
				num = (int)Math.Floor((PyNumber)parameters[1]);
			}
			if (num < 0)
			{
				num = pyList.Count + num;
			}
			if (num < 0 || num >= pyList.Count)
			{
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_index_out_of_bounds", num, pyList), 1, 1);
			}
			IPyObject returnValue = pyList[num];
			pyList.RemoveAt(num);
			exec.States[droneId].ReturnValue = returnValue;
			return 1.0 * (double)(pyList.Count - num + 1);
		}
		if (parameters.Count > 0 && parameters[0] is PyDict)
		{
			PyDict pyDict = (PyDict)parameters[0];
			CorrectParams(parameters, new List<Type>
			{
				typeof(PyDict),
				typeof(IPyObject)
			}, "pop");
			if (!pyDict.dict.ContainsKey(parameters[1]))
			{
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_set_element_not_found", parameters[1]));
			}
			exec.States[droneId].ReturnValue = pyDict.dict[parameters[1]].obj;
			pyDict.dict.Remove(parameters[1]);
			return 1.0 * (double)parameters[1].Size();
		}
		object[] array = new object[1];
		IPyObject pyObject2;
		if (parameters.Count <= 0)
		{
			IPyObject pyObject = new PyNone();
			pyObject2 = pyObject;
		}
		else
		{
			pyObject2 = parameters[0];
		}
		array[0] = pyObject2;
		throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_pop_on_non_dict_or_list", array));
	}

	private static double Insert(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
	{
		CorrectParams(parameters, new List<Type>
		{
			typeof(PyList),
			typeof(PyNumber),
			typeof(IPyObject)
		}, "insert");
		PyList pyList = (PyList)parameters[0];
		int num = (int)Math.Floor((PyNumber)parameters[1]);
		if (num < 0)
		{
			num = pyList.Count + num;
		}
		if (num < 0 || num > pyList.Count)
		{
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_index_out_of_bounds", num, pyList), 1, 1);
		}
		double result = 1.0 * (double)(pyList.Count - num + 1);
		pyList.Insert(num, parameters[2]);
		exec.States[droneId].ReturnValue = new PyNone();
		return result;
	}
}
