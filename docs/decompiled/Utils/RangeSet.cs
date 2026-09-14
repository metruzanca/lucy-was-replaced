using System.Collections.Generic;

public class RangeSet
{
	private LinkedList<int> intervals = new LinkedList<int>();

	public bool AddRange(int start, int length)
	{
		LinkedListNode<int>? first = intervals.First;
		LinkedListNode<int> linkedListNode = ((first != null && first.Value <= start) ? intervals.First : null);
		LinkedListNode<int>? first2 = intervals.First;
		bool flag = first2 != null && first2.Value <= start;
		while (linkedListNode?.Next != null && linkedListNode.Next.Value <= start)
		{
			linkedListNode = linkedListNode.Next;
			flag = !flag;
		}
		if (flag)
		{
			return false;
		}
		if ((linkedListNode == null && intervals.Count > 0 && start + length > intervals.First.Value) || (linkedListNode?.Next != null && start + length > linkedListNode.Next.Value))
		{
			return false;
		}
		if (linkedListNode == null)
		{
			intervals.AddFirst(start + length);
			intervals.AddFirst(start);
			return true;
		}
		if (linkedListNode.Next != null && linkedListNode.Next.Value == start + length)
		{
			intervals.Remove(linkedListNode.Next);
		}
		else
		{
			intervals.AddAfter(linkedListNode, start + length);
		}
		if (linkedListNode.Value == start)
		{
			intervals.Remove(linkedListNode);
		}
		else
		{
			intervals.AddAfter(linkedListNode, start);
		}
		return true;
	}
}
