using System;
using System.Collections.Generic;
using UnityEngine;

public class UndoHistory
{
	private Dictionary<GameObject, Stack<(Func<object, object> callback, object state)>> undoHistory = new Dictionary<GameObject, Stack<(Func<object, object>, object)>>();

	private Dictionary<GameObject, Stack<(Func<object, object> callback, object state)>> redoHistory = new Dictionary<GameObject, Stack<(Func<object, object>, object)>>();

	public void Undo(GameObject gameObject)
	{
		if (undoHistory.ContainsKey(gameObject) && undoHistory[gameObject].Count != 0)
		{
			(Func<object, object>, object) tuple = undoHistory[gameObject].Pop();
			object item = tuple.Item1(tuple.Item2);
			redoHistory[gameObject].Push((tuple.Item1, item));
		}
	}

	public void Redo(GameObject gameObject)
	{
		if (redoHistory.ContainsKey(gameObject) && redoHistory[gameObject].Count != 0)
		{
			(Func<object, object>, object) tuple = redoHistory[gameObject].Pop();
			object item = tuple.Item1(tuple.Item2);
			undoHistory[gameObject].Push((tuple.Item1, item));
		}
	}

	public void AddChange(Func<object, object> callback, object state, GameObject gameObject)
	{
		if (!undoHistory.ContainsKey(gameObject))
		{
			undoHistory[gameObject] = new Stack<(Func<object, object>, object)>();
		}
		undoHistory[gameObject].Push((callback, state));
		if (!redoHistory.ContainsKey(gameObject))
		{
			redoHistory[gameObject] = new Stack<(Func<object, object>, object)>();
		}
		redoHistory[gameObject].Clear();
	}
}
