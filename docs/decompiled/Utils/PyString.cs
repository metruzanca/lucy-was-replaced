using System;
using System.Collections;
using System.Collections.Generic;

public class PyString : IPySequence, IReadOnlyList<IPyObject>, IEnumerable<IPyObject>, IEnumerable, IReadOnlyCollection<IPyObject>, IPyObject, IPyIndexable
{
	public string str;

	public int Count => str.Length;

	public IPyObject this[int index] => new PyString(str[index].ToString());

	public PyString(string s)
	{
		str = s;
	}

	public bool Contains(IPyObject obj, ref int steps)
	{
		steps += Count;
		if (!(obj is PyString))
		{
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_in_string", obj));
		}
		string value = ((PyString)obj).str;
		return str.Contains(value);
	}

	public int Size()
	{
		return Math.Max(1, Count / 8);
	}

	public override bool Equals(object obj)
	{
		if (obj is PyString)
		{
			return str == ((PyString)obj).str;
		}
		return false;
	}

	public override string ToString()
	{
		return str;
	}

	public override int GetHashCode()
	{
		return str.GetHashCode();
	}

	public IEnumerator<IPyObject> GetEnumerator()
	{
		string text = str;
		for (int i = 0; i < text.Length; i++)
		{
			yield return new PyString(text[i].ToString());
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		return this;
	}
}
