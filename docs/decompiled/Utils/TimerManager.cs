using System;
using System.Collections.Generic;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
	public struct Timer
	{
		public Action func;

		public double waitTime;

		public double startTime;

		public bool loop;

		public double TimeOfExecution => startTime + waitTime;
	}

	private static TimerManager _instance;

	private List<Timer> timers = new List<Timer>();

	private double currentTime;

	public static TimerManager Inst => _instance;

	public double CurrentTime => currentTime;

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
		currentTime = Time.time;
	}

	public static void StartTimer(Action func, double time, bool loop = false, double startTime = -1.0)
	{
		Timer timer = default(Timer);
		timer.waitTime = time;
		timer.startTime = ((startTime >= 0.0) ? startTime : Inst.currentTime);
		timer.loop = loop;
		timer.func = func;
		Inst.Insert(timer);
	}

	public static void StopTimer(Action func)
	{
		List<Timer> list = Inst.timers;
		for (int num = list.Count - 1; num >= 0; num--)
		{
			if (list[num].func == func)
			{
				list.RemoveAt(num);
			}
		}
	}

	private void Insert(Timer timer)
	{
		int num = timers.Count;
		while (num > 0 && Inst.timers[num - 1].TimeOfExecution < timer.TimeOfExecution)
		{
			num--;
		}
		timers.Insert(num, timer);
	}

	private void Update()
	{
		currentTime = Time.timeAsDouble;
		double num = 0.0;
		while (timers.Count > 0 && timers[timers.Count - 1].TimeOfExecution <= (double)(Time.time + Time.deltaTime))
		{
			Timer timer = timers[timers.Count - 1];
			timers.RemoveAt(timers.Count - 1);
			num += timer.startTime - currentTime + timer.waitTime;
			currentTime = (double)Time.time + num;
			if (timer.loop)
			{
				timer.startTime = currentTime;
				Insert(timer);
			}
			timer.func();
		}
	}
}
