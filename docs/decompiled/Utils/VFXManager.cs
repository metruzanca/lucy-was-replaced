using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
	private static VFXManager _instance;

	private Queue<float> effectStartTimes = new Queue<float>();

	[SerializeField]
	private GameObject dustPrefab;

	[SerializeField]
	private GameObject lightDustPrefab;

	[SerializeField]
	private GameObject hitPrefab;

	[SerializeField]
	private GameObject coinsPrefab;

	[SerializeField]
	private GameObject waterPrefab;

	[SerializeField]
	private GameObject heartsPrefab;

	[SerializeField]
	private GameObject starsPrefab;

	[SerializeField]
	private GameObject textPopupPrefab;

	[SerializeField]
	private GameObject printSignPrefab;

	private Dictionary<VFXType, List<GameObject>> effectPool = new Dictionary<VFXType, List<GameObject>>();

	private ConcurrentQueue<VFXCall> vfxCalls = new ConcurrentQueue<VFXCall>();

	private static VFXManager Inst => _instance;

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Object.Destroy(base.gameObject);
		}
		else
		{
			_instance = this;
		}
	}

	public static void EnqueueEffect(VFXType effect, Vector3 localPos, bool changeColor = false, Color color = default(Color), string text = null)
	{
		Inst.vfxCalls.Enqueue(new VFXCall(effect, localPos, changeColor, color, text));
	}

	private void Update()
	{
		VFXCall result;
		while (vfxCalls.TryDequeue(out result))
		{
			Play(result.effect, result.localPos, result.changeColor, result.color, result.text);
		}
	}

	public static float Play(VFXType effect, Vector3 localPos, bool changeColor = false, Color color = default(Color), string text = null)
	{
		GameObject effectObject = Inst.GetEffectObject(effect);
		if (effectObject == null)
		{
			return 0f;
		}
		effectObject.transform.localPosition = localPos;
		IVisualEffect component = effectObject.GetComponent<IVisualEffect>();
		if (changeColor)
		{
			component.SetColor(color);
		}
		float result = 0f;
		if (text != null)
		{
			result = component.SetText(text);
		}
		component.Play();
		return result;
	}

	private GameObject GetEffectObject(VFXType effect)
	{
		float result;
		while (effectStartTimes.TryPeek(out result) && Time.time - result > 1f)
		{
			effectStartTimes.Dequeue();
		}
		float @float = OptionHolder.GetFloat("vfx limit", 50f);
		float num = (float)effectStartTimes.Count / @float;
		float num2 = num * num;
		if (Random.Range(0f, 1f) < num2 && effect != VFXType.print_sign)
		{
			return null;
		}
		effectStartTimes.Enqueue(Time.time);
		if (!effectPool.ContainsKey(effect))
		{
			effectPool[effect] = new List<GameObject>();
		}
		foreach (GameObject item in effectPool[effect])
		{
			if (!item.GetComponent<IVisualEffect>().IsPlaying())
			{
				return item;
			}
		}
		GameObject gameObject = Object.Instantiate(GetPrefab(effect), base.transform).gameObject;
		effectPool[effect].Add(gameObject);
		return gameObject;
	}

	private GameObject GetPrefab(VFXType effect)
	{
		return effect switch
		{
			VFXType.dust => dustPrefab, 
			VFXType.light_dust => lightDustPrefab, 
			VFXType.hit => hitPrefab, 
			VFXType.coins => coinsPrefab, 
			VFXType.water => waterPrefab, 
			VFXType.hearts => heartsPrefab, 
			VFXType.stars => starsPrefab, 
			VFXType.text_popup => textPopupPrefab, 
			VFXType.print_sign => printSignPrefab, 
			_ => null, 
		};
	}
}
