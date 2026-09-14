using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OldSaveGame
{
	public ItemBlock items;

	public List<string> functionNames;

	public List<string> functionCode;

	public List<string> functionDocked;

	public List<string> minimized;

	public List<Vector2> openFunctionPositions;

	public Vector2 docsPos;

	public string openDocsPage;

	public List<string> unlocks;

	public List<SFO> grounds;

	public List<SFO> entities;
}
