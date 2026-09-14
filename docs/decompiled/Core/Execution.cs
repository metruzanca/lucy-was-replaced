using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Execution
{
	public const double ACTION_OPS = 200.0;

	public const double OPERATION_OPS = 1.0;

	public Simulation sim;

	private List<ProgramState> states;

	private bool activeDroneExecutedStep;

	private bool stopped;

	public object lockExecution = new object();

	public ProgramState MainState { get; private set; }

	public List<ProgramState> States => states;

	public PriorityQueue<(IPyObject data, int senderId, int receiverId), double> MessageChannel { get; } = new PriorityQueue<(IPyObject, int, int), double>();

	public double GlobalOpCount { get; private set; }

	public bool IsPerformingAStep { get; private set; }

	public int Id { get; private set; }

	public Duration NextExecutionTime { get; set; }

	public Execution(Simulation sim, Node syntaxTree, int id)
	{
		this.sim = sim;
		states = new List<ProgramState>();
		AddProgramState(0, syntaxTree, 0.0);
		MainState = states[0];
		Id = id;
		NextExecutionTime = sim.CurrentTime;
	}

	public void StopExecution()
	{
		if (!stopped)
		{
			stopped = true;
			if (sim.IsExecuting())
			{
				sim.StopProgramExecution();
			}
		}
	}

	public void AddProgramState(int droneId, Node syntaxTree, double opCount)
	{
		ProgramState programState = new ProgramState(opCount, sim.randomRandom, droneId);
		foreach (PyFunction item in Enumerable.Concat<PyFunction>((IEnumerable<PyFunction>)BuiltinFunctions.Functions.Values, (IEnumerable<PyFunction>)BuiltinFunctions.Methods.Values))
		{
			programState.moduleState.globalScope.SetVar(item.functionName, item, checkShadow: false, isStatic: true);
		}
		programState.PushOntoExecutionStack(syntaxTree.Execute(programState, this, 0));
		if (droneId >= states.Count)
		{
			states.Add(programState);
		}
		else
		{
			states[droneId] = programState;
		}
	}

	public void Execute(Duration targetRunTime, object lockSimulation)
	{
		IsPerformingAStep = true;
		double num = Math.Floor(GlobalOpCount + Math.Min(199.0, targetRunTime / sim.OpDuration));
		if (sim.stepByStepMode && activeDroneExecutedStep)
		{
			lock (lockSimulation)
			{
				sim.Paused = true;
				IsPerformingAStep = false;
				activeDroneExecutedStep = false;
				return;
			}
		}
		while (true)
		{
			double globalOpCount = GlobalOpCount;
			lock (lockExecution)
			{
				if (stopped)
				{
					break;
				}
				for (int i = 0; i < States.Count; i++)
				{
					if (States[i] != null && States[i].currentSideEffect == SideEffect.None && !(States[i].OpCount > num) && States[i].awaitedDroneId < 0)
					{
						States[i].PerformExecutionStep(num, out var flag);
						activeDroneExecutedStep |= flag;
					}
				}
				try
				{
					GlobalOpCount = Enumerable.Min<ProgramState>(Enumerable.Where<ProgramState>((IEnumerable<ProgramState>)States, (Func<ProgramState, bool>)((ProgramState s) => s != null && s.awaitedDroneId < 0)), (Func<ProgramState, double>)((ProgramState s) => s.OpCount));
				}
				catch (InvalidOperationException)
				{
					GlobalOpCount = num;
				}
			}
			lock (lockSimulation)
			{
				double num2 = 0.0;
				double num3 = num;
				for (int j = 0; j < States.Count; j++)
				{
					if (States[j] != null && States[j].currentDependencies.Count > 0)
					{
						try
						{
							foreach (var (dependency, wordStart, wordEnd) in States[j].currentDependencies)
							{
								sim.farm.AssertUnlocked(dependency, wordStart, wordEnd);
							}
							States[j].currentDependencies.Clear();
						}
						catch (ExecuteException currentExecuteException)
						{
							States[j].currentExecuteException = currentExecuteException;
							States[j].currentSideEffect = SideEffect.Error;
							ApplySideEffect(j);
						}
					}
					if (States[j] != null && States[j].awaitedDroneId < 0)
					{
						num3 = Math.Min(num3, States[j].OpCount);
					}
				}
				sim.AddOpsToCurrentTime(num3 - globalOpCount);
				if (!stopped && !sim.Paused)
				{
					for (int k = 0; k < States.Count; k++)
					{
						if (States[k] != null)
						{
							num2 += States[k].ConsumeOps();
						}
					}
					for (int l = 0; l < States.Count; l++)
					{
						if (GlobalOpCount > num)
						{
							break;
						}
						if (States[l] == null || States[l].awaitedDroneId >= 0)
						{
							continue;
						}
						if (States[l].hitBreakpoint)
						{
							sim.mainSim.StepByStepMode = true;
							States[l].hitBreakpoint = false;
							sim.farm.mainDroneId = l;
							MainState = States[l];
						}
						else if (States[l].currentSideEffect != 0 && !(States[l].OpCount > GlobalOpCount))
						{
							try
							{
								ApplySideEffect(l);
							}
							catch (ExecuteException currentExecuteException2)
							{
								States[l].currentExecuteException = currentExecuteException2;
								States[l].currentSideEffect = SideEffect.Error;
								ApplySideEffect(l);
							}
							if (States.Count > l && States[l] != null)
							{
								num2 += States[l].ConsumeOps();
								States[l].currentSideEffect = SideEffect.None;
								States[l].currentSideEffectArgument = null;
								States[l].currentSideEffectArgument2 = null;
							}
						}
					}
					sim.farm.UsedPower += num2 / 200.0 / 30.0;
				}
				try
				{
					GlobalOpCount = Enumerable.Min<ProgramState>(Enumerable.Where<ProgramState>((IEnumerable<ProgramState>)States, (Func<ProgramState, bool>)((ProgramState s) => s != null && s.awaitedDroneId < 0)), (Func<ProgramState, double>)((ProgramState s) => s.OpCount));
				}
				catch (InvalidOperationException)
				{
					GlobalOpCount = num;
				}
				NextExecutionTime = sim.CurrentTime + sim.OpDuration * (GlobalOpCount - num3);
				sim.AddOpsToCurrentTime(Math.Min(GlobalOpCount, num) - num3);
				if ((sim.stepByStepMode && activeDroneExecutedStep) || GlobalOpCount > num || sim.Paused)
				{
					break;
				}
			}
		}
	}

	private double ApplySideEffect(int droneId)
	{
		double num = 0.0;
		ProgramState programState = States[droneId];
		IPyObject currentSideEffectArgument = programState.currentSideEffectArgument;
		object currentSideEffectArgument2 = programState.currentSideEffectArgument2;
		SideEffect currentSideEffect = programState.currentSideEffect;
		bool flag = true;
		switch (currentSideEffect)
		{
		case SideEffect.Harvest:
		{
			bool flag2 = sim.farm.drones[droneId].Harvest();
			programState.ReturnValue = new PyBool(flag2);
			num = (flag2 ? 200.0 : 1.0);
			break;
		}
		case SideEffect.CanHarvest:
			programState.ReturnValue = new PyBool(sim.farm.drones[droneId].CanHarvest());
			num = 1.0;
			break;
		case SideEffect.Swap:
		{
			bool flag3 = sim.farm.drones[droneId].Swap((PyGridDirection)currentSideEffectArgument, programState);
			programState.ReturnValue = new PyBool(flag3);
			num = (flag3 ? 200.0 : 1.0);
			break;
		}
		case SideEffect.Plant:
		{
			FarmObjectSO farmObjectSO2 = (FarmObjectSO)currentSideEffectArgument;
			bool flag4 = farmObjectSO2.canBePlanted && sim.farm.drones[droneId].Plant(farmObjectSO2, programState);
			programState.ReturnValue = new PyBool(flag4);
			num = (flag4 ? 200.0 : 1.0);
			break;
		}
		case SideEffect.Move:
		{
			double ops;
			bool b = sim.farm.drones[droneId].Move((PyGridDirection)currentSideEffectArgument, programState, out ops);
			programState.ReturnValue = new PyBool(b);
			num = ops;
			break;
		}
		case SideEffect.CanMove:
			programState.ReturnValue = new PyBool(sim.farm.drones[droneId].CanMove((PyGridDirection)currentSideEffectArgument));
			num = 1.0;
			break;
		case SideEffect.Till:
			sim.farm.drones[droneId].ChangeGround("soil");
			programState.ReturnValue = new PyNone();
			num = 200.0;
			break;
		case SideEffect.GetPosX:
			programState.ReturnValue = new PyNumber(sim.farm.drones[droneId].pos.x);
			num = 1.0;
			break;
		case SideEffect.GetPosY:
			programState.ReturnValue = new PyNumber(sim.farm.drones[droneId].pos.y);
			num = 1.0;
			break;
		case SideEffect.GetWorldSize:
			programState.ReturnValue = new PyNumber(sim.farm.grid.WorldSize.y);
			num = 1.0;
			break;
		case SideEffect.GetEntityType:
			if (sim.farm.drones[droneId].EntityUnderDrone() != null)
			{
				programState.ReturnValue = sim.farm.drones[droneId].EntityUnderDrone().objectSO;
			}
			else
			{
				programState.ReturnValue = new PyNone();
			}
			num = 1.0;
			break;
		case SideEffect.GetGroundType:
			programState.ReturnValue = sim.farm.drones[droneId].GroundUnderDrone().objectSO;
			num = 1.0;
			break;
		case SideEffect.UseItem:
		{
			ItemSO itemSO2 = (ItemSO)currentSideEffectArgument;
			int num7 = (int)((PyNumber)currentSideEffectArgument2).num;
			if (!sim.farm.Items.Contains(itemSO2.itemId, num7))
			{
				Logger.LogWarning(string.Format(Localizer.Localize("warning_no_item_to_use"), itemSO2), States[droneId]);
				programState.ReturnValue = new PyBool(b: false);
				num = 1.0;
				break;
			}
			bool flag6 = false;
			bool useActionTicks = false;
			switch (itemSO2.itemName)
			{
			case "water":
				flag6 = sim.farm.drones[droneId].Water(num7);
				useActionTicks = flag6;
				break;
			case "fertilizer":
				flag6 = sim.farm.drones[droneId].Fertilize(num7);
				useActionTicks = flag6;
				break;
			case "weird_substance":
			{
				FarmObject farmObject = sim.farm.drones[droneId].EntityUnderDrone();
				if (farmObject is BushPlant bushPlant && sim.farm.IsUnlocked("mazes"))
				{
					int num8 = 1 << sim.farm.NumUnlocked("mazes") - 1;
					if (num7 % num8 != 0)
					{
						Logger.LogWarning(Localizer.Localize("warning_weird_substance_not_divisible"), States[droneId]);
					}
					int num9 = num7 / num8;
					if (num9 < 1)
					{
						bushPlant.ToggleWeird();
						break;
					}
					bushPlant.GenerateHedgeMaze(num9);
					flag6 = true;
					useActionTicks = flag6;
				}
				else if (farmObject is Treasure treasure && sim.farm.IsUnlocked("mazes"))
				{
					int num10 = num7 / (1 << sim.farm.NumUnlocked("mazes") - 1);
					if (num10 >= 1)
					{
						flag6 = treasure.RepositionTreasure(num10, out useActionTicks);
					}
				}
				else if (farmObject is Growable growable)
				{
					growable.ToggleWeird();
					flag6 = true;
					useActionTicks = flag6;
				}
				break;
			}
			}
			programState.ReturnValue = new PyBool(flag6);
			if (flag6)
			{
				sim.farm.Items.RemoveItem(itemSO2.itemId, num7);
			}
			num = (useActionTicks ? 200.0 : 1.0);
			break;
		}
		case SideEffect.GetWater:
			programState.ReturnValue = new PyNumber(sim.farm.drones[droneId].GetWater());
			num = 1.0;
			break;
		case SideEffect.ChangeHat:
			sim.farm.drones[droneId].ChangeHat((HatSO)currentSideEffectArgument, programState);
			programState.ReturnValue = new PyNone();
			num = 200.0;
			break;
		case SideEffect.NumItems:
			programState.ReturnValue = new PyNumber(sim.farm.Items.GetNumber(((ItemSO)currentSideEffectArgument).itemId));
			num = 1.0;
			break;
		case SideEffect.GetCost:
			if (currentSideEffectArgument is UnlockSO unlockSO2)
			{
				int numUnlocked = ((!(currentSideEffectArgument2 is PyNumber pyNumber)) ? (-1) : ((int)(double)pyNumber));
				States[droneId].ReturnValue = BuiltinFunctions.ItemsToNewDict(sim.farm.GetUnlockCost(unlockSO2, numUnlocked));
			}
			else if (currentSideEffectArgument is FarmObjectSO farmObjectSO3)
			{
				if (string.IsNullOrEmpty(farmObjectSO3.yieldUpgradeName))
				{
					States[droneId].ReturnValue = BuiltinFunctions.ItemsToNewDict(farmObjectSO3.cost);
				}
				else
				{
					int num5 = Mathf.Max(0, sim.farm.NumUnlocked(farmObjectSO3.yieldUpgradeName) - 1);
					States[droneId].ReturnValue = BuiltinFunctions.ItemsToNewDict(farmObjectSO3.cost * (1 << num5));
				}
			}
			num = 1.0;
			break;
		case SideEffect.Clear:
			sim.farm.RemoveSpawnedDrones();
			sim.farm.drones[0].ResetPos();
			sim.farm.grid.ClearGrid();
			MainState = States[droneId];
			States.RemoveAll((ProgramState s) => s != MainState);
			MainState.DroneId = 0;
			if (MainState.awaitedDroneId >= 0)
			{
				MainState.awaitedDroneId = -1;
				MainState.OpCount = GlobalOpCount;
				MainState.ReturnValue = PyNone.Instance;
			}
			num = 200.0;
			programState.ReturnValue = PyNone.Instance;
			break;
		case SideEffect.GetCompanion:
		{
			IPyObject returnValue2 = ((!sim.farm.grid.entities.ContainsKey(sim.farm.drones[droneId].pos) || !(sim.farm.grid.entities[sim.farm.drones[droneId].pos] is Growable)) ? new PyNone() : ((Growable)sim.farm.grid.entities[sim.farm.drones[droneId].pos]).GetCompanion());
			programState.ReturnValue = returnValue2;
			num = 1.0;
			break;
		}
		case SideEffect.Unlock:
		{
			bool flag5 = sim.farm.UnlockOrUpgrade((UnlockSO)currentSideEffectArgument, requireParent: false);
			programState.ReturnValue = new PyBool(flag5);
			num = (flag5 ? 200.0 : 1.0);
			break;
		}
		case SideEffect.NumUnlocked:
			if (currentSideEffectArgument is UnlockSO unlockSO)
			{
				programState.ReturnValue = new PyNumber(sim.farm.NumUnlocked(unlockSO));
			}
			else if (currentSideEffectArgument is ItemSO itemSO)
			{
				programState.ReturnValue = new PyNumber(sim.farm.NumUnlocked(itemSO.itemName));
			}
			else if (currentSideEffectArgument is FarmObjectSO farmObjectSO)
			{
				programState.ReturnValue = new PyNumber(sim.farm.NumUnlocked(farmObjectSO.objectName));
			}
			else if (currentSideEffectArgument is HatSO hatSO)
			{
				programState.ReturnValue = new PyNumber(sim.farm.NumUnlocked(hatSO.hatName));
			}
			num = 1.0;
			break;
		case SideEffect.Measure:
		{
			Vector2Int key;
			if (currentSideEffectArgument == null || currentSideEffectArgument is PyNone)
			{
				key = sim.farm.drones[droneId].pos;
			}
			else
			{
				GridDirection dir = (PyGridDirection)currentSideEffectArgument;
				key = sim.farm.grid.Wrap(sim.farm.drones[droneId].pos + dir.GetDirectionVector());
			}
			IPyObject pyObject = sim.farm.grid.entities.GetValueOrDefault(key)?.Measure();
			IPyObject returnValue;
			if (pyObject == null)
			{
				IPyObject pyObject2 = new PyNone();
				returnValue = pyObject2;
			}
			else
			{
				returnValue = pyObject;
			}
			programState.ReturnValue = returnValue;
			num = 1.0;
			break;
		}
		case SideEffect.SetExecutionSpeed:
		{
			double num6 = (PyNumber)currentSideEffectArgument;
			if (double.IsNaN(num6) || num6 > sim.farm.MaxSpeedFactor() || num6 < 0.1)
			{
				sim.ChangeExecutionSpeed(sim.farm.MaxSpeedFactor());
			}
			else
			{
				sim.ChangeExecutionSpeed(num6);
			}
			programState.ReturnValue = new PyNone();
			num = 200.0;
			break;
		}
		case SideEffect.SetWorldSize:
			if ((int)(double)(PyNumber)currentSideEffectArgument != sim.farm.grid.WorldSize.y)
			{
				foreach (Drone drone in sim.farm.drones)
				{
					drone?.ResetPos();
				}
				sim.farm.grid.SizeLimit = (int)(double)(PyNumber)currentSideEffectArgument;
			}
			programState.ReturnValue = new PyNone();
			num = 200.0;
			break;
		case SideEffect.GetTime:
			programState.ReturnValue = new PyNumber(sim.CurrentTime.Seconds);
			num = 0.0;
			break;
		case SideEffect.SpawnDrone:
		{
			int num2 = Enumerable.Count<ProgramState>(Enumerable.Where<ProgramState>((IEnumerable<ProgramState>)States, (Func<ProgramState, bool>)((ProgramState s) => s != null)));
			int num3 = Helper.NumDrones(sim.farm.NumUnlocked("megafarm"));
			if (num2 >= num3)
			{
				programState.ReturnValue = PyNone.Instance;
				num = 1.0;
				break;
			}
			PyList obj = (PyList)currentSideEffectArgument;
			PyFunction pyFunction = (PyFunction)obj[0];
			Dictionary<object, object> copies2 = new Dictionary<object, object>();
			pyFunction = (PyFunction)pyFunction.DeepCopy(copies2);
			int num4 = sim.farm.AddDrone(droneId);
			FunctionNode functionNode = (FunctionNode)pyFunction.syntaxTree;
			List<IPyObject> list = new List<IPyObject>();
			foreach (IPyObject item in Enumerable.Skip<IPyObject>((IEnumerable<IPyObject>)obj.list, 1))
			{
				list.Add(item.DeepCopy(copies2));
			}
			functionNode.Arguments = list;
			AddProgramState(num4, functionNode, GlobalOpCount + 200.0);
			States[num4].PushScope(new Scope(functionNode, null, pyFunction.parentScope, functionNode.Vars));
			PyDroneHandle pyDroneHandle2 = new PyDroneHandle(num4, sim.farm.droneGeneration);
			States[num4].DroneHandle = pyDroneHandle2;
			States[num4].CurrentExecutingNode = programState.CurrentExecutingNode;
			programState.ReturnValue = pyDroneHandle2;
			num = 200.0;
			if (sim.leaderboardType == LeaderboardType.none)
			{
				Achievements.UnlockAchievement("USE_MULTIPLE_DRONES");
				if (num2 + 1 == 32)
				{
					Achievements.UnlockAchievement("SWARM");
				}
			}
			break;
		}
		case SideEffect.GetDroneId:
			programState.ReturnValue = new PyNumber(droneId);
			num = 1.0;
			break;
		case SideEffect.NumDrones:
			programState.ReturnValue = new PyNumber(Enumerable.Count<ProgramState>(Enumerable.Where<ProgramState>((IEnumerable<ProgramState>)States, (Func<ProgramState, bool>)((ProgramState s) => s != null))));
			num = 1.0;
			break;
		case SideEffect.MaxDrones:
			programState.ReturnValue = new PyNumber(Helper.NumDrones(sim.farm.NumUnlocked("megafarm")));
			num = 1.0;
			break;
		case SideEffect.Await:
		{
			PyDroneHandle pyDroneHandle = (PyDroneHandle)currentSideEffectArgument;
			if (pyDroneHandle.returnValue != null)
			{
				Dictionary<object, object> copies = new Dictionary<object, object>();
				programState.ReturnValue = pyDroneHandle.returnValue.DeepCopy(copies);
				num = 1.0;
			}
			else
			{
				programState.awaitedDroneId = pyDroneHandle.id;
				num = 1.0;
			}
			break;
		}
		case SideEffect.HasFinished:
			if (((PyDroneHandle)currentSideEffectArgument).returnValue != null)
			{
				programState.ReturnValue = new PyBool(b: true);
			}
			else
			{
				programState.ReturnValue = new PyBool(b: false);
			}
			num = 1.0;
			break;
		case SideEffect.Terminated:
		{
			for (int i = 0; i < States.Count; i++)
			{
				if (States[i] != null && States[i].awaitedDroneId == droneId)
				{
					States[i].awaitedDroneId = -1;
					States[i].OpCount = GlobalOpCount;
					Dictionary<object, object> copies3 = new Dictionary<object, object>();
					States[i].ReturnValue = States[droneId].ReturnValue.DeepCopy(copies3);
				}
			}
			if (States[droneId].DroneHandle != null)
			{
				States[droneId].DroneHandle.returnValue = States[droneId].ReturnValue;
			}
			if (States[droneId] == MainState)
			{
				MainState = null;
			}
			States[droneId] = null;
			if (Enumerable.All<ProgramState>((IEnumerable<ProgramState>)States, (Func<ProgramState, bool>)((ProgramState s) => s == null)))
			{
				IsPerformingAStep = false;
				StopExecution();
			}
			else
			{
				sim.farm.RemoveDrone(droneId);
				MainState = States[sim.farm.mainDroneId];
			}
			break;
		}
		case SideEffect.Error:
		{
			ExecuteException currentExecuteException = States[droneId].currentExecuteException;
			Node currentExecutingNode = States[droneId].CurrentExecutingNode;
			int startIndex = ((currentExecuteException.startIndex >= 0) ? currentExecuteException.startIndex : currentExecutingNode.boxedParams.wordStart);
			int endIndex = ((currentExecuteException.endIndex >= 0) ? currentExecuteException.endIndex : currentExecutingNode.boxedParams.wordEnd);
			States[droneId].CurrentExecutingNode?.boxedParams.codeWindow?.SetErrorMessage(currentExecuteException.Message, startIndex, endIndex);
			Logger.LogError(Localizer.Localize(currentExecuteException.Message), States[droneId]);
			sim.Error();
			IsPerformingAStep = false;
			sim.farm.mainDroneId = droneId;
			MainState = States[droneId];
			if (sim.leaderboardType == LeaderboardType.none)
			{
				Achievements.UnlockAchievement("CAUSE_A_RUNTIME_ERROR");
			}
			break;
		}
		case SideEffect.DoAFlip:
			sim.farm.drones[droneId].DoAFlip();
			programState.ReturnValue = new PyNone();
			num = Math.Floor(1.0 / sim.OpDuration.Seconds);
			flag = false;
			break;
		case SideEffect.PetThePiggy:
			sim.farm.drones[droneId].PetThePiggy();
			programState.ReturnValue = new PyNone();
			num = Math.Floor(1.0 / sim.OpDuration.Seconds);
			flag = false;
			break;
		case SideEffect.Print:
		{
			string str = ((PyString)currentSideEffectArgument).str;
			sim.farm.drones[droneId].PrintToAir(str);
			Logger.Log(str);
			programState.ReturnValue = new PyNone();
			num = Math.Floor(1.0 / sim.OpDuration.Seconds);
			flag = false;
			break;
		}
		case SideEffect.Simulate:
			if (sim.mainSim != null && sim.leaderboardType == LeaderboardType.none)
			{
				MainSim.LeaderboardStartArgs startArgs2 = (MainSim.LeaderboardStartArgs)currentSideEffectArgument2;
				sim.mainSim.TimeFactor = (PyNumber)currentSideEffectArgument;
				sim.mainSim.ScheduleLeaderboardStart(startArgs2);
				sim.Paused = true;
			}
			else
			{
				Logger.LogWarning(Localizer.Localize("warning_recursive_simulation"), programState);
			}
			programState.ReturnValue = new PyNone();
			num = 200.0;
			break;
		case SideEffect.RunLeaderboard:
			if (sim.mainSim != null && sim.leaderboardType == LeaderboardType.none)
			{
				MainSim.LeaderboardStartArgs startArgs = (MainSim.LeaderboardStartArgs)currentSideEffectArgument2;
				sim.mainSim.TimeFactor = (PyNumber)currentSideEffectArgument;
				sim.mainSim.ScheduleLeaderboardStart(startArgs);
				sim.Paused = true;
			}
			else
			{
				Logger.LogWarning(Localizer.Localize("warning_recursive_simulation"), programState);
			}
			programState.ReturnValue = new PyNone();
			num = 200.0;
			break;
		}
		if (programState != null)
		{
			if (flag)
			{
				programState.OpCount += num;
			}
			else
			{
				programState.AddAndConsumeOps(num);
			}
		}
		return num;
	}
}
