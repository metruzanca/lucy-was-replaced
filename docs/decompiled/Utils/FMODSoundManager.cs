using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public static class FMODSoundManager
{
	private static SoundReferencesSO soundReferencesSO;

	private static bool gameStarted;

	private static EventInstance music;

	private static EventInstance ambience;

	private static List<EventInstance> droneSounds;

	public static void PlaySound(SoundEffectType effectType, Vector3 position)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		switch (effectType)
		{
		case SoundEffectType.HarvestGrass:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnHarvestHay, position);
			break;
		case SoundEffectType.HarvestBush:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnHarvestBush, position);
			break;
		case SoundEffectType.HarvestTree:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnHarvestTree, position);
			break;
		case SoundEffectType.HarvestCarrot:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnHarvestCarrot, position);
			break;
		case SoundEffectType.HarvestPumpkin:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnHarvestPumpkin, position);
			break;
		case SoundEffectType.HarvestSunflower:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnHarvestSunflower, position);
			break;
		case SoundEffectType.HarvestCactus:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnHarvestCactus, position);
			break;
		case SoundEffectType.HarvestTreasureChest:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnHarvestTreasureChest, position);
			break;
		case SoundEffectType.Plant:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnPlant, position);
			break;
		case SoundEffectType.Till:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnTill, position);
			break;
		case SoundEffectType.PickUpItem:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnItemPickup, position);
			break;
		case SoundEffectType.UseWater:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnUseWater, position);
			break;
		case SoundEffectType.UseFertilizer:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnUseFertilizer, position);
			break;
		case SoundEffectType.SwapPlants:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnSwapPlants, position);
			break;
		case SoundEffectType.SpawnMaze:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnSpawnMaze, position);
			break;
		case SoundEffectType.DinosaurEatApple:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnDinosaurEatApple, position);
			break;
		case SoundEffectType.DinosaurDie:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnDinosaurDeath, position);
			break;
		case SoundEffectType.Unlock:
			RuntimeManager.PlayOneShot(soundReferencesSO.OnUnlock, position);
			break;
		case SoundEffectType.ButtonHovered:
			RuntimeManager.PlayOneShot(soundReferencesSO.ButtonHovered, position);
			break;
		case SoundEffectType.ButtonPressed:
			RuntimeManager.PlayOneShot(soundReferencesSO.ButtonPressed, position);
			break;
		}
	}

	public static void GameStart(SoundReferencesSO scriptableObjectInstance)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		if (!gameStarted)
		{
			gameStarted = true;
			soundReferencesSO = scriptableObjectInstance;
			music = RuntimeManager.CreateInstance(soundReferencesSO.Music);
			ambience = RuntimeManager.CreateInstance(soundReferencesSO.Ambience);
			((EventInstance)(ref ambience)).start();
			((EventInstance)(ref music)).start();
			droneSounds = new List<EventInstance> { CreateDroneSoundInstance() };
		}
	}

	private static EventInstance CreateDroneSoundInstance()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		EventInstance result = RuntimeManager.CreateInstance(soundReferencesSO.DroneSound);
		((EventInstance)(ref result)).start();
		return result;
	}

	public static void zoomLevelField(float zoom)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		System studioSystem = RuntimeManager.StudioSystem;
		((System)(ref studioSystem)).setParameterByName("zoom", zoom, false);
	}

	public static void StopMusic()
	{
	}

	public static void OpenMenu()
	{
	}

	public static void CloseMenu()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		((EventInstance)(ref music)).stop((STOP_MODE)0);
	}

	public static void UpdateDroneParams((float droneSpeed, Vector3 position)[] drones)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		EventInstance val;
		if (drones.Length < droneSounds.Count)
		{
			for (int i = drones.Length; i < droneSounds.Count; i++)
			{
				val = droneSounds[i];
				((EventInstance)(ref val)).stop((STOP_MODE)0);
				val = droneSounds[i];
				((EventInstance)(ref val)).release();
			}
			droneSounds.RemoveRange(drones.Length, droneSounds.Count - drones.Length);
		}
		else if (drones.Length > droneSounds.Count)
		{
			for (int j = droneSounds.Count; j < drones.Length; j++)
			{
				droneSounds.Add(CreateDroneSoundInstance());
			}
		}
		for (int k = 0; k < drones.Length; k++)
		{
			val = droneSounds[k];
			((EventInstance)(ref val)).set3DAttributes(RuntimeUtils.To3DAttributes(drones[k].position));
			val = droneSounds[k];
			((EventInstance)(ref val)).setParameterByName("droneSpeed", drones[k].droneSpeed, false);
		}
	}

	public static void SetGameSpeedParam(float gameSpeed)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		System studioSystem = RuntimeManager.StudioSystem;
		((System)(ref studioSystem)).setParameterByName("gameSpeed", gameSpeed, false);
	}

	public static void SetMusicVCAVolume(float volume)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		VCA vCA = RuntimeManager.GetVCA(soundReferencesSO.musicVCA);
		((VCA)(ref vCA)).setVolume(volume);
	}

	public static void SetAmbienceVCAVolume(float volume)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		VCA vCA = RuntimeManager.GetVCA(soundReferencesSO.ambienceVCA);
		((VCA)(ref vCA)).setVolume(volume);
	}

	public static void SetSFXVCAVolume(float volume)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		VCA vCA = RuntimeManager.GetVCA(soundReferencesSO.sfxVCA);
		((VCA)(ref vCA)).setVolume(volume);
	}

	public static void SetUIVCAVolume(float volume)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		VCA vCA = RuntimeManager.GetVCA(soundReferencesSO.uiVCA);
		((VCA)(ref vCA)).setVolume(volume);
	}

	public static void SetDroneVCAVolume(float volume)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		VCA vCA = RuntimeManager.GetVCA(soundReferencesSO.droneVCA);
		((VCA)(ref vCA)).setVolume(volume);
	}
}
