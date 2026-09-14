using System.Collections;
using System.Collections.Generic;

public class PyConstBag : IEnumerable<IPyObject>, IEnumerable, IPyObject, IPyNameSpace
{
	private Dictionary<string, IPyObject> elements;

	public Dictionary<string, IPyObject> hiddenElements;

	private string toString;

	public PyConstBag(Dictionary<string, IPyObject> elements, string toString)
	{
		this.elements = elements;
		this.toString = toString;
	}

	public (IPyObject val, bool isStatic) Evaluate(string value, int wordStart, int wordEnd)
	{
		value = value.ToLowerInvariant();
		if (!elements.ContainsKey(value))
		{
			if (hiddenElements != null && hiddenElements.ContainsKey(value))
			{
				return (val: hiddenElements[value], isStatic: true);
			}
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_invalid_const", toString + "." + CodeUtilities.ToUpperSnake(value)), wordStart, wordEnd);
		}
		return (val: elements[value], isStatic: true);
	}

	public override bool Equals(object obj)
	{
		if (!(obj is PyConstBag))
		{
			return false;
		}
		return ((PyConstBag)obj).toString == toString;
	}

	public override int GetHashCode()
	{
		return toString.GetHashCode();
	}

	public IEnumerator<IPyObject> GetEnumerator()
	{
		return elements.Values.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public override string ToString()
	{
		return toString;
	}

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		return this;
	}

	public int Size()
	{
		return 1;
	}
}
