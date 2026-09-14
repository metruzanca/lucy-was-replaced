using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PyTuple : IPySequence, IReadOnlyList<IPyObject>, IEnumerable<IPyObject>, IEnumerable, IReadOnlyCollection<IPyObject>, IPyObject, IPyIndexable
{
	public List<IPyObject> elements;

	public int Count => elements.Count;

	public IPyObject this[int index] => elements[index];

	public PyTuple(List<IPyObject> elements)
	{
		this.elements = elements;
	}

	public bool Contains(IPyObject item, ref int steps)
	{
		return new PyList(elements).Contains(item, ref steps);
	}

	public int Size()
	{
		return Enumerable.Sum(Enumerable.Select<IPyObject, int>((IEnumerable<IPyObject>)elements, (Func<IPyObject, int>)((IPyObject k) => k.Size())));
	}

	public override bool Equals(object obj)
	{
		if (!(obj is PyTuple))
		{
			return false;
		}
		int steps = 0;
		return CodeUtilities.DeepEquals(this, (PyTuple)obj, ref steps);
	}

	public override int GetHashCode()
	{
		int num = 1430287;
		foreach (IPyObject element in elements)
		{
			if (element is PyDict || element is PyList || element is PySet || element is PyModule)
			{
				throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_bad_key", this));
			}
			num = (num * 7302013) ^ element.GetHashCode();
		}
		return num;
	}

	public IEnumerator<IPyObject> GetEnumerator()
	{
		return new PyList.PyListEnumerator(elements);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.ContainsKey(elements))
		{
			return (PyTuple)copies[elements];
		}
		PyTuple pyTuple = new PyTuple(new List<IPyObject>());
		copies[elements] = pyTuple;
		foreach (IPyObject element in elements)
		{
			pyTuple.elements.Add(element.DeepCopy(copies));
		}
		return pyTuple;
	}
}
