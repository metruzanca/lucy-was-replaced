using System.Collections.Generic;

public class PyGridDirection : IPyObject
{
	public GridDirection dir;

	public PyGridDirection(GridDirection dir)
	{
		this.dir = dir;
	}

	public static implicit operator GridDirection(PyGridDirection py)
	{
		return py.dir;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is PyGridDirection))
		{
			return false;
		}
		return ((PyGridDirection)obj).dir == dir;
	}

	public override int GetHashCode()
	{
		return dir.GetHashCode();
	}

	public override string ToString()
	{
		return dir.ToString();
	}

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		return this;
	}
}
