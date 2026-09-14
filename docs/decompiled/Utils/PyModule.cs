using System.Collections.Generic;

public class PyModule : IPyNameSpace, IPyObject
{
	public Dictionary<string, (IPyObject val, bool isStatic)> elements;

	private string moduleName;

	public bool fullyInitialized;

	public PyModule(Dictionary<string, (IPyObject val, bool isStatic)> elements, string moduleName)
	{
		this.elements = elements;
		this.moduleName = moduleName;
	}

	public (IPyObject val, bool isStatic) Evaluate(string value, int wordStart, int wordEnd)
	{
		return Export(value, wordStart, wordEnd);
	}

	public void SetAttribute(string attribute, IPyObject value)
	{
		elements[attribute] = (value, false);
	}

	public (IPyObject val, bool isStatic) Export(string value, int wordStart, int wordEnd)
	{
		if (!elements.ContainsKey(value))
		{
			throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_invalid_const", moduleName + "." + value), wordStart, wordEnd);
		}
		return elements[value];
	}

	public override string ToString()
	{
		return "<Module " + moduleName + ">";
	}

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.ContainsKey(elements))
		{
			return (PyModule)copies[elements];
		}
		Dictionary<string, (IPyObject, bool)> dictionary = new Dictionary<string, (IPyObject, bool)>();
		PyModule pyModule = new PyModule(dictionary, moduleName);
		pyModule.fullyInitialized = fullyInitialized;
		copies[elements] = pyModule;
		foreach (KeyValuePair<string, (IPyObject, bool)> element in elements)
		{
			dictionary[element.Key] = (element.Value.Item1.DeepCopy(copies), element.Value.Item2);
		}
		return pyModule;
	}

	public int Size()
	{
		return elements.Count;
	}
}
