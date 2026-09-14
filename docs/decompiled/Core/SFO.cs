using System;
using UnityEngine;

[Serializable]
public struct SFO
{
	public Vector2Int pos;

	public string data;

	public SFO(Vector2Int pos, string data)
	{
		this.pos = pos;
		this.data = data;
	}
}
