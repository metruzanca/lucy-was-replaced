using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleEffect : MonoBehaviour, IVisualEffect
{
	private ParticleSystem particles;

	private void Awake()
	{
		particles = GetComponent<ParticleSystem>();
	}

	public bool IsPlaying()
	{
		return particles.isPlaying;
	}

	public void Play()
	{
		particles.Play();
	}

	public void SetColor(Color color)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		MainModule main = particles.main;
		((MainModule)(ref main)).startColor = MinMaxGradient.op_Implicit(color);
	}

	public float SetText(string text)
	{
		return 0f;
	}
}
