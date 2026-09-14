using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DropdownTooltips : MonoBehaviour, ITooltipHandler
{
	[SerializeField]
	private TMP_Dropdown dropdown;

	[NonSerialized]
	public List<string> descriptions = new List<string>();

	[NonSerialized]
	public List<ItemBlock> costs;

	private int index;

	public void OnItemHovered(GameObject item)
	{
		index = item.transform.GetSiblingIndex() - 1;
	}

	public TooltipInfo GetTooltipInfo(Action updateTooltipCallback)
	{
		return null;
	}

	public void TooltipGone()
	{
	}
}
