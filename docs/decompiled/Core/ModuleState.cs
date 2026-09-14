using System.Collections.Generic;

public class ModuleState
{
	public Scope globalScope;

	public Stack<Scope> callStack = new Stack<Scope>();

	public IPyObject returnValue;

	public bool isExpressionStatic;

	public Node currentExecutingNode;
}
