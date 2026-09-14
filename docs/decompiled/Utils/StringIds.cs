using System.Collections.Generic;
using System.Linq;

public static class StringIds
{
	private static Dictionary<string, int> itemIds;

	private static string[] itemNames;

	public static int NumItems => itemIds.Count;

	public static int PowerItemId { get; private set; }

	public static int WeirdSubstanceItemId { get; private set; }

	public static int CactusItemId { get; private set; }

	public static int WaterItemId { get; private set; }

	public static int FertilizerItemId { get; private set; }

	public static int BoneItemId { get; private set; }

	public static int GetItemId(string item)
	{
		if (itemIds == null || !itemIds.ContainsKey(item))
		{
			return -1;
		}
		return itemIds[item];
	}

	public static string GetItemName(int itemId)
	{
		return itemNames[itemId];
	}

	public static void SetItemIds(IEnumerable<string> items)
	{
		itemNames = Enumerable.ToArray<string>(items);
		itemIds = new Dictionary<string, int>();
		for (int i = 0; i < itemNames.Length; i++)
		{
			itemIds[itemNames[i]] = i;
			switch (itemNames[i])
			{
			case "power":
				PowerItemId = i;
				break;
			case "weird_substance":
				WeirdSubstanceItemId = i;
				break;
			case "cactus":
				CactusItemId = i;
				break;
			case "water":
				WaterItemId = i;
				break;
			case "fertilizer":
				FertilizerItemId = i;
				break;
			case "bone":
				BoneItemId = i;
				break;
			}
		}
	}
}
