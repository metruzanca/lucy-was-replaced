public class PyBool : PyNumber
{
	public PyBool(bool b)
		: base(b ? 1 : 0)
	{
	}

	public override string ToString()
	{
		if (num == 0.0)
		{
			return "False";
		}
		return "True";
	}
}
