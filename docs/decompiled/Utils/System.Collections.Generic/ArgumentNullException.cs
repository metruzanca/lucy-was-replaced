namespace System.Collections.Generic;

internal static class ArgumentNullException
{
	public static void ThrowIfNull(object o)
	{
		if (o == null)
		{
			throw new System.ArgumentNullException();
		}
	}
}
