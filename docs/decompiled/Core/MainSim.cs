using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

public class MainSim : MonoBehaviour
{
	public class LeaderboardStartArgs
	{
		public string fileName;

		public IEnumerable<string> unlocks;

		public ItemBlock items;

		public List<KeyValuePair<string, IPyObject>> globals;

		public string leaderboardName;

		public string steamLeaderboardName;

		public LeaderboardType leaderboardType;

		public int seed;

		public bool singleDrone;

		public LeaderboardStartArgs(string fileName, IEnumerable<string> unlocks, ItemBlock items, List<KeyValuePair<string, IPyObject>> globals, string leaderboardName, string steamLeaderboardName, LeaderboardType leaderboardType, int seed = -1, bool singleDrone = false)
		{
			this.fileName = fileName;
			this.unlocks = unlocks;
			this.items = items;
			this.globals = globals;
			this.leaderboardName = leaderboardName;
			this.steamLeaderboardName = steamLeaderboardName;
			this.leaderboardType = leaderboardType;
			this.seed = seed;
			this.singleDrone = singleDrone;
		}
	}

	private static MainSim _instance;

	private const float MAX_SIM_TIME_PER_FRAME = 0.2f;

	private const long MIN_LEADERBOARD_TIME = 7200000000000L;

	public PiggyBank pb;

	public Transform sceneScaler;

	public LeaderboardManager leaderboardManager;

	public Workspace workspace;

	public WarningPopup warningPopup;

	public WarningIcon warningIcon;

	public ResearchMenu researchMenu;

	public Menu menu;

	public BlinkManager blinkManager;

	[NonSerialized]
	public Camera mainCamera;

	public Inventory inv;

	private ConcurrentQueue<HatSO> hatsToUnlock = new ConcurrentQueue<HatSO>();

	public int harvestFactor = 1;

	[SerializeField]
	private double timeFactor = 1.0;

	[NonSerialized]
	public Vector2Int hoveredCell = new Vector2Int(-1, -1);

	private double lastHoveredTime;

	private Simulation sim;

	public Simulation storedSim;

	private Duration goalTime;

	public Duration totalLeaderboardTime;

	public int numLeaderboardRuns;

	private bool leaderboardCancelled;

	private LeaderboardStartArgs startLeaderboardNow;

	private LeaderboardStartArgs prevLeaderboardStart;

	private CodeWindow activeCodeWindow;

	public int executionId;

	public volatile bool dirty;

	private object lockSimulation = new object();

	private volatile bool disposed;

	private bool prevStepByStepMode;

	public static MainSim Inst => _instance;

	public double TimeFactor
	{
		get
		{
			return timeFactor;
		}
		set
		{
			if (value < 0.0 || value > 1000000.0 || double.IsNaN(value) || double.IsInfinity(value))
			{
				timeFactor = 1000000.0;
			}
			else if (value < 0.01)
			{
				timeFactor = 0.01;
			}
			else
			{
				timeFactor = value;
			}
		}
	}

	public bool StepByStepMode
	{
		get
		{
			lock (lockSimulation)
			{
				if (sim.IsExecuting())
				{
					return sim.stepByStepMode;
				}
				return false;
			}
		}
		set
		{
			lock (lockSimulation)
			{
				if (sim.IsExecuting() && sim.stepByStepMode != value)
				{
					sim.stepByStepMode = value;
					sim.NextExecutionStep();
				}
			}
		}
	}

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		else
		{
			_instance = this;
		}
		mainCamera = Camera.main;
	}

	private void Start()
	{
		Thread thread = new Thread(SimulationLoop);
		thread.IsBackground = true;
		thread.Priority = System.Threading.ThreadPriority.AboveNormal;
		thread.Start();
	}

	private void SimulationLoop()
	{
		while (!disposed)
		{
			sim.RunNextStep(goalTime, lockSimulation, sim.leaderboardType != LeaderboardType.none);
		}
	}

	private void OnDestroy()
	{
		disposed = true;
	}

	private void Update()
	{
		Saver.ApplyCodeChanges(workspace);
		lock (lockSimulation)
		{
			float num = Mathf.Min(Time.deltaTime, 0.2f);
			goalTime = sim.CurrentTime + Duration.FromSeconds((double)num * TimeFactor);
			if (sim.leaderboardType != 0 && !sim.IsExecuting())
			{
				LeaderboardExecutionFinished();
			}
			if (startLeaderboardNow != null)
			{
				StartLeaderboard(startLeaderboardNow);
				startLeaderboardNow = null;
			}
			activeCodeWindow?.SetExecutionColor();
			if (sim.IsExecuting() && sim.Execution.MainState != null && sim.Execution.MainState.CurrentExecutingNode?.boxedParams.codeWindow != activeCodeWindow)
			{
				CodeWindow codeWindow = sim.Execution.MainState.CurrentExecutingNode?.boxedParams.codeWindow;
				if (codeWindow != null)
				{
					activeCodeWindow = codeWindow;
					activeCodeWindow.StartExecutionMode(closeErrors: false);
					if (StepByStepMode)
					{
						activeCodeWindow.StartStepByStepMode();
					}
					Transform obj = activeCodeWindow.transform;
					obj.SetSiblingIndex(obj.parent.childCount - 1);
				}
			}
			else if (!sim.IsExecuting() && activeCodeWindow != null)
			{
				activeCodeWindow = null;
				foreach (CodeWindow value in workspace.codeWindows.Values)
				{
					value.StopExecutionMode();
				}
			}
			if (prevStepByStepMode != StepByStepMode)
			{
				foreach (CodeWindow value2 in workspace.codeWindows.Values)
				{
					if (value2.isExecuting)
					{
						if (StepByStepMode)
						{
							value2.StartStepByStepMode();
						}
						else
						{
							value2.StartExecutionMode();
						}
					}
				}
				prevStepByStepMode = StepByStepMode;
			}
			if (sim.farm != null && sim.farm.drones != null && sim.leaderboardType == LeaderboardType.none)
			{
				FMODSoundManager.UpdateDroneParams(Enumerable.ToArray<(float, Vector3)>(Enumerable.Select<Drone, (float, Vector3)>(Enumerable.Where<Drone>((IEnumerable<Drone>)sim.farm.drones, (Func<Drone, bool>)((Drone d) => d != null)), (Func<Drone, (float, Vector3)>)((Drone d) => d.GetSpeedAndPos(Time.time)))));
				FMODSoundManager.SetGameSpeedParam((float)(Duration.FromSeconds(0.0025) / sim.OpDuration * (double)Enumerable.Count<Drone>(Enumerable.Where<Drone>((IEnumerable<Drone>)sim.farm.drones, (Func<Drone, bool>)((Drone d) => d != null)))));
			}
			Vector2Int vector2Int = GetHoveredCell();
			if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1) && sim.farm.grid.IsWithinBounds(vector2Int))
			{
				bool flag = workspace.tooltip.CanShowTooltipImmediate();
				if (vector2Int != hoveredCell)
				{
					hoveredCell = vector2Int;
					lastHoveredTime = Time.time;
				}
				else if ((double)Time.time - lastHoveredTime > 0.4000000059604645)
				{
					hoveredCell = vector2Int;
					if (!menu.gameObject.activeInHierarchy && !researchMenu.IsOpen)
					{
						workspace.tooltip.SetTooltipImmediate(GetHoveredTooltip());
					}
					else
					{
						flag = false;
					}
				}
				if (flag)
				{
					hoveredCell = vector2Int;
				}
				else
				{
					hoveredCell = new Vector2Int(-1, -1);
				}
			}
			else
			{
				hoveredCell = new Vector2Int(-1, -1);
				workspace.tooltip.SetTooltipImmediate(null);
			}
			if (hatsToUnlock.TryDequeue(out var result) && sim.farm != null)
			{
				if (IsSimulating() && storedSim != null)
				{
					if (storedSim.farm.UnlockHat(result))
					{
						HatPopup.Inst.ShowPopup(result);
					}
				}
				else if (sim.farm.UnlockHat(result) || result.hatName == "the_farmers_remains")
				{
					HatPopup.Inst.ShowPopup(result);
				}
			}
			Monitor.Pulse(lockSimulation);
		}
	}

	public void SetupSim(IEnumerable<string> unlocks, ItemBlock items, List<SFO> loadedGrounds = null, List<SFO> loadedEntities = null, bool resetUnlocks = false)
	{
		lock (lockSimulation)
		{
			sim = new Simulation(this, unlocks, items, "", "", LeaderboardType.none, -1, loadedGrounds, loadedEntities, resetUnlocks);
			foreach (UnlockSO allUnlock in ResourceManager.GetAllUnlocks())
			{
				if (sim.farm.IsUnlocked(allUnlock.unlockName))
				{
					researchMenu.openedUnlockDocs.Add(allUnlock.docs);
				}
			}
			if (sim.farm.NumUnlocked("expand") >= 2)
			{
				researchMenu.openedUnlockDocs.Add("docs/unlocks/expand_2.md");
			}
		}
	}

	private void StartLeaderboard(LeaderboardStartArgs startArgs)
	{
		if (workspace.codeWindows.TryGetValue(startArgs.fileName, out var value))
		{
			Node node = value.Parse();
			if (node != null && node != null)
			{
				if (startArgs.leaderboardType != LeaderboardType.simulation)
				{
					StopMainExecution();
				}
				else
				{
					sim.Paused = true;
				}
				storedSim = sim;
				sim = new Simulation(this, startArgs.unlocks, startArgs.items, startArgs.leaderboardName, startArgs.steamLeaderboardName, startArgs.leaderboardType, startArgs.seed)
				{
					singleDrone = startArgs.singleDrone
				};
				Execution execution = new Execution(sim, node, executionId);
				executionId++;
				value.StartExecutionMode();
				foreach (KeyValuePair<string, IPyObject> global in startArgs.globals)
				{
					Dictionary<object, object> copies = new Dictionary<object, object>();
					execution.MainState.moduleState.globalScope.SetVar(global.Key, global.Value.DeepCopy(copies), checkShadow: false);
				}
				sim.StartProgramExecution(execution);
				goalTime = Duration.FromSeconds(0.0);
				prevLeaderboardStart = startArgs;
				prevLeaderboardStart.items = new ItemBlock(prevLeaderboardStart.items);
				leaderboardManager.StartLeaderboardRun(startArgs.leaderboardType != LeaderboardType.simulation);
				return;
			}
		}
		sim.Paused = false;
		TimeFactor = 1.0;
	}

	public void ScheduleLeaderboardStart(LeaderboardStartArgs startArgs)
	{
		startLeaderboardNow = startArgs;
	}

	private void LeaderboardExecutionFinished()
	{
		if (!leaderboardManager.IsRunning)
		{
			return;
		}
		double num = TimeFactor;
		TimeFactor = 1.0;
		if (sim.leaderboardType != LeaderboardType.simulation)
		{
			sim.Paused = true;
			bool flag = sim.leaderboardType switch
			{
				LeaderboardType.reset => sim.farm.IsUnlocked("leaderboard"), 
				LeaderboardType.farm_resources => sim.farm.Items.Contains(ResourceManager.GetLeaderboard(sim.leaderboardName).goalItems), 
				_ => false, 
			};
			totalLeaderboardTime += sim.CurrentTime;
			numLeaderboardRuns++;
			if (leaderboardCancelled)
			{
				leaderboardCancelled = false;
				flag = false;
			}
			if (totalLeaderboardTime >= new Duration(7200000000000L) || !flag)
			{
				leaderboardManager.StopLeaderboardRun(flag, sim.leaderboardName, sim.steamLeaderboardName, totalLeaderboardTime.ToTimeSpan() / numLeaderboardRuns);
				totalLeaderboardTime = new Duration(0L);
				numLeaderboardRuns = 0;
				return;
			}
			_ = sim.leaderboardName;
			_ = sim.leaderboardType;
			int seed = Mathf.Abs(sim.randomVarious.Next() + sim.randomSunflower.Next() + sim.randomSnake.Next() + sim.randomRandom.Next() + sim.randomPumpkin.Next() + sim.randomPoly.Next() + sim.randomMaze.Next() + sim.randomCactus.Next());
			RestoreMainSim();
			prevLeaderboardStart.seed = seed;
			StartLeaderboard(prevLeaderboardStart);
			TimeFactor = num;
		}
		else
		{
			leaderboardManager.RemoveOverlay();
			if (storedSim.IsExecuting())
			{
				storedSim.Execution.MainState.ReturnValue = new PyNumber(sim.CurrentTime.Seconds);
			}
			RestoreMainSim();
			sim.Paused = false;
		}
	}

	public void RestoreMainSim()
	{
		lock (lockSimulation)
		{
			sim = storedSim;
			storedSim = null;
			goalTime = sim.CurrentTime;
		}
	}

	public void StartMainExecution(CodeWindow cw, Node syntaxTree)
	{
		lock (lockSimulation)
		{
			if (IsExecuting())
			{
				return;
			}
			activeCodeWindow = cw;
			if (activeCodeWindow != null)
			{
				activeCodeWindow.SetExecutionColor();
				activeCodeWindow.StartExecutionMode(closeErrors: false);
				if (StepByStepMode)
				{
					activeCodeWindow.StartStepByStepMode();
				}
				Transform obj = activeCodeWindow.transform;
				obj.SetSiblingIndex(obj.parent.childCount - 1);
			}
			Execution execution = new Execution(sim, syntaxTree, executionId);
			executionId++;
			Logger.Clear();
			sim.StartProgramExecution(execution);
		}
		if (sim.leaderboardType == LeaderboardType.none)
		{
			Achievements.UnlockAchievement("RUN_YOUR_FIRST_CODE");
		}
	}

	public void StopMainExecution()
	{
		lock (lockSimulation)
		{
			if (sim.leaderboardType != 0 && sim.leaderboardType != LeaderboardType.simulation)
			{
				leaderboardCancelled = true;
			}
			storedSim?.StopProgramExecution();
			sim.StopProgramExecution();
		}
	}

	public void PlaySound(SoundEffectType sound, Vector2Int farmPos)
	{
		if (TimeFactor < 2.0)
		{
			SoundManager.EnqueueSound(sound, GridManager.CellToLocal(farmPos));
		}
	}

	public void PlayEffect(VFXType effect, Vector3 localPos, bool changeColor = false, Color color = default(Color), string text = null)
	{
		if (TimeFactor < 2.0)
		{
			VFXManager.EnqueueEffect(effect, localPos, changeColor, color, text);
		}
	}

	public void CollectItemEffect(int item, Vector3 localPos)
	{
		if (TimeFactor < 2.0)
		{
			pb.EnqueueCollect(item, localPos);
		}
	}

	public void BlinkEffect(Node node)
	{
		blinkManager.EnqueueBlink(node);
	}

	public List<(FunctionNode func, Node callNode)> GetCallStack()
	{
		lock (lockSimulation)
		{
			if (!sim.IsExecuting())
			{
				return null;
			}
			lock (sim.Execution.lockExecution)
			{
				return sim.Execution?.MainState?.GetCallStack();
			}
		}
	}

	public bool IsUnlocked(string unlock)
	{
		lock (lockSimulation)
		{
			return sim.farm.IsUnlocked(unlock);
		}
	}

	public int NumUnlocked(string unlock)
	{
		lock (lockSimulation)
		{
			return sim.farm.NumUnlocked(unlock);
		}
	}

	public Dictionary<string, int> GetUnlocks()
	{
		lock (lockSimulation)
		{
			return sim.farm.GetUnlocks();
		}
	}

	public double GetNumItem(int itemId)
	{
		lock (lockSimulation)
		{
			return sim.farm.Items.GetNumber(itemId);
		}
	}

	public ItemBlock GetInventory()
	{
		lock (lockSimulation)
		{
			return new ItemBlock(sim.farm.Items);
		}
	}

	public HashSet<string> GetUnlockedKeywords()
	{
		lock (lockSimulation)
		{
			return new HashSet<string>((IEnumerable<string>)sim.farm.unlockedKeyWords);
		}
	}

	public ItemBlock GetUnlockCost(UnlockSO unlock)
	{
		lock (lockSimulation)
		{
			return sim.farm.GetUnlockCost(unlock);
		}
	}

	public bool EvaluateName(string name, out IPyObject value)
	{
		lock (lockSimulation)
		{
			if (sim.IsExecuting())
			{
				return sim.EvaluateName(name, out value);
			}
			value = null;
			return false;
		}
	}

	public void NextExecutionStep()
	{
		lock (lockSimulation)
		{
			sim.NextExecutionStep();
		}
	}

	public bool UnlockOrUpgrade(UnlockSO unlockSO)
	{
		lock (lockSimulation)
		{
			if (IsSimulating())
			{
				return false;
			}
			return sim.farm.UnlockOrUpgrade(unlockSO);
		}
	}

	public void UnlockHat(HatSO hatSO)
	{
		hatsToUnlock.Enqueue(hatSO);
	}

	public void ResetUnlocksToMax()
	{
		lock (lockSimulation)
		{
			if (!IsSimulating())
			{
				sim.farm.ResetUnlocksToMax();
			}
		}
	}

	public bool IsSimulating()
	{
		lock (lockSimulation)
		{
			return sim.leaderboardType != LeaderboardType.none;
		}
	}

	public bool MightBeSimulating()
	{
		return storedSim != null;
	}

	public bool IsExecuting()
	{
		lock (lockSimulation)
		{
			return sim.IsExecuting() || storedSim != null;
		}
	}

	public List<string> GetSerializedUnlocks()
	{
		lock (lockSimulation)
		{
			return sim.farm.SerializeUnlocks();
		}
	}

	public List<(Mesh, Matrix4x4)> GetFarmMeshes()
	{
		lock (lockSimulation)
		{
			List<(Mesh, Matrix4x4)> list = new List<(Mesh, Matrix4x4)>();
			foreach (FarmObject item in Enumerable.Concat<FarmObject>((IEnumerable<FarmObject>)sim.farm.grid.entities.Values, (IEnumerable<FarmObject>)sim.farm.grid.grounds.Values))
			{
				foreach (var mesh in item.GetMeshes())
				{
					list.Add(mesh);
				}
			}
			return list;
		}
	}

	public List<(HatSO, bool highlight, Matrix4x4)> GetDroneMeshes()
	{
		lock (lockSimulation)
		{
			List<(HatSO, bool, Matrix4x4)> list = new List<(HatSO, bool, Matrix4x4)>();
			foreach (Drone item in Enumerable.Where<Drone>((IEnumerable<Drone>)sim.farm.drones, (Func<Drone, bool>)((Drone d) => d != null)))
			{
				list.Add((item.hat.hatSO, IsExecuting() && sim.farm.mainDroneId == item.DroneId && (sim.Paused || StepByStepMode), item.GetTransform()));
			}
			return list;
		}
	}

	public Duration GetCurrentTime()
	{
		lock (lockSimulation)
		{
			return sim.CurrentTime;
		}
	}

	public Vector2Int GetWorldSize()
	{
		lock (lockSimulation)
		{
			return sim.farm.grid.WorldSize;
		}
	}

	private TooltipInfo GetHoveredTooltip()
	{
		ItemBlock itemBlock = null;
		if (sim.farm.grid.IsWithinBounds(hoveredCell))
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("`x = ").Append(hoveredCell.x).Append(", y = ")
				.Append(hoveredCell.y)
				.Append("`")
				.Append("\n\n");
			if (sim.farm.grid.entities.TryGetValue(hoveredCell, out var value))
			{
				stringBuilder.Append("### `").Append(CodeUtilities.ToNiceString(value.objectSO)).Append("`");
				if (value is Growable growable && value.objectSO.className != "Apple")
				{
					stringBuilder.Append("\n").Append(CodeUtilities.LocalizeAndFormat("runtime_growable_tooltip", new PyNumber(growable.GrowTime.Seconds), new PyNumber(growable.GrownPercent * 100.0)));
				}
				IPyObject pyObject = value.Measure();
				if (pyObject != null && !(pyObject is PyNone) && IsUnlocked("measure"))
				{
					stringBuilder.Append("\n");
					stringBuilder.Append("`measure(): " + CodeUtilities.ToNiceString(pyObject) + "`");
				}
				stringBuilder.Append("\n").Append(Localizer.Localize("runtime_entity_tooltip"));
				stringBuilder.Append("\n\n");
				itemBlock = value.GetYield();
			}
			stringBuilder.Append("### `").Append(CodeUtilities.ToNiceString(sim.farm.grid.grounds[hoveredCell].objectSO)).Append("`")
				.Append("\n")
				.Append(CodeUtilities.LocalizeAndFormat("runtime_ground_tooltip", new PyNumber(sim.farm.grid.waterVolume[hoveredCell.x, hoveredCell.y])));
			return new TooltipInfo(stringBuilder.ToString(), 0.2f)
			{
				itemBlock = itemBlock
			};
		}
		hoveredCell = new Vector2Int(-1, -1);
		return null;
	}

	private Vector2Int GetHoveredCell()
	{
		Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
		return GridManager.LocalToCell((ray.origin + ray.direction * ((0f - ray.origin.z + base.transform.position.z) / ray.direction.z) - base.transform.position) / sceneScaler.localScale.x);
	}
}
