using System.Diagnostics;

namespace System.Collections.Generic;

internal sealed class PriorityQueueDebugView<TElement, TPriority>
{
	private readonly PriorityQueue<TElement, TPriority> _queue;

	private readonly bool _sort;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public (TElement Element, TPriority Priority)[] Items
	{
		get
		{
			List<(TElement, TPriority)> list = new List<(TElement, TPriority)>(_queue.UnorderedItems);
			if (_sort)
			{
				list.Sort(((TElement Element, TPriority Priority) i1, (TElement Element, TPriority Priority) i2) => _queue.Comparer.Compare(i1.Priority, i2.Priority));
			}
			return list.ToArray();
		}
	}

	public PriorityQueueDebugView(PriorityQueue<TElement, TPriority> queue)
	{
		ArgumentNullException.ThrowIfNull(queue);
		_queue = queue;
		_sort = true;
	}

	public PriorityQueueDebugView(PriorityQueue<TElement, TPriority>.UnorderedItemsCollection collection)
	{
		_queue = collection?._queue ?? throw new System.ArgumentNullException("collection");
	}
}
