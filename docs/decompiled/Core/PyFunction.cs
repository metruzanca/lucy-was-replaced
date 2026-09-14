using System;
using System.Collections.Generic;

public class PyFunction : IPyObject
{
	public string functionName;

	public Node syntaxTree;

	public Scope parentScope;

	public Func<List<IPyObject>, Simulation, Execution, int, double> binding;

	public IPyObject methodObject;

	public bool isFree;

	public PyFunction(string functionName, Node syntaxTree, Scope parentScope, Func<List<IPyObject>, Simulation, Execution, int, double> binding, IPyObject methodObject, bool isFree)
	{
		this.functionName = functionName;
		this.syntaxTree = syntaxTree;
		this.parentScope = parentScope;
		this.binding = binding;
		this.methodObject = methodObject;
		this.isFree = isFree;
	}

	public PyFunction(string functionName, Func<List<IPyObject>, Simulation, Execution, int, double> binding, IPyObject methodObject = null, bool isFree = false)
	{
		this.functionName = functionName;
		syntaxTree = null;
		parentScope = null;
		this.methodObject = methodObject;
		this.isFree = isFree;
		this.binding = binding;
	}

	public PyFunction(string functionName, Node syntaxTree = null, Scope parentScope = null)
	{
		this.functionName = functionName;
		this.syntaxTree = syntaxTree;
		this.parentScope = parentScope;
		methodObject = null;
		isFree = false;
		binding = null;
	}

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		if (copies.ContainsKey(this))
		{
			return (PyFunction)copies[this];
		}
		PyFunction pyFunction2 = (PyFunction)(copies[this] = new PyFunction(functionName, null, null, binding, methodObject, isFree));
		pyFunction2.syntaxTree = syntaxTree?.DeepCopy(copies);
		pyFunction2.parentScope = parentScope?.DeepCopy(copies);
		pyFunction2.methodObject = methodObject?.DeepCopy(copies);
		return pyFunction2;
	}

	public override string ToString()
	{
		return functionName;
	}
}
