using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FarmObject", menuName = "ScriptableObjects/FarmObject", order = 1)]
public class FarmObjectSO : ScriptableObject, IPyObject
{
	[Header("Farm Object")]
	public string className;

	public bool isGround;

	public string objectName;

	public string description;

	public string docs;

	public Color color;

	public List<Mesh> meshes;

	public List<Mesh> gooMeshes;

	public bool canBeSwapped;

	public List<string> placeableOn;

	public SoundEffectType harvestSound;

	[Header("Growable")]
	public bool verticalGrowth;

	public float meanGrowTime;

	public float growTimeDeviationPercent;

	public bool canHaveCompanion;

	public bool randomRotation = true;

	public bool canBePlanted = true;

	public bool canBeOverplanted;

	[Header("Drops")]
	public string dropItem;

	[NonSerialized]
	public int dropItemId;

	public double dropAmount = 1.0;

	public string yieldUpgradeName;

	public ItemBlock cost;

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		return this;
	}

	public string GetDescription()
	{
		if (meanGrowTime > 0f && className != "Apple")
		{
			float num = meanGrowTime * (1f - growTimeDeviationPercent);
			float num2 = meanGrowTime * (1f + growTimeDeviationPercent);
			return string.Format(Localizer.Localize("plant_tooltip_template"), Localizer.Localize(description), num, num2, string.Join(" or ", placeableOn));
		}
		return Localizer.Localize(description);
	}

	public override string ToString()
	{
		if (isGround)
		{
			return "Grounds." + CodeUtilities.ToUpperSnake(objectName);
		}
		return "Entities." + CodeUtilities.ToUpperSnake(objectName);
	}
}
