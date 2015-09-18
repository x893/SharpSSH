using System;
using IO = System.IO;

namespace SharpSsh.java.io
{
	/// <summary>
	/// Summary description for FileInputStream.
	/// </summary>
	public class FileInputStream : InputStream
	{
		private IO.FileStream m_fileStream;

		public FileInputStream(string file)
		{
			m_fileStream = IO.File.OpenRead(file);
		}

		public FileInputStream(File file)
			: this(file.Info.Name)
		{
		}

		public override void Close()
		{
			m_fileStream.Close();
		}


		public override int Read(byte[] buffer, int offset, int count)
		{
			return m_fileStream.Read(buffer, offset, count);
		}

		public override bool CanSeek
		{
			get { return m_fileStream.CanSeek; }
		}

		public override long Seek(long offset, IO.SeekOrigin origin)
		{
			return m_fileStream.Seek(offset, origin);
		}
	}
}
