using System.Collections.Generic;

public class PyNone : IPyObject
{
	public static PyNone Instance { get; } = new PyNone();

	public override bool Equals(object obj)
	{
		return obj is PyNone;
	}

	public override int GetHashCode()
	{
		return 12903874;
	}

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		return this;
	}
}
