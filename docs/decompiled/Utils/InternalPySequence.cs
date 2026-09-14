using System;
using System.Collections;
using System.Collections.Generic;

public class InternalPySequence : IPySequence, IReadOnlyList<IPyObject>, IEnumerable<IPyObject>, IEnumerable, IReadOnlyCollection<IPyObject>, IPyObject, IPyIndexable
{
	public List<IPyObject> elements;

	public int Count => elements.Count;

	public IPyObject this[int index] => elements[index];

	public InternalPySequence(List<IPyObject> elements)
	{
		this.elements = elements;
	}

	public bool Contains(IPyObject obj, ref int steps)
	{
		return elements.Contains(obj);
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
		throw new NotImplementedException();
	}

	public int Size()
	{
		throw new NotImplementedException();
	}
}
