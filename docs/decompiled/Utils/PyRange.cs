using System;
using System.Collections;
using System.Collections.Generic;

public class PyRange : IPySequence, IReadOnlyList<IPyObject>, IEnumerable<IPyObject>, IEnumerable, IReadOnlyCollection<IPyObject>, IPyObject, IPyIndexable
{
	public class RangeEnumerator : IEnumerator<IPyObject>, IEnumerator, IDisposable
	{
		private double i;

		private double start;

		private double end;

		private double step;

		public IPyObject Current => new PyNumber(i);

		object IEnumerator.Current => new PyNumber(i);

		public RangeEnumerator(double start, double end, double step)
		{
			this.start = start;
			this.end = end;
			this.step = step;
			i = start - step;
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			i += step;
			if (!(step >= 0.0))
			{
				return end < i;
			}
			return i < end;
		}

		public void Reset()
		{
			i = start - step;
		}
	}

	private double start;

	private double end;

	private double step;

	public int Count => (int)Math.Ceiling(Math.Max((end - start) / step, 0.0));

	public IPyObject this[int index]
	{
		get
		{
			if (index < 0 || index >= Count)
			{
				throw new IndexOutOfRangeException();
			}
			return new PyNumber(start + (double)index * step);
		}
	}

	public PyRange(double start, double end, double step)
	{
		if (Math.Abs(step) < 0.01)
		{
			throw new ExecuteException("error_zero_step_size");
		}
		this.start = start;
		this.end = end;
		this.step = step;
	}

	public PyRange(double start, double end)
		: this(start, end, 1.0)
	{
	}

	public PyRange(double end)
		: this(0.0, end, 1.0)
	{
	}

	public bool Contains(IPyObject obj, ref int steps)
	{
		steps += 8;
		if (!(obj is PyNumber))
		{
			return false;
		}
		double num = ((PyNumber)obj).num;
		if (step > 0.0)
		{
			if (num >= start && num < end)
			{
				return (num - start) % step == 0.0;
			}
			return false;
		}
		if (num <= start && num > end)
		{
			return (start - num) % step == 0.0;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is PyRange))
		{
			return false;
		}
		PyRange pyRange = (PyRange)obj;
		if (pyRange.Count != Count)
		{
			return false;
		}
		if (Count == 0)
		{
			return true;
		}
		if (pyRange[0].Equals(this[0]))
		{
			return pyRange[Count - 1].Equals(this[Count - 1]);
		}
		return false;
	}

	public override int GetHashCode()
	{
		int num = 1430287;
		num = (num * 7302013) ^ Count.GetHashCode();
		if (Count > 0)
		{
			num = (num * 7302013) ^ this[0].GetHashCode();
			num = (num * 7302013) ^ this[Count - 1].GetHashCode();
		}
		return num;
	}

	public IEnumerator<IPyObject> GetEnumerator()
	{
		return new RangeEnumerator(start, end, step);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public override string ToString()
	{
		if (step == 1.0)
		{
			return $"range({start},{end})";
		}
		return $"range({start},{end},{step})";
	}

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		return this;
	}
}
