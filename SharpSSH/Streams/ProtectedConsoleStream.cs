using System;
using System.IO;

namespace SharpSsh.Streams
{
	/// <summary>
	/// This class provide access to the console stream obtained by calling the
	/// Console.OpenStandardInput() and Console.OpenStandardOutput(), and prevents reading 
	/// into buffers to large for the Console Stream
	/// </summary>
	public class ProtectedConsoleStream : System.IO.Stream
	{
		private Stream m_stream;
		public ProtectedConsoleStream(Stream stream)
		{
			if ((stream.GetType() != Type.GetType("System.IO.__ConsoleStream")) &&
				(stream.GetType() != Type.GetType("System.IO.FileStream")))
			{
				throw new ArgumentException("Not ConsoleStream");
			}
			m_stream = stream;
		}

		//		public static Stream Protect(Stream s)
		//		{
		//			if(s.GetType() == Console.
		//		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (count > 256)
				count = 256;
			return m_stream.Read(buffer, offset, count);
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return m_stream.BeginRead(buffer, offset, count, callback, state);
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return m_stream.BeginWrite(buffer, offset, count, callback, state);
		}

		public override bool CanRead
		{
			get { return m_stream.CanRead; }
		}

		public override bool CanSeek
		{
			get { return m_stream.CanSeek; }
		}
		public override bool CanWrite
		{
			get { return m_stream.CanWrite; }
		}
		public override void Close()
		{
			m_stream.Close();
		}
		public override System.Runtime.Remoting.ObjRef CreateObjRef(Type requestedType)
		{
			return m_stream.CreateObjRef(requestedType);
		}
		public override int EndRead(IAsyncResult asyncResult)
		{
			return m_stream.EndRead(asyncResult);
		}
		public override void EndWrite(IAsyncResult asyncResult)
		{
			m_stream.EndWrite(asyncResult);
		}
		public override bool Equals(object obj)
		{
			return m_stream.Equals(obj);
		}
		public override void Flush()
		{
			m_stream.Flush();
		}
		public override int GetHashCode()
		{
			return m_stream.GetHashCode();
		}
		public override object InitializeLifetimeService()
		{
			return m_stream.InitializeLifetimeService();
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
		public override int ReadByte()
		{
			return m_stream.ReadByte();
		}
		public override long Seek(long offset, SeekOrigin origin)
		{
			return m_stream.Seek(offset, origin);
		}
		public override void SetLength(long value)
		{
			m_stream.SetLength(value);
		}
		public override string ToString()
		{
			return m_stream.ToString();
		}
		public override void Write(byte[] buffer, int offset, int count)
		{
			m_stream.Write(buffer, offset, count);
		}
		public override void WriteByte(byte value)
		{
			m_stream.WriteByte(value);
		}

	}
}
