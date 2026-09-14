using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TitleSave
{
	public List<Pair<string, string>> dockedFiles;

	public List<string> minimizedFiles;

	public List<Pair<string, Vector2>> openFilePositions;

	public List<Pair<string, float>> openFileScrollPositions;

	public List<Pair<string, Vector2>> openFileSizes;

	public List<Pair<string, string>> openTitlePages;

	public List<string> windowHistory;
}
