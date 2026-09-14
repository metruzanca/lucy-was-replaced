using System;

public class ParseException : Exception
{
	public int startIndex;

	public int endIndex;

	public ParseException(string message, int startIndex, int endIndex)
		: base(message)
	{
		this.startIndex = startIndex;
		this.endIndex = endIndex;
	}
}
