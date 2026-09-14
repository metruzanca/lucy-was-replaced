using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[Serializable]
public class ItemBlock
{
	[SerializeField]
	private SerializableItem[] serializeList;

	[NonSerialized]
	public double[] items;

	public ItemBlock()
	{
	}

	public ItemBlock(ItemBlock itemBlock)
	{
		if (itemBlock != null && itemBlock.items != null)
		{
			items = Enumerable.ToArray<double>((IEnumerable<double>)itemBlock.items);
		}
		else
		{
			items = new double[StringIds.NumItems];
		}
	}

	public ItemBlock(int itemId, double n)
	{
		items = new double[StringIds.NumItems];
		items[itemId] = n;
	}

	public static ItemBlock CreateEmpty()
	{
		return new ItemBlock
		{
			items = new double[StringIds.NumItems]
		};
	}

	public void Deserialize()
	{
		if (serializeList == null)
		{
			return;
		}
		items = new double[StringIds.NumItems];
		SerializableItem[] array = serializeList;
		for (int i = 0; i < array.Length; i++)
		{
			SerializableItem serializableItem = array[i];
			int num = StringIds.GetItemId(serializableItem.name);
			if (num < 0)
			{
				continue;
			}
			for (int j = 0; j < items.Length; j++)
			{
				if (items[num] == 0.0)
				{
					break;
				}
				num = (num + 1) % items.Length;
			}
			items[num] = serializableItem.nr;
		}
	}

	public void Serialize()
	{
		serializeList = new SerializableItem[Enumerable.Count<double>((IEnumerable<double>)items, (Func<double, bool>)((double it) => it > 0.0))];
		int num = 0;
		foreach (int item in ItemIds())
		{
			serializeList[num] = new SerializableItem(StringIds.GetItemName(item), items[item]);
			num++;
		}
	}

	public void Add(ItemBlock other)
	{
		if (other != null)
		{
			for (int i = 0; i < items.Length; i++)
			{
				items[i] += other.items[i];
			}
		}
	}

	public void AddItem(int itemId, double n)
	{
		items[itemId] += n;
	}

	public bool Remove(ItemBlock other)
	{
		if (other == null)
		{
			return true;
		}
		if (!Contains(other))
		{
			return false;
		}
		RemovePartially(other);
		return true;
	}

	public bool RemoveItem(int itemId, double n)
	{
		return Remove(itemId, n);
	}

	public bool Remove(int itemId, double n)
	{
		if (items[itemId] < n)
		{
			return false;
		}
		items[itemId] -= n;
		return true;
	}

	public void Clear()
	{
		for (int i = 0; i < items.Length; i++)
		{
			items[i] = 0.0;
		}
	}

	public void RemovePartially(ItemBlock other)
	{
		if (other != null)
		{
			for (int i = 0; i < items.Length; i++)
			{
				items[i] = Math.Max(0.0, items[i] - other.items[i]);
			}
		}
	}

	public void SetNumber(int itemId, double n)
	{
		items[itemId] = Math.Max(0.0, n);
	}

	public bool Contains(ItemBlock other)
	{
		if (other == null)
		{
			return true;
		}
		for (int i = 0; i < items.Length; i++)
		{
			if (items[i] < other.items[i])
			{
				return false;
			}
		}
		return true;
	}

	public bool Contains(int itemId, double n)
	{
		return items[itemId] >= n;
	}

	public double GetNumber(int itemId)
	{
		return items[itemId];
	}

	public bool IsEmpty()
	{
		return Enumerable.All<double>((IEnumerable<double>)items, (Func<double, bool>)((double it) => it == 0.0));
	}

	public void TruncateNumbers()
	{
		for (int i = 0; i < items.Length; i++)
		{
			items[i] = Math.Truncate(items[i]);
		}
	}

	public IEnumerable<int> ItemIds()
	{
		for (int itemId = 0; itemId < items.Length; itemId++)
		{
			if (items[itemId] > 0.0)
			{
				yield return itemId;
			}
		}
	}

	public static ItemBlock operator /(ItemBlock a, double b)
	{
		if (a == null)
		{
			return null;
		}
		if (b == 0.0)
		{
			return null;
		}
		ItemBlock itemBlock = CreateEmpty();
		for (int i = 0; i < itemBlock.items.Length; i++)
		{
			itemBlock.items[i] = a.items[i] / b;
		}
		return itemBlock;
	}

	public static ItemBlock operator *(ItemBlock a, double b)
	{
		if (a == null)
		{
			return null;
		}
		if (b == 0.0)
		{
			return null;
		}
		ItemBlock itemBlock = CreateEmpty();
		for (int i = 0; i < itemBlock.items.Length; i++)
		{
			itemBlock.items[i] = a.items[i] * b;
		}
		return itemBlock;
	}

	public static ItemBlock operator +(ItemBlock a, ItemBlock b)
	{
		ItemBlock itemBlock = new ItemBlock(a);
		itemBlock.Add(b);
		return itemBlock;
	}

	public static ItemBlock operator -(ItemBlock a, ItemBlock b)
	{
		ItemBlock itemBlock = new ItemBlock(a);
		itemBlock.RemovePartially(b);
		return itemBlock;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder("");
		for (int i = 0; i < items.Length; i++)
		{
			stringBuilder.Append("item: " + StringIds.GetItemName(i) + " / " + items[i]);
		}
		return stringBuilder.ToString();
	}
}
