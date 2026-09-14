using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class Logger
{
	public delegate void PrintAction();

	public const int NUM_OUTPUT_LIMIT = 100;

	public const int MESSAGE_LENGTH_LIMIT = 1000;

	private static HashSet<Node> warningNodes = new HashSet<Node>();

	private static LinkedList<string> output = new LinkedList<string>();

	private static StreamWriter logWriter = null;

	private static string persistentDataPath = Helper.persistentDataPath;

	private static object outputLock = new object();

	public static event PrintAction OnOutputChanged;

	public static void Log(string logMessage)
	{
		WriteLog(logMessage, addSeparator: false);
	}

	public static void LogError(string errorMessage, ProgramState state)
	{
		WriteLog("Error: " + errorMessage + "\nIn: " + state.GetTrace(), addSeparator: true);
	}

	public static void LogWarning(string warningMessage, ProgramState state)
	{
		if (!(OptionHolder.GetString("print warnings") == "disabled") && !warningNodes.Contains(state.CurrentExecutingNode))
		{
			WriteLog("Warning: " + warningMessage + "\nIn: " + state.GetTrace(), addSeparator: true);
			MainSim.Inst.warningIcon.PopUp();
			warningNodes.Add(state.CurrentExecutingNode);
		}
	}

	public static void Close()
	{
		logWriter?.Dispose();
		logWriter = null;
	}

	public static void Clear()
	{
		lock (outputLock)
		{
			output.Clear();
		}
		warningNodes.Clear();
		MainSim.Inst.warningIcon.Dismiss();
		try
		{
			File.WriteAllText(Path.Combine(persistentDataPath, "output.txt"), "");
		}
		catch (IOException message)
		{
			Debug.LogError(message);
		}
		if (logWriter != null)
		{
			logWriter = null;
		}
		Logger.OnOutputChanged?.Invoke();
	}

	public static string GetOutputString()
	{
		lock (outputLock)
		{
			return string.Join("\n", (IEnumerable<string>)output);
		}
	}

	private static void WriteLog(string logMessage, bool addSeparator)
	{
		if (addSeparator)
		{
			logMessage += "\n--------------------------------------------------------------------";
		}
		try
		{
			if (logWriter == null)
			{
				logWriter = new StreamWriter(Path.Combine(persistentDataPath, "output.txt"), append: true);
			}
			logWriter.WriteLine(CodeUtilities.RemoveSyntaxTags(logMessage));
			logWriter.Flush();
		}
		catch (IOException message)
		{
			Debug.LogError(message);
		}
		if (logMessage.Length > 1000)
		{
			logMessage = logMessage.Substring(0, 1000);
		}
		lock (outputLock)
		{
			output.AddLast(logMessage);
			if (output.Count >= 100)
			{
				output.RemoveFirst();
			}
			Logger.OnOutputChanged?.Invoke();
		}
	}
}
