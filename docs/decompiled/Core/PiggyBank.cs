using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class PiggyBank : MonoBehaviour
{
	private struct ActiveEffect
	{
		public ItemEffect effect;

		public float speed;

		public float zVelocity;

		public ActiveEffect(ItemEffect effect, float zVelocity, float speed)
		{
			this.effect = effect;
			this.zVelocity = zVelocity;
			this.speed = speed;
		}
	}

	[SerializeField]
	private float maxSpeed;

	[SerializeField]
	private float acceleration;

	[SerializeField]
	private float accelerationTowardsCursor;

	[SerializeField]
	private float maxRotation;

	[SerializeField]
	private float maxDistance;

	[SerializeField]
	private float effectAcceleration;

	[SerializeField]
	private float effectStartZVelocity;

	[SerializeField]
	private float effectGravity;

	[SerializeField]
	private float effectMaxSpeed;

	[SerializeField]
	private float effectShrinkSpeed;

	[SerializeField]
	private AnimationCurve throwZCurve;

	[SerializeField]
	private Transform piggyMesh;

	[SerializeField]
	private Camera cam;

	[SerializeField]
	private Transform slot;

	[SerializeField]
	private AudioClip sellSound;

	[SerializeField]
	private ItemEffect itemEffectPrefab;

	private Stack<ItemEffect> itemPool = new Stack<ItemEffect>();

	private List<ActiveEffect> activeEffects = new List<ActiveEffect>();

	private Vector3 velLocal;

	private ConcurrentQueue<(int, Vector3)> collectQueue = new ConcurrentQueue<(int, Vector3)>();

	public Vector3 Position => piggyMesh.localPosition;

	private void Update()
	{
		(int, Vector3) result;
		while (collectQueue.TryDequeue(out result))
		{
			CollectItem(result.Item1, result.Item2);
		}
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		Vector3 position = (Vector3)(-(Vector2)ray.direction * ((ray.origin.z - piggyMesh.position.z) / ray.direction.z)) + ray.origin;
		position = MainSim.Inst.sceneScaler.InverseTransformPoint(position);
		position.z = piggyMesh.localPosition.z;
		bool flag = (piggyMesh.localPosition - position).magnitude < maxDistance;
		Vector3 vector = (flag ? position : MainSim.Inst.GetDroneMeshes()[0].Item3.GetPosition()) - piggyMesh.localPosition;
		vector.z = 0f;
		velLocal += vector * Time.deltaTime * (flag ? accelerationTowardsCursor : acceleration);
		velLocal = velLocal.normalized * Mathf.Min(velLocal.magnitude, maxSpeed);
		piggyMesh.localPosition += velLocal * Time.deltaTime;
		Quaternion to = Quaternion.AngleAxis(Mathf.Atan2(velLocal.y, velLocal.x) * 57.29578f + 180f, Vector3.forward);
		piggyMesh.localRotation = Quaternion.RotateTowards(piggyMesh.localRotation, to, maxRotation * Time.deltaTime);
		for (int num = activeEffects.Count - 1; num >= 0; num--)
		{
			ActiveEffect value = activeEffects[num];
			Vector3 vector2 = piggyMesh.position - value.effect.transform.position;
			if (vector2.magnitude < value.speed * Time.deltaTime)
			{
				EndCollect(activeEffects[num].effect);
				activeEffects.RemoveAt(num);
			}
			else
			{
				value.effect.transform.position += (value.speed * vector2.normalized + Vector3.forward * value.zVelocity) * Time.deltaTime;
				float num2 = Mathf.Max(value.effect.transform.localScale.x - effectShrinkSpeed * Time.deltaTime, 0.2f);
				value.effect.transform.localScale = new Vector3(num2, num2, num2);
				value.speed = Mathf.Min(value.speed + effectAcceleration * Time.deltaTime, effectMaxSpeed);
				value.zVelocity = Mathf.Max(value.zVelocity - effectGravity * Time.deltaTime, 0f);
				activeEffects[num] = value;
			}
		}
	}

	public void EnqueueCollect(int item, Vector3 localPos)
	{
		collectQueue.Enqueue((item, localPos));
	}

	private void CollectItem(int itemId, Vector3 localPos)
	{
		float @float = OptionHolder.GetFloat("vfx limit", 50f);
		float num = (float)activeEffects.Count / @float;
		float num2 = num * num;
		if (!(Random.Range(0f, 1f) < num2))
		{
			ItemEffect itemEffect = ((itemPool.Count <= 0) ? Object.Instantiate(itemEffectPrefab, base.transform) : itemPool.Pop());
			itemEffect.Setup(itemId);
			itemEffect.transform.position = MainSim.Inst.sceneScaler.TransformPoint(localPos);
			itemEffect.transform.localScale = Vector3.one;
			itemEffect.gameObject.SetActive(value: true);
			activeEffects.Add(new ActiveEffect(itemEffect, effectStartZVelocity, 0f));
		}
	}

	private void EndCollect(ItemEffect obj)
	{
		obj.gameObject.SetActive(value: false);
		itemPool.Push(obj);
		VFXManager.Play(VFXType.coins, slot.localPosition + piggyMesh.localPosition);
		LeanTween.cancel(piggyMesh.gameObject);
		piggyMesh.localScale = Vector3.one;
		LeanTween.scale(piggyMesh.gameObject, Vector3.one * 1.2f, 0.1f).setLoopPingPong(1);
		FMODSoundManager.PlaySound(SoundEffectType.PickUpItem, piggyMesh.position / MainSim.Inst.sceneScaler.localScale.x);
	}
}
