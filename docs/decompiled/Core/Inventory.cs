using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory : MonoBehaviour
{
	[SerializeField]
	private ItemUI itemUIPrefab;

	[SerializeField]
	private Transform container;

	private Dictionary<int, ItemUI> itemUIs = new Dictionary<int, ItemUI>();

	private Func<ItemBlock> getItemBlock;

	private void Update()
	{
		ItemBlock itemBlock = getItemBlock();
		if (itemBlock == null)
		{
			return;
		}
		foreach (int item in Enumerable.Union<int>(itemBlock.ItemIds(), (IEnumerable<int>)itemUIs.Keys))
		{
			UpdateItem(item, itemBlock.GetNumber(item));
		}
	}

	public void SetUp(Func<ItemBlock> getItemBlock)
	{
		foreach (ItemUI value in itemUIs.Values)
		{
			UnityEngine.Object.Destroy(value.gameObject);
		}
		itemUIs.Clear();
		this.getItemBlock = getItemBlock;
		ItemBlock itemBlock = getItemBlock();
		if (itemBlock == null)
		{
			return;
		}
		foreach (int item in itemBlock.ItemIds())
		{
			AddItem(item, itemBlock.GetNumber(item));
		}
	}

	private void AddItem(int itemId, double n)
	{
		ItemUI itemUI = UnityEngine.Object.Instantiate(itemUIPrefab, container);
		itemUI.Setup(itemId, n);
		itemUIs[itemId] = itemUI;
	}

	private void UpdateItem(int itemId, double n)
	{
		if (itemUIs.ContainsKey(itemId))
		{
			itemUIs[itemId].UpdateCount(n);
		}
		else
		{
			AddItem(itemId, n);
		}
	}
}
