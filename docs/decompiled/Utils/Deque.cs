using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class Deque<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>
{
	private T[] values;

	private int start;

	private int end;

	private int size;

	public T this[int index]
	{
		get
		{
			if ((uint)index >= (uint)size)
			{
				throw new IndexOutOfRangeException();
			}
			return values[(start + index) % values.Length];
		}
		set
		{
			if ((uint)index >= (uint)size)
			{
				throw new IndexOutOfRangeException();
			}
			values[(start + index) % values.Length] = value;
		}
	}

	public int Count => size;

	public bool IsReadOnly => false;

	public Deque()
	{
		values = new T[0];
	}

	public Deque(int capacity)
	{
		values = new T[capacity];
	}

	public void Add(T item)
	{
		if (size == values.Length)
		{
			IncreaseSize();
		}
		if (end == values.Length)
		{
			end = 0;
		}
		values[end] = item;
		end++;
		size++;
	}

	public void Clear()
	{
		size = 0;
		start = 0;
		end = 0;
	}

	public bool Contains(T item)
	{
		throw new NotImplementedException();
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		throw new NotImplementedException();
	}

	public IEnumerator<T> GetEnumerator()
	{
		throw new NotImplementedException();
	}

	public int IndexOf(T item)
	{
		if (item == null)
		{
			for (int i = 0; i < size; i++)
			{
				if (values[(start + i) % values.Length] == null)
				{
					return i;
				}
			}
		}
		else
		{
			EqualityComparer<T> @default = EqualityComparer<T>.Default;
			for (int j = 0; j < size; j++)
			{
				if (@default.Equals(values[(start + j) % values.Length], item))
				{
					return j;
				}
			}
		}
		return -1;
	}

	public void Insert(int index, T item)
	{
		if (size == values.Length)
		{
			IncreaseSize();
		}
		if (index < values.Length / 2)
		{
			start = (start + values.Length - 1) % values.Length;
			values[start] = item;
			int num = start;
			for (int i = 0; i < index; i++)
			{
				int num2 = (num + 1) % values.Length;
				T val = values[num];
				values[num] = values[num2];
				values[num2] = val;
				num = num2;
			}
			size++;
			return;
		}
		if (end == values.Length)
		{
			end = 0;
		}
		values[end] = item;
		int num3 = end;
		for (int j = 0; j < size - index; j++)
		{
			int num4 = (num3 - 1 + values.Length) % values.Length;
			T val2 = values[num3];
			values[num3] = values[num4];
			values[num4] = val2;
			num3 = num4;
		}
		end++;
		size++;
	}

	public bool Remove(T item)
	{
		int num = IndexOf(item);
		if (num < 0)
		{
			return false;
		}
		if (num < size / 2)
		{
			for (int num2 = num; num2 > 0; num2--)
			{
				int num3 = (start + num2) % values.Length;
				int num4 = (num3 - 1 + values.Length) % values.Length;
				values[num3] = values[num4];
			}
			start = (start + 1) % values.Length;
		}
		else
		{
			for (int i = num; i < size - 1; i++)
			{
				int num5 = (start + i) % values.Length;
				int num6 = (num5 + 1) % values.Length;
				values[num5] = values[num6];
			}
			end = (end - 1 + values.Length) % values.Length;
		}
		size--;
		return true;
	}

	public void RemoveAt(int index)
	{
		throw new NotImplementedException();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		throw new NotImplementedException();
	}

	private void IncreaseSize()
	{
		T[] destinationArray = new T[Math.Max(1, values.Length * 2)];
		if (size > 0)
		{
			if (end > start)
			{
				Array.Copy(values, start, destinationArray, 0, size);
			}
			else
			{
				Array.Copy(values, start, destinationArray, 0, values.Length - start);
				Array.Copy(values, 0, destinationArray, values.Length - start, end);
			}
		}
		values = destinationArray;
		start = 0;
		end = size;
	}

	public void Enqueue(T item)
	{
		if (size == values.Length)
		{
			IncreaseSize();
		}
		start = (start + values.Length - 1) % values.Length;
		values[start] = item;
		size++;
	}

	public T Dequeue()
	{
		if (size == 0)
		{
			throw new InvalidOperationException("Tried to remove from an empty deque");
		}
		size--;
		end = (end - 1 + values.Length) % values.Length;
		return values[end];
	}

	public bool TryDequeue(out T item)
	{
		if (size == 0)
		{
			item = default(T);
			return false;
		}
		item = Dequeue();
		return true;
	}

	public T Peek()
	{
		return values[(end - 1 + values.Length) % values.Length];
	}

	public string DebugView()
	{
		StringBuilder stringBuilder = new StringBuilder("[");
		for (int i = 0; i < values.Length; i++)
		{
			if (start == i)
			{
				stringBuilder.Append("s");
			}
			if (end == i)
			{
				stringBuilder.Append("e");
			}
			stringBuilder.Append(values[i].ToString());
			stringBuilder.Append(',');
		}
		if (end == values.Length)
		{
			stringBuilder.Append("e");
		}
		stringBuilder.Append(']');
		stringBuilder.Append($", size: {size}");
		return stringBuilder.ToString();
	}
}
