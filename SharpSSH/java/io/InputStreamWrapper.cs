using SharpSsh.java.io;

namespace SharpSsh.Streams
{
	/// <summary>
	/// Summary description for InputStreamWrapper.
	/// </summary>
	public class InputStreamWrapper : InputStream
	{
		System.IO.Stream m_streams;
		public InputStreamWrapper(System.IO.Stream stream)
		{
			m_streams = stream;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return m_streams.Read(buffer, offset, count);
		}
	}
}
