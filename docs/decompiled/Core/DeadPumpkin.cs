public class DeadPumpkin : FarmObject
{
	public override bool Harvestable => false;

	public override ItemBlock Harvest(out Duration collectionDuration)
	{
		sim.farm.grid.RemoveEntity(pos);
		return base.Harvest(out collectionDuration);
	}
}
