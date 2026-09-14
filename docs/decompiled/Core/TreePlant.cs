public class TreePlant : Growable
{
	protected override double GrowTimeFactor
	{
		get
		{
			FarmObject[] neighbors = sim.farm.grid.GetNeighbors(pos);
			double num = 1.0;
			FarmObject[] array = neighbors;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i]?.objectSO.objectName == "tree")
				{
					num *= 2.0;
				}
			}
			return base.GrowTimeFactor * num;
		}
	}

	public override void OnRestart()
	{
		base.OnRestart();
		UpdateNeighbors();
	}
}
