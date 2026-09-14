using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveGame
{
	public ItemBlock items;

	public List<Pair<string, string>> dockedFiles;

	public List<string> minimizedFiles;

	public List<Pair<string, Vector2>> openFilePositions;

	public List<Pair<string, float>> openFileScrollPositions;

	public List<Pair<string, Vector2>> openFileSizes;

	public List<Pair<string, string>> openDocPages;

	public List<string> unlocks;

	public int version;
}
