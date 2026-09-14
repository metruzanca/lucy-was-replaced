using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PyList : IList<IPyObject>, ICollection<IPyObject>, IEnumerable<IPyObject>, IEnumerable, IPySequence, IReadOnlyList<IPyObject>, IReadOnlyCollection<IPyObject>, IPyObject, IPyIndexable
{
	public class PyListEnumerator : IEnumerator<IPyObject>, IEnumerator, IDisposable
	{
		private int i;

		private List<IPyObject> list;

		public IPyObject Current => list[i];

		object IEnumerator.Current => list[i];

		public PyListEnumerator(List<IPyObject> list)
		{
			this.list = list;
			i = -1;
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			i++;
			return i < list.Count;
		}

		public void Reset()
		{
			i = -1;
		}
	}

	public List<IPyObject> list;

	public IPyObject this[int index]
	{
		get
		{
			return list[index];
		}
		set
		{
			list[index] = value;
		}
	}

	public int Count => list.Count;

	public bool IsReadOnly => false;

	public PyList(List<IPyObject> list)
	{
		this.list = list;
	}

	public bool Contains(IPyObject obj, ref int steps)
	{
		foreach (IPyObject item in list)
		{
			if (CodeUtilities.DeepEquals(obj, item, ref steps))
			{
				return true;
			}
		}
		return false;
	}

	public void Add(IPyObject item)
	{
		list.Add(item);
	}

	public void Clear()
	{
		list.Clear();
	}

	public bool Contains(IPyObject item)
	{
		return list.Contains(item);
	}

	public void CopyTo(IPyObject[] array, int arrayIndex)
	{
		list.CopyTo(array, arrayIndex);
	}

	public int IndexOf(IPyObject item)
	{
		return list.IndexOf(item);
	}

	public void Insert(int index, IPyObject item)
	{
		list.Insert(index, item);
	}

	public bool Remove(IPyObject item)
	{
		return list.Remove(item);
	}

	public void RemoveAt(int index)
	{
		list.RemoveAt(index);
	}

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.ContainsKey(list))
		{
			return (PyList)copies[list];
		}
		PyList pyList = new PyList(new List<IPyObject>());
		copies[list] = pyList;
		foreach (IPyObject item in list)
		{
			pyList.Add(item.DeepCopy(copies));
		}
		return pyList;
	}

	public int Size()
	{
		return Enumerable.Sum(Enumerable.Select<IPyObject, int>((IEnumerable<IPyObject>)list, (Func<IPyObject, int>)((IPyObject k) => k.Size())));
	}

	public IEnumerator<IPyObject> GetEnumerator()
	{
		return new PyListEnumerator(list);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
