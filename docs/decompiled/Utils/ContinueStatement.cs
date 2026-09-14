using System;

public class ContinueStatement : Exception
{
	public int startIndex;

	public int endIndex;

	public ContinueStatement(int startIndex, int endIndex)
	{
		this.startIndex = startIndex;
		this.endIndex = endIndex;
	}
}
