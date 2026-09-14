using UnityEngine;

public class Dinosaur : FarmObject
{
	public bool isInSnake = true;

	public bool canMoveTo;

	public Drone drone;

	public void FirstTailAnim(double ops)
	{
		scaleStart = new Vector3(1f, 0f, 1f);
		scaleEnd = Vector3.one;
		scaleDuration = sim.GetActionTime(ops);
		scaleEndTime = sim.CurrentTime + scaleDuration;
		Vector3 localPosition = base.LocalPosition;
		Vector3 vector = GridManager.CellToLocal(pos + base.Orientation.Reverse().GetDirectionVector()) - localPosition;
		positionStart = localPosition + 0.9f * vector;
		positionEnd = localPosition;
		moveDuration = scaleDuration;
		moveEndTime = sim.CurrentTime + scaleDuration;
	}

	public void LastTailAnim(double ops)
	{
		scaleEnd = new Vector3(1f, 0f, 1f);
		scaleStart = Vector3.one;
		scaleDuration = sim.GetActionTime(ops);
		scaleEndTime = sim.CurrentTime + scaleDuration;
		Vector3 localPosition = base.LocalPosition;
		Vector3 vector = GridManager.CellToLocal(pos + base.Orientation.GetDirectionVector()) - localPosition;
		positionStart = localPosition;
		positionEnd = localPosition + 0.1f * vector;
		moveDuration = scaleDuration;
		moveEndTime = sim.CurrentTime + scaleDuration;
	}

	public void DoubleTailAnim()
	{
		scaleEnd = new Vector3(0f, 0f, 0f);
	}

	public override bool CanDroneMoveToFrom(GridDirection direction)
	{
		return canMoveTo;
	}

	public override void OnRestart()
	{
		base.OnRestart();
		if (!(sim.farm.grid.grounds[pos] is Soil))
		{
			sim.farm.grid.SetGround(pos, "soil");
		}
	}

	public override void OnFree()
	{
		base.OnFree();
		if (isInSnake && drone.hat.hatSO.hatName == "dinosaur_hat")
		{
			drone.ChangeHat(ResourceManager.GetHat("straw_hat"), null);
		}
	}

	public override ItemBlock GetYield()
	{
		if (!isInSnake || !(drone.hat is DinosaurHat dinosaurHat))
		{
			return base.GetYield();
		}
		ItemBlock itemBlock = ItemBlock.CreateEmpty();
		itemBlock.AddItem(StringIds.BoneItemId, dinosaurHat.BoneCount());
		return itemBlock;
	}
}
