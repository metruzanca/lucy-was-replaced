using UnityEngine;

[CreateAssetMenu(fileName = "Drone", menuName = "ScriptableObjects/Drone", order = 2)]
public class DroneSO : ScriptableObject
{
	public AudioClip dirtSound;

	public AudioClip plantSound;

	public AudioClip harvestSound;

	public AudioClip pickupSound;

	public AudioClip waterSound;

	public AnimationCurve actionZCurve;

	public AnimationCurve flipZCuve;

	public AnimationCurve printZCurve;

	public AnimationCurve printXCurve;

	public AnimationCurve printRotationCurve;

	public AnimationCurve petMoveXYCurve;

	public AnimationCurve petMoveZCurve;

	public float extraPrintWidth;

	public float characterPrintWidth;

	public int charactersAtMaxPrintWidth;
}
