public interface IPyNameSpace : IPyObject
{
	(IPyObject val, bool isStatic) Evaluate(string value, int wordStart, int wordEnd);
}
