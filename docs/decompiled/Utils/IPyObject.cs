using System.Collections.Generic;

public interface IPyObject
{
	IPyObject DeepCopy(Dictionary<object, object> copies);

	int Size()
	{
		return 1;
	}
}
