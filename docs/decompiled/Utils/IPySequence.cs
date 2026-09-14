using System.Collections;
using System.Collections.Generic;

public interface IPySequence : IReadOnlyList<IPyObject>, IEnumerable<IPyObject>, IEnumerable, IReadOnlyCollection<IPyObject>, IPyObject, IPyIndexable
{
	bool Contains(IPyObject obj, ref int steps);
}
