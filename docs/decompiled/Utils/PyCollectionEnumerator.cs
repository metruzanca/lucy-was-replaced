using System;
using System.Collections;
using System.Collections.Generic;

public class PyCollectionEnumerator : IEnumerator<IPyObject>, IEnumerator, IDisposable
{
	private IEnumerator<IPyObject> keys;

	private int errorStartIndex = -1;

	private int errorEndIndex = -1;

	public IPyObject Current => keys.Current;

	object IEnumerator.Current => keys.Current;

	public PyCollectionEnumerator(ICollection<IPyObject> collection)
	{
		keys = collection.GetEnumerator();
	}

	public void SetErrorIndices(int startIndex, int endIndex)
	{
		errorStartIndex = startIndex;
		errorEndIndex = endIndex;
	}

	public void Dispose()
	{
	}

	public bool MoveNext()
	{
		try
		{
			return keys.MoveNext();
		}
		catch (InvalidOperationException)
		{
			throw new ExecuteException("error_collection_changed_size_during_iteration", errorStartIndex, errorEndIndex);
		}
	}

	public void Reset()
	{
		keys.Reset();
	}
}
