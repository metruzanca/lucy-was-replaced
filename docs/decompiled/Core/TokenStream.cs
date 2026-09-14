using System.Collections.Generic;
using System.Linq;

public class TokenStream
{
	private LinkedList<Token> tokens = new LinkedList<Token>();

	private int lastStringIndex;

	public Token Current => tokens.First?.Value;

	public Token LookAhead => tokens.First?.Next?.Value;

	public Token LookAheadIgnoreNewlines
	{
		get
		{
			LinkedListNode<Token> linkedListNode = tokens.First;
			while (linkedListNode != null && linkedListNode.Value?.type == TokenType.NEW_LINE)
			{
				linkedListNode = linkedListNode.Next;
			}
			return linkedListNode?.Next?.Value;
		}
	}

	public Token Last => tokens.Last?.Value;

	public int CurrentStringEndIndex
	{
		get
		{
			if (Current == null)
			{
				return lastStringIndex;
			}
			return Current.startIndex + Current.value.Length;
		}
	}

	public int CurrentStringStartIndex
	{
		get
		{
			if (Current == null)
			{
				return lastStringIndex;
			}
			return Current.startIndex;
		}
	}

	public void Add(Token t)
	{
		tokens.AddLast(t);
		lastStringIndex = t.startIndex + t.value.Length;
	}

	public Token Consume(TokenType type = TokenType.NO_TOKEN, string error = null, bool moveErrorBack = false)
	{
		Token current = Current;
		if (current == null)
		{
			if (error == null)
			{
				throw new ParseException(string.Format(Localizer.Localize("error_unexpected_token"), type), lastStringIndex, lastStringIndex);
			}
			throw new ParseException(error, lastStringIndex, lastStringIndex);
		}
		if (type != 0 && current.type != type)
		{
			int num = ((current.type == TokenType.NEW_LINE) ? 1 : 0);
			if (error == null)
			{
				throw new ParseException(string.Format(Localizer.Localize("error_unexpected_token"), type), current.startIndex - (moveErrorBack ? 1 : 0) + num, current.startIndex + ((!moveErrorBack) ? current.value.Length : 0));
			}
			throw new ParseException(error, current.startIndex - (moveErrorBack ? 1 : 0) + num, current.startIndex + ((!moveErrorBack) ? current.value.Length : 0));
		}
		tokens.RemoveFirst();
		return current;
	}

	public void RemoveLast()
	{
		if (tokens.Last != null)
		{
			tokens.RemoveLast();
		}
		lastStringIndex = ((Last != null) ? (Last.startIndex + Last.value.Length) : 0);
	}

	public IEnumerator<Token> GetEnumerator()
	{
		return (IEnumerator<Token>)(object)tokens.GetEnumerator();
	}

	public IEnumerable<Token> IterateReverse()
	{
		for (LinkedListNode<Token> node = tokens.Last; node != null; node = node.Previous)
		{
			yield return node.Value;
		}
	}

	public List<Token> ToList()
	{
		return Enumerable.ToList<Token>((IEnumerable<Token>)tokens);
	}

	public override string ToString()
	{
		string text = "";
		using IEnumerator<Token> enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			Token current = enumerator.Current;
			text += $" {current.value} {current.type} |".Replace("\n", "\\n").Replace("\t", "\\t");
		}
		return text;
	}

	public Token GetLastNewLine(int pos)
	{
		LinkedListNode<Token> linkedListNode = TokenNodeAtPos(pos);
		for (linkedListNode = ((linkedListNode != null) ? linkedListNode.Previous : tokens.Last); linkedListNode != null; linkedListNode = linkedListNode.Previous)
		{
			if (linkedListNode.Value.type == TokenType.NEW_LINE)
			{
				return linkedListNode.Value;
			}
		}
		return null;
	}

	public Token GetTokenAtPos(int pos)
	{
		return TokenNodeAtPos(pos).Value;
	}

	private LinkedListNode<Token> TokenNodeAtPos(int pos)
	{
		LinkedListNode<Token> linkedListNode = tokens.First;
		while (linkedListNode != null && linkedListNode.Value.startIndex + linkedListNode.Value.value.Length <= pos)
		{
			linkedListNode = linkedListNode.Next;
		}
		return linkedListNode;
	}
}
