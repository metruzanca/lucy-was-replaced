using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PyDict : ICollection<IPyObject>, IEnumerable<IPyObject>, IEnumerable, IPyObject, IPyIndexable
{
	public Dictionary<IPyObject, PyObjectBox> dict;

	public int Count => dict.Count;

	public bool IsReadOnly => false;

	public PyDict(Dictionary<IPyObject, PyObjectBox> dict)
	{
		this.dict = dict;
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
		return dict.ContainsKey(item);
	}

	public void CopyTo(IPyObject[] array, int arrayIndex)
	{
		dict.Keys.CopyTo(array, arrayIndex);
	}

	public bool Remove(IPyObject item)
	{
		throw new NotImplementedException();
	}

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.ContainsKey(dict))
		{
			return (PyDict)copies[dict];
		}
		PyDict pyDict = new PyDict(new Dictionary<IPyObject, PyObjectBox>());
		copies[dict] = pyDict;
		foreach (KeyValuePair<IPyObject, PyObjectBox> item in dict)
		{
			pyDict.dict[item.Key.DeepCopy(copies)] = new PyObjectBox(item.Value.obj.DeepCopy(copies));
		}
		return pyDict;
	}

	public IPyObject At(InternalPySequence indices, ref int steps, IPyObject valueToSet = null)
	{
		if (indices.Count != 1)
		{
			throw new ExecuteException("error_slice_dict");
		}
		IPyObject pyObject = indices[0];
		if (pyObject is PyDict || pyObject is PyList || pyObject is PySet || pyObject is PyModule)
		{
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_bad_key", pyObject));
		}
		if (!Contains(pyObject) && valueToSet == null)
		{
			throw new ExecuteException(string.Format(CodeUtilities.LocalizeAndFormat("error_key", pyObject)));
		}
		steps = Mathf.Max(1, pyObject.Size());
		if (valueToSet != null)
		{
			if (dict.ContainsKey(pyObject))
			{
				dict[pyObject].obj = valueToSet;
			}
			else
			{
				dict[pyObject] = new PyObjectBox(valueToSet);
			}
		}
		return dict[pyObject].obj;
	}

	public int Size()
	{
		return Enumerable.Sum(Enumerable.Select<IPyObject, int>((IEnumerable<IPyObject>)dict.Keys, (Func<IPyObject, int>)((IPyObject k) => k.Size())));
	}

	public IEnumerator<IPyObject> GetEnumerator()
	{
		return new PyCollectionEnumerator(dict.Keys);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
