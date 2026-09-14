using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
	private struct AudioCall
	{
		public AudioClip clip;

		public float volume;

		public float pitch;

		public AudioCall(AudioClip clip, float volume, float pitch)
		{
			this.clip = clip;
			this.volume = volume;
			this.pitch = pitch;
		}
	}

	private struct AudioVolumePair
	{
		public AudioSource source;

		public float volume;

		public AudioVolumePair(AudioSource source)
		{
			this.source = source;
			volume = 1f;
		}
	}

	private static SoundManager _instance;

	[SerializeField]
	private Transform sceneScaler;

	private Transform cameraTransform;

	private List<AudioVolumePair> audioSources = new List<AudioVolumePair>();

	private int numPlaying;

	private ConcurrentQueue<(SoundEffectType, Vector3)> audioCalls = new ConcurrentQueue<(SoundEffectType, Vector3)>();

	private static SoundManager Inst => _instance;

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
		FMODSoundManager.GameStart(Resources.Load<SoundReferencesSO>("SoundReferences"));
		cameraTransform = Camera.main.transform;
	}

	public static void EnqueueSound(SoundEffectType sound, Vector3 position)
	{
		Inst.audioCalls.Enqueue((sound, position));
	}

	private void Update()
	{
		(SoundEffectType, Vector3) result;
		while (audioCalls.TryDequeue(out result))
		{
			Vector3 position = ZoomAdjustedPosition(result.Item2);
			_ = Inst.cameraTransform.position;
			FMODSoundManager.PlaySound(result.Item1, position);
		}
	}

	public static Vector3 ZoomAdjustedPosition(Vector3 localPosition)
	{
		Vector3 localPosition2 = Inst.cameraTransform.localPosition;
		Vector3 vector = Inst.sceneScaler.TransformPoint(localPosition) - localPosition2;
		vector /= Inst.sceneScaler.localScale.x;
		vector *= 0.8f;
		return Inst.cameraTransform.parent.TransformPoint(localPosition2 + vector);
	}

	public static void PlaySound(AudioClip sound, float volume = 1f, float pitch = 1f)
	{
		int emptyAudioSource = Inst.GetEmptyAudioSource();
		if (emptyAudioSource < 0)
		{
			return;
		}
		AudioVolumePair value = Inst.audioSources[emptyAudioSource];
		value.source.volume = volume * Random.Range(0.9f, 1.1f);
		value.source.pitch = pitch * Random.Range(0.8f, 1.2f);
		value.source.clip = sound;
		value.volume = volume * OptionHolder.GetFloat("volume", 1f);
		Inst.audioSources[emptyAudioSource] = value;
		foreach (AudioVolumePair audioSource in Inst.audioSources)
		{
			audioSource.source.volume = audioSource.volume * Mathf.Pow(OptionHolder.GetFloat("volume damping", 0.98f), Inst.numPlaying);
		}
		value.source.Play();
	}

	private int GetEmptyAudioSource()
	{
		numPlaying = 0;
		int num = -1;
		for (int i = 0; i < audioSources.Count; i++)
		{
			if (!audioSources[i].source.isPlaying)
			{
				num = i;
			}
			else
			{
				numPlaying++;
			}
		}
		float @float = OptionHolder.GetFloat("sfx limit", 50f);
		float num2 = (float)numPlaying / @float;
		float num3 = num2 * num2;
		if (Random.Range(0f, 1f) < num3)
		{
			return -1;
		}
		if (num == -1)
		{
			AddAudioSource();
			return audioSources.Count - 1;
		}
		return num;
	}

	private void AddAudioSource()
	{
		AudioSource val = base.gameObject.AddComponent<AudioSource>();
		val.playOnAwake = false;
		audioSources.Add(new AudioVolumePair(val));
	}
}
