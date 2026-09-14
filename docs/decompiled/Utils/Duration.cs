using System;

public struct Duration : IComparable<Duration>
{
	public long nanoseconds;

	public double Seconds => (double)nanoseconds / 1000000000.0;

	public Duration(long nanoseconds)
	{
		this.nanoseconds = nanoseconds;
	}

	public static Duration operator +(Duration a, Duration b)
	{
		return new Duration(a.nanoseconds + b.nanoseconds);
	}

	public static Duration operator -(Duration a, Duration b)
	{
		return new Duration(a.nanoseconds - b.nanoseconds);
	}

	public static Duration operator *(Duration a, double b)
	{
		return new Duration((long)((double)a.nanoseconds * b));
	}

	public static Duration operator /(Duration a, double b)
	{
		return new Duration((long)((double)a.nanoseconds / b));
	}

	public static double operator /(Duration a, Duration b)
	{
		return (double)a.nanoseconds / (double)b.nanoseconds;
	}

	public static bool operator ==(Duration a, Duration b)
	{
		return a.nanoseconds == b.nanoseconds;
	}

	public static bool operator !=(Duration a, Duration b)
	{
		return a.nanoseconds != b.nanoseconds;
	}

	public static bool operator <(Duration a, Duration b)
	{
		return a.nanoseconds < b.nanoseconds;
	}

	public static bool operator >(Duration a, Duration b)
	{
		return a.nanoseconds > b.nanoseconds;
	}

	public static bool operator >=(Duration a, Duration b)
	{
		return a.nanoseconds >= b.nanoseconds;
	}

	public static bool operator <=(Duration a, Duration b)
	{
		return a.nanoseconds <= b.nanoseconds;
	}

	public static Duration FromSeconds(double seconds)
	{
		return new Duration((long)(seconds * 1000000000.0));
	}

	public override bool Equals(object obj)
	{
		if (obj is Duration)
		{
			return nanoseconds.Equals(((Duration)obj).nanoseconds);
		}
		return base.Equals(obj);
	}

	public static Duration Min(Duration a, Duration b)
	{
		return new Duration(Math.Min(a.nanoseconds, b.nanoseconds));
	}

	public override int GetHashCode()
	{
		return nanoseconds.GetHashCode();
	}

	public TimeSpan ToTimeSpan()
	{
		return TimeSpan.FromTicks(nanoseconds / 100);
	}

	public override string ToString()
	{
		return Seconds.ToString();
	}

	public int CompareTo(Duration other)
	{
		if (nanoseconds < other.nanoseconds)
		{
			return -1;
		}
		if (nanoseconds > other.nanoseconds)
		{
			return 1;
		}
		return 0;
	}
}
