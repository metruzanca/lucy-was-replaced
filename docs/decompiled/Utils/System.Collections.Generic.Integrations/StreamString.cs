using System.IO;
using System.Text;

namespace System.Collections.Generic.Integrations;

public class StreamString
{
	private Stream ioStream;

	private UnicodeEncoding streamEncoding;

	public StreamString(Stream ioStream)
	{
		this.ioStream = ioStream;
		streamEncoding = new UnicodeEncoding();
	}

	public string ReadString()
	{
		int num = ioStream.ReadByte() * 256;
		num += ioStream.ReadByte();
		byte[] array = new byte[num];
		ioStream.Read(array, 0, num);
		return streamEncoding.GetString(array);
	}

	public int WriteString(string outString)
	{
		byte[] bytes = streamEncoding.GetBytes(outString);
		int num = bytes.Length;
		if (num > 65535)
		{
			num = 65535;
		}
		ioStream.WriteByte((byte)(num / 256));
		ioStream.WriteByte((byte)(num & 0xFF));
		ioStream.Write(bytes, 0, num);
		ioStream.Flush();
		return bytes.Length + 2;
	}
}
