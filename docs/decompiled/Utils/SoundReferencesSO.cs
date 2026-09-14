using FMODUnity;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundReferences", menuName = "ScriptableObjects/SoundReferences", order = 1)]
public class SoundReferencesSO : ScriptableObject
{
	public EventReference ButtonHovered;

	public EventReference ButtonPressed;

	public EventReference OnUnlock;

	public EventReference OnHarvestHay;

	public EventReference OnHarvestBush;

	public EventReference OnHarvestTree;

	public EventReference OnHarvestCarrot;

	public EventReference OnHarvestPumpkin;

	public EventReference OnHarvestSunflower;

	public EventReference OnHarvestCactus;

	public EventReference OnHarvestTreasureChest;

	public EventReference OnPlant;

	public EventReference OnTill;

	public EventReference OnItemPickup;

	public EventReference OnUseWater;

	public EventReference OnUseFertilizer;

	public EventReference OnSwapPlants;

	public EventReference OnSpawnMaze;

	public EventReference OnDinosaurEatApple;

	public EventReference OnDinosaurDeath;

	public EventReference Music;

	public EventReference Ambience;

	public EventReference DroneSound;

	public string musicVCA;

	public string ambienceVCA;

	public string sfxVCA;

	public string uiVCA;

	public string droneVCA;
}
