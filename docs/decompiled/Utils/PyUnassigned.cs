using System.Collections.Generic;

public class PyUnassigned : IPyObject
{
	public override bool Equals(object obj)
	{
		return false;
	}

	public override int GetHashCode()
	{
		return 12903875;
	}

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		return this;
	}
}
