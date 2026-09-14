public class Ground : FarmObject
{
	public override string ToString()
	{
		return "Grounds." + CodeUtilities.ToUpperSnake(objectSO.objectName);
	}
}
