using System;

public class BreakStatement : Exception
{
	public int startIndex;

	public int endIndex;

	public BreakStatement(int startIndex, int endIndex)
	{
		this.startIndex = startIndex;
		this.endIndex = endIndex;
	}
}
