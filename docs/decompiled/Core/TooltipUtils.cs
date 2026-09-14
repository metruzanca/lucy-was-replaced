using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public static class TooltipUtils
{
	private static Dictionary<string, string> keywordDocs;

	public static TooltipInfo ItemTooltip(string itemName)
	{
		int itemId = StringIds.GetItemId(itemName.ToLower());
		if (itemId < 0)
		{
			return null;
		}
		ItemSO item = ResourceManager.GetItem(itemId);
		if (item == null)
		{
			return null;
		}
		double numItem = MainSim.Inst.GetNumItem(itemId);
		string text = "";
		string text2 = "\n\n" + string.Format(Localizer.Localize("item_tooltip_template_amount"), "`" + numItem.ToString("0.##", CultureInfo.InvariantCulture) + "`");
		return new TooltipInfo($"## {CodeUtilities.ToUpperSnake(item.itemName)}\n{Localizer.Localize(item.description)}{text2}{text}", 0f, default(Vector3), TooltipInfo.Anchor.Auto, item.docs);
	}

	public static TooltipInfo FarmObjectTooltip(string objectName)
	{
		FarmObjectSO farmObject = ResourceManager.GetFarmObject(objectName);
		if (farmObject == null || !MainSim.Inst.IsUnlocked(objectName))
		{
			return null;
		}
		string text = ((farmObject.cost == null || farmObject.cost.IsEmpty()) ? "" : ("\n\n" + string.Format(Localizer.Localize("object_tooltip_template_cost"), farmObject.objectName)));
		string text2 = "";
		return new TooltipInfo($"## {CodeUtilities.ToUpperSnake(farmObject.objectName)}\n{Localizer.Localize(farmObject.GetDescription())}{text2}{text}", 0f, default(Vector3), TooltipInfo.Anchor.Auto, farmObject.docs);
	}

	public static TooltipInfo UnlockTooltip(string unlockName)
	{
		UnlockSO unlock = ResourceManager.GetUnlock(unlockName);
		if (unlock == null)
		{
			return null;
		}
		string docs = (MainSim.Inst.IsUnlocked(unlock.unlockName) ? unlock.docs : "");
		int num = MainSim.Inst.NumUnlocked(unlock.unlockName);
		string text;
		if (num > unlock.maxUnlockLevel)
		{
			text = "Upgrade level is above the maximum. Stats and Achievements are disabled while this is the case. Click to reset to the max level.";
		}
		else if (unlock.IsMultiUnlock && num > 0)
		{
			text = Localizer.Localize(unlock.multiUnlockDescr);
			int num2 = num;
			bool flag = num2 >= unlock.maxUnlockLevel;
			switch (unlock.multiUnlockDescrMode)
			{
			case MultiUnlockDescrMode.AdditivePercent:
				text = string.Format(flag ? "{0}\n{1}%" : "{0}\n{1}% => {2}%", text, Mathf.Pow(unlock.additivePercentFactor, num2 - 1) * (100f + unlock.additivePercentStart), Mathf.Pow(unlock.additivePercentFactor, num2) * (100f + unlock.additivePercentStart));
				break;
			case MultiUnlockDescrMode.GridSize:
			{
				string format = (flag ? "{0}\n{1}x{2}" : "{0}\n{1}x{2} => {3}x{3}");
				Vector2Int worldSize = MainSim.Inst.GetWorldSize();
				int num3 = Helper.WorldSizeScale(num2 + 1);
				text = string.Format(format, text, worldSize.x, worldSize.y, num3);
				break;
			}
			case MultiUnlockDescrMode.Per10Seconds:
			{
				string format2 = (flag ? "{0}\n{1}/s" : "{0}\n{1}/s => {2}/s");
				float num4 = (float)(1 << Mathf.Max(0, num2 - 1)) * 0.1f;
				text = string.Format(format2, text, num4, num4 * 2f);
				break;
			}
			case MultiUnlockDescrMode.Megafarm:
				text = string.Format(flag ? "{0}\n{1}" : "{0}\n{1} => {2}", text, Helper.NumDrones(num2), Helper.NumDrones(num2 + 1));
				break;
			}
		}
		else
		{
			text = Localizer.Localize(unlock.description);
		}
		string arg = "";
		ItemBlock unlockCost = MainSim.Inst.GetUnlockCost(unlock);
		if (unlockCost != null && !unlockCost.IsEmpty())
		{
			arg = "\n\n" + string.Format(Localizer.Localize("unlock_tooltip_template_cost"), unlock.unlockName);
		}
		text = $"## {Localizer.Localize(unlock.unlockName)}\n{text}{arg}";
		return new TooltipInfo(text, 0f, Vector3.zero, TooltipInfo.Anchor.Auto, docs);
	}

	public static TooltipInfo HatTooltip(string hatName)
	{
		if (ResourceManager.GetHat(hatName) == null)
		{
			return null;
		}
		return new TooltipInfo(Localizer.Localize("hat_descr_" + hatName.ToLower()));
	}

	public static TooltipInfo LeaderboardTooltip(string lbName)
	{
		if (ResourceManager.GetLeaderboard(lbName) == null)
		{
			return null;
		}
		return new TooltipInfo(Localizer.Localize("lb_descr_" + lbName.ToLower()));
	}

	public static string GetKeywordDocsPage(string keyword)
	{
		if (keywordDocs == null)
		{
			keywordDocs = new Dictionary<string, string>();
			foreach (UnlockSO allUnlock in ResourceManager.GetAllUnlocks())
			{
				foreach (string unlock in allUnlock.unlocks)
				{
					if (Localizer.LoadDoc("docs/scripting/" + unlock) != "")
					{
						keywordDocs[unlock] = "docs/scripting/" + unlock;
					}
					else
					{
						keywordDocs[unlock] = allUnlock.docs;
					}
				}
			}
		}
		return keywordDocs.GetValueOrDefault(keyword, "");
	}

	public static TooltipInfo KeywordTooltip(string word)
	{
		if (!MainSim.Inst.IsUnlocked(word))
		{
			return null;
		}
		string text = "code_tooltip_" + word;
		string text2 = Localizer.Localize(text);
		if (text2 == text)
		{
			return null;
		}
		string text3 = $"## `{word}`\n{text2}";
		string docs = ((!BuiltinFunctions.Functions.ContainsKey(word) && !BuiltinFunctions.Methods.ContainsKey(word)) ? GetKeywordDocsPage(word) : ("functions/" + word));
		return new TooltipInfo(text3, 0f, default(Vector3), TooltipInfo.Anchor.Auto, docs);
	}

	public static TooltipInfo GetWordTooltip(string word, CodeWindow context)
	{
		if (word.Contains('.'))
		{
			string[] array = word.Split('.');
			word = Enumerable.Last<string>((IEnumerable<string>)array).ToLower();
			switch (Enumerable.First<string>((IEnumerable<string>)array))
			{
			case "Entities":
				return FarmObjectTooltip(word);
			case "Grounds":
				return FarmObjectTooltip(word);
			case "Items":
				return ItemTooltip(word);
			case "Unlocks":
				return UnlockTooltip(word);
			case "Hats":
				return HatTooltip(word);
			case "Leaderboards":
				return LeaderboardTooltip(word);
			}
		}
		TooltipInfo tooltipInfo = KeywordTooltip(word);
		if (tooltipInfo != null)
		{
			return tooltipInfo;
		}
		tooltipInfo = ItemTooltip(word);
		if (tooltipInfo != null)
		{
			return tooltipInfo;
		}
		tooltipInfo = FarmObjectTooltip(word);
		if (tooltipInfo != null)
		{
			return tooltipInfo;
		}
		tooltipInfo = UnlockTooltip(word);
		if (tooltipInfo != null)
		{
			return tooltipInfo;
		}
		FunctionNode functionNode = null;
		foreach (CodeWindow value in context.workspace.codeWindows.Values)
		{
			if (value.parsedFunctions.ContainsKey(word))
			{
				functionNode = value.parsedFunctions.GetValueOrDefault(word);
			}
		}
		if (functionNode != null)
		{
			return new TooltipInfo(CodeUtilities.ApplyCodeTags("`" + functionNode.GetSignature() + "`"));
		}
		return null;
	}
}
