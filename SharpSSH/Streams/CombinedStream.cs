using System;
using System.IO;

namespace SharpSsh.Streams
{
	/// <summary>
	/// Summary description for CombinedStream.
	/// </summary>
	public class CombinedStream : Stream
	{
		private Stream m_in;
		private Stream m_out;

		public CombinedStream(Stream inputStream, Stream outputStream)
		{
			m_in = inputStream;
			m_out = outputStream;
		}

		/// <summary>
		/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count- 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream. </param>
		/// <param name="count">The maximum number of bytes to be read from the current stream. </param>
		/// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			return m_in.Read(buffer, offset, count);
		}

		/// <summary>
		/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count- 1) replaced by the bytes read from the current source.</param>
		/// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
		public virtual int Read(byte[] buffer)
		{
			return Read(buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
		/// </summary>
		/// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
		public override int ReadByte()
		{
			return m_in.ReadByte();
		}

		/// <summary>
		/// Writes a byte to the current position in the stream and advances the position within the stream by one byte.
		/// </summary>
		/// <param name="value">The byte to write to the stream. </param>
		public override void WriteByte(byte value)
		{
			m_out.WriteByte(value);
		}

		/// <summary>
		/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream. </param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream. </param>
		public override void Write(byte[] buffer, int offset, int count)
		{
			m_out.Write(buffer, offset, count);
		}

		/// <summary>
		/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream. </param>
		public virtual void Write(byte[] buffer)
		{
			Write(buffer, 0, buffer.Length);
		}


		/// <summary>
		/// Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.
		/// </summary>
		public override void Close()
		{
			try
			{
				base.Close();
				m_in.Close();
				m_out.Close();
			}
			catch { }
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports reading.
		/// </summary>
		public override bool CanRead
		{
			get { return m_in.CanRead; }
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports writing.
		/// </summary>
		public override bool CanWrite
		{
			get { return m_out.CanWrite; }
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports seeking. This stream cannot seek, and will always return false.
		/// </summary>
		public override bool CanSeek
		{
			get { return false; }
		}

		/// <summary>
		/// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
		/// </summary>
		public override void Flush()
		{
			m_out.Flush();
		}

		/// <summary>
		/// Gets the length in bytes of the stream.
		/// </summary>
		public override long Length
		{
			get { return 0; }
		}

		/// <summary>
		/// Gets or sets the position within the current stream. This Stream cannot seek. This property has no effect on the Stream and will always return 0.
		/// </summary>
		public override long Position
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		/// <summary>
		/// This method has no effect on the Stream.
		/// </summary>
		public override void SetLength(long value)
		{
		}

		/// <summary>
		/// This method has no effect on the Stream.
		/// </summary>
		public override long Seek(long offset, SeekOrigin origin)
		{
			return 0;
		}
	}
}
