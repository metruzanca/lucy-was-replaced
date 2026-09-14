using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IPyIndexable
{
	IPyObject At(InternalPySequence indices, ref int steps, IPyObject valueToSet = null)
	{
		try
		{
			if (this is IPySequence pySequence)
			{
				if (!Enumerable.All<IPyObject>((IEnumerable<IPyObject>)indices, (Func<IPyObject, bool>)((IPyObject o) => o is PyNumber || o is PyNone)) && (indices.Count < 3 || !(indices[2] is PyNone)))
				{
					throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_index", indices[0], this));
				}
				double num = IndexFromDoubleOrNone(indices[0], pySequence, indices, isStartIndex: true);
				if (indices.Count == 1)
				{
					if (indices[0] is PyNone)
					{
						throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_index", indices[0], pySequence));
					}
					if (num < 0.0 || num >= (double)pySequence.Count || double.IsNaN(num))
					{
						throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_index_out_of_bounds", num, pySequence));
					}
					steps = 1;
					if (valueToSet != null)
					{
						if (!(pySequence is IList<IPyObject> list))
						{
							if (pySequence is PyTuple)
							{
								throw new ExecuteException("error_index_on_tuple");
							}
							throw new ExecuteException(CodeUtilities.LocalizeAndFormat("error_index", indices[0], pySequence));
						}
						list[(int)num] = valueToSet;
					}
					return pySequence[(int)num];
				}
				double val = IndexFromDoubleOrNone(indices[1], pySequence, indices, isStartIndex: false);
				double num2 = ((indices.Count == 3) ? ((double)(PyNumber)indices[2]) : 1.0);
				if (num2 == 0.0)
				{
					throw new ExecuteException("error_zero_step");
				}
				if (num2 > 0.0)
				{
					num = Math.Max(num, 0.0);
					val = Math.Min(val, pySequence.Count);
				}
				else
				{
					val = Math.Max(val, -1.0);
					num = Math.Min(num, pySequence.Count - 1);
				}
				List<IPyObject> list2 = new List<IPyObject>();
				for (double num3 = num; num3 >= 0.0 && num3 < (double)pySequence.Count && ((num2 >= 0.0 && num3 < val) || (num2 < 0.0 && num3 > val)); num3 += num2)
				{
					list2.Add(pySequence[(int)num3]);
				}
				steps = Mathf.Max(1, list2.Count);
				if (this is PyTuple)
				{
					return new PyTuple(list2);
				}
				if (this is PyString)
				{
					return new PyString(string.Join("", list2));
				}
				return new PyList(list2);
			}
			throw new NotImplementedException("At is not implemented for a non sequence indexable");
		}
		catch (IndexOutOfRangeException)
		{
			throw new ExecuteException("error_index_out_of_bounds2");
		}
	}

	static double IndexFromDoubleOrNone(IPyObject o, IPySequence list, InternalPySequence numbers, bool isStartIndex)
	{
		double num = ((!(o is PyNone)) ? ((double)(PyNumber)o) : ((double)(((numbers.Count == 3 && (int)(double)(PyNumber)numbers[2] < 0) == isStartIndex) ? list.Count : (-1))));
		if (num < 0.0 && !(o is PyNone))
		{
			num = (double)list.Count + num;
		}
		return num;
	}
}
