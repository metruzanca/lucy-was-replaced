using System;
using System.Collections.Generic;

public class PyNumber : IPyObject
{
	public double num;

	public PyNumber(double num)
	{
		this.num = num;
	}

	public static implicit operator double(PyNumber py)
	{
		return py.num;
	}

	public static PyNumber Modulo(PyNumber lhs, PyNumber rhs)
	{
		double num = (double)lhs % (double)rhs;
		return new PyNumber((num * (double)rhs < 0.0) ? (num + (double)rhs) : num);
	}

	public static PyNumber FloorDivision(PyNumber lhs, PyNumber rhs)
	{
		return new PyNumber(Math.Floor((double)lhs / (double)rhs));
	}

	public override bool Equals(object obj)
	{
		if (!(obj is PyNumber))
		{
			return false;
		}
		return ((PyNumber)obj).num == num;
	}

	public override int GetHashCode()
	{
		return num.GetHashCode();
	}

	public IPyObject DeepCopy(Dictionary<object, object> copies)
	{
		return this;
	}
}
