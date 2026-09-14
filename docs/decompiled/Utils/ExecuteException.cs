using System;

public class ExecuteException : Exception
{
	public int startIndex;

	public int endIndex;

	public ExecuteException()
	{
	}

	public ExecuteException(string message, int startIndex = -1, int endIndex = -1)
		: base(message)
	{
		this.startIndex = startIndex;
		this.endIndex = endIndex;
	}
}
