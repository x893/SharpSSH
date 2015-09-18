using System;
using IO = System.IO;

namespace SharpSsh.java.io
{
	/// <summary>
	/// Summary description for Stream.
	/// </summary>
	public class JStream : IO.Stream
	{
		private IO.Stream m_stream;

		public IO.Stream Stream
		{
			get { return m_stream; }
		}

		public JStream(IO.Stream stream)
		{
			m_stream = stream;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return m_stream.Read(buffer, offset, count);
		}

		public override int ReadByte()
		{
			return m_stream.ReadByte();
		}

		public void close()
		{
			Close();
		}

		public override void Close()
		{
			m_stream.Close();
		}

		public override void WriteByte(byte value)
		{
			m_stream.WriteByte(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			m_stream.Write(buffer, offset, count);
		}

		public void Write(byte[] buffer)
		{
			Write(buffer, 0, buffer.Length);
		}
		public void Write(string msg)
		{
			Write(StringEx.getBytes(msg));
		}

		public override bool CanRead
		{
			get { return m_stream.CanRead; }
		}

		public override bool CanWrite
		{
			get { return m_stream.CanWrite; }
		}

		public override bool CanSeek
		{
			get { return m_stream.CanSeek; }
		}

		public override void Flush()
		{
			m_stream.Flush();
		}
		public override long Length
		{
			get { return m_stream.Length; }
		}

		public override long Position
		{
			get { return m_stream.Position; }
			set { m_stream.Position = value; }
		}

		public override void SetLength(long value)
		{
			m_stream.SetLength(value);
		}
		public override long Seek(long offset, IO.SeekOrigin origin)
		{
			return m_stream.Seek(offset, origin);
		}

		public long Skip(long len)
		{
			//Seek doesn't work
			//return Seek(offset, IO.SeekOrigin.Current);
			int i = 0;
			int count = 0;
			byte[] buf = new byte[len];
			while (len > 0)
			{
				i = Read(buf, count, (int)len);	//tamir: possible lost of pressision
				if (i <= 0)
					throw new Exception("inputstream is closed");
				count += i;
				len -= i;
			}
			return count;
		}

		public int Available()
		{
			if (m_stream is Streams.PipedInputStream)
				return ((Streams.PipedInputStream)m_stream).available();
			throw new Exception("JStream.available() -- Method not implemented");
		}

		public void flush()
		{
			m_stream.Flush();
		}
	}
}
