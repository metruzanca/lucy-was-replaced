using System;

[Serializable]
public struct SerializableItem
{
	public string name;

	public double nr;

	public SerializableItem(string name, double nr)
	{
		this.name = name;
		this.nr = nr;
	}
}
