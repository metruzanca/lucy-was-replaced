using System.Collections.Generic;

public class PyDroneHandle : IPyObject
{
	public int id;

	public int generation;

	public IPyObject returnValue;

	public PyDroneHandle(int id, int generation = 0)
	{
		this.id = id;
		this.generation = generation;
	}

	public static implicit operator int(PyDroneHandle py)
	{
		return py.id;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is PyDroneHandle))
		{
			return false;
		}
		return ((PyDroneHandle)obj).generation == generation;
	}

	public override string ToString()
	{
		return $"<drone {generation}>";
	}

	public override int GetHashCode()
	{
		return generation.GetHashCode();
	}

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		return this;
	}
}
