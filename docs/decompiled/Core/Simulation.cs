using System;
using System.Collections.Generic;
using System.Threading;

public class Simulation
{
	public class Timer
	{
		public Action func;

		public bool stopped;
	}

	public const double BASE_OP_DURATION = 0.0025;

	public Farm farm;

	private Execution execution;

	public MainSim mainSim;

	private bool hasError;

	private PriorityQueue<Timer, Duration> timers = new PriorityQueue<Timer, Duration>();

	public bool stepByStepMode;

	private Random random;

	public Random randomVarious;

	public Random randomMaze;

	public Random randomSnake;

	public Random randomCactus;

	public Random randomSunflower;

	public Random randomPumpkin;

	public Random randomPoly;

	public Random randomRandom;

	public string leaderboardName;

	public string steamLeaderboardName;

	public LeaderboardType leaderboardType;

	public bool singleDrone;

	public double SpeedFactor { get; private set; } = 1.0;

	public Duration OpDuration { get; private set; } = Duration.FromSeconds(0.0025);

	public Duration CurrentTime { get; private set; }

	public bool Paused { get; set; }

	public Execution Execution => execution;

	public Simulation(MainSim mainSim, IEnumerable<string> unlocks, ItemBlock items, string leaderboardName, string steamLeaderboardName, LeaderboardType leaderboardType, int seed = -1, List<SFO> loadedGrounds = null, List<SFO> loadedEntities = null, bool resetUnlocks = false)
	{
		this.mainSim = mainSim;
		this.leaderboardName = leaderboardName;
		this.steamLeaderboardName = steamLeaderboardName;
		this.leaderboardType = leaderboardType;
		CurrentTime = new Duration(0L);
		if (seed >= 0)
		{
			random = new Random(seed);
		}
		else
		{
			random = new Random();
		}
		randomVarious = new Random(Helper.JustSha256It(random));
		randomMaze = new Random(Helper.JustSha256It(random));
		randomSnake = new Random(Helper.JustSha256It(random));
		randomCactus = new Random(Helper.JustSha256It(random));
		randomSunflower = new Random(Helper.JustSha256It(random));
		randomPumpkin = new Random(Helper.JustSha256It(random));
		randomPoly = new Random(Helper.JustSha256It(random));
		randomRandom = new Random(Helper.JustSha256It(random));
		farm = new Farm(this, unlocks, items, loadedGrounds, loadedEntities, resetUnlocks);
		ChangeExecutionSpeed(farm.MaxSpeedFactor());
	}

	public bool IsExecuting()
	{
		return Execution != null;
	}

	public void StartProgramExecution(Execution execution)
	{
		if (IsExecuting())
		{
			throw new Exception("Tried to start a simulation twice");
		}
		this.execution = execution;
		NextExecutionStep();
	}

	public void StopProgramExecution()
	{
		if (IsExecuting())
		{
			lock (execution.lockExecution)
			{
				execution.StopExecution();
				execution = null;
			}
			ChangeExecutionSpeed(farm.MaxSpeedFactor());
			hasError = false;
			farm.grid.SizeLimit = 0;
			farm.RemoveSpawnedDrones();
			stepByStepMode = false;
			Paused = false;
			Logger.Close();
		}
	}

	public void Error()
	{
		hasError = true;
		Paused = true;
	}

	public Timer StartTimer(Action func, Duration time)
	{
		Timer timer = new Timer();
		timer.func = func;
		Insert(timer, CurrentTime + time);
		return timer;
	}

	private void Insert(Timer timer, Duration finishTime)
	{
		timers.Enqueue(timer, finishTime);
	}

	public void ChangeExecutionSpeed(double speedFactor)
	{
		OpDuration = new Duration((long)Math.Ceiling(2500000.0 / speedFactor));
		SpeedFactor = speedFactor;
	}

	public void RunNextStep(Duration goalTime, object lockSimulation, bool stopOnFinished = false)
	{
		Duration priority2;
		lock (lockSimulation)
		{
			if (Paused || (stopOnFinished && !IsExecuting()))
			{
				Monitor.Wait(lockSimulation);
				return;
			}
			if (IsExecuting() && hasError)
			{
				StopProgramExecution();
			}
			Timer element;
			Duration priority;
			while (timers.TryPeek(out element, out priority) && CurrentTime < goalTime && element.stopped)
			{
				timers.Dequeue();
			}
			if ((!IsExecuting() || !(execution.NextExecutionTime <= goalTime)) && (!timers.TryPeek(out var _, out priority2) || !(priority2 <= goalTime)))
			{
				CurrentTime = goalTime;
				Monitor.Wait(lockSimulation);
				return;
			}
			if (timers.TryPeek(out var element3, out priority2) && (!IsExecuting() || execution.NextExecutionTime >= priority2))
			{
				CurrentTime = priority2;
				timers.Dequeue();
				element3.func();
				return;
			}
			CurrentTime = execution.NextExecutionTime;
			Paused = false;
		}
		priority2.nanoseconds--;
		Duration targetRunTime = Duration.Min(priority2, goalTime) - CurrentTime;
		execution.Execute(targetRunTime, lockSimulation);
	}

	public void NextExecutionStep()
	{
		if (execution != null && !execution.IsPerformingAStep)
		{
			Paused = false;
			execution.NextExecutionTime = CurrentTime;
		}
	}

	public void AddOpsToCurrentTime(double ops)
	{
		CurrentTime += OpDuration * ops;
	}

	public bool EvaluateName(string name, out IPyObject value)
	{
		lock (execution.lockExecution)
		{
			if (execution.MainState == null)
			{
				value = null;
				return false;
			}
			return execution.MainState.EvaluateVarAlongCallstack(name, out value);
		}
	}

	public Duration GetActionTime(double ops)
	{
		return OpDuration * ops;
	}
}
