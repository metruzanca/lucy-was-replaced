using System;

public class ReturnStatement : Exception
{
	public int startIndex;

	public int endIndex;

	public ReturnStatement(int startIndex, int endIndex)
	{
		this.startIndex = startIndex;
		this.endIndex = endIndex;
	}
}
