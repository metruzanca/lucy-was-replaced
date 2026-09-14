using System.Collections.Generic;

public class Program
{
	public Node syntaxTree;

	public HashSet<string> globalVars;

	public HashSet<string> allVars;

	public HashSet<string> importedModules = new HashSet<string>();

	public Program(Node syntaxTree, HashSet<string> globalVars, HashSet<string> allVars, HashSet<string> importedModules)
	{
		this.syntaxTree = syntaxTree;
		this.globalVars = globalVars;
		this.allVars = allVars;
		this.importedModules = importedModules;
	}
}
