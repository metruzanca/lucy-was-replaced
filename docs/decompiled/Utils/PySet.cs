using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PySet : ICollection<IPyObject>, IEnumerable<IPyObject>, IEnumerable, IPyObject
{
	public HashSet<IPyObject> set;

	public int Count => set.Count;

	public bool IsReadOnly => false;

	public PySet(HashSet<IPyObject> set)
	{
		this.set = set;
	}

	public void Add(IPyObject item)
	{
		throw new NotImplementedException();
	}

	public void Clear()
	{
		throw new NotImplementedException();
	}

	public bool Contains(IPyObject item)
	{
		return set.Contains(item);
	}

	public void CopyTo(IPyObject[] array, int arrayIndex)
	{
		set.CopyTo(array);
	}

	public bool Remove(IPyObject item)
	{
		throw new NotImplementedException();
	}

	public int Size()
	{
		return Enumerable.Sum(Enumerable.Select<IPyObject, int>((IEnumerable<IPyObject>)set, (Func<IPyObject, int>)((IPyObject x) => x.Size())));
	}

	public IEnumerator<IPyObject> GetEnumerator()
	{
		return new PyCollectionEnumerator((ICollection<IPyObject>)set);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.ContainsKey(set))
		{
			return (PySet)copies[set];
		}
		PySet pySet = new PySet(new HashSet<IPyObject>());
		copies[set] = pySet;
		foreach (IPyObject item in set)
		{
			pySet.set.Add(item.DeepCopy(copies));
		}
		return pySet;
	}
}
