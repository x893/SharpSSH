using System;
using IO = System.IO;

namespace SharpSsh.java.io
{
	/// <summary>
	/// Summary description for FileInputStream.
	/// </summary>
	public class FileOutputStream : OutputStream
	{
		private IO.FileStream m_fileStream;

		public FileOutputStream(string file)
			: this(file, false)
		{
		}

		public FileOutputStream(File file)
			: this(file.Info.Name, false)
		{
		}

		public FileOutputStream(string file, bool append)
		{
			if (append)
				m_fileStream = new IO.FileStream(file, IO.FileMode.Append);
			else
				m_fileStream = new IO.FileStream(file, IO.FileMode.Create);
		}

		public FileOutputStream(File file, bool append)
			: this(file.Info.Name)
		{
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			m_fileStream.Write(buffer, offset, count);
		}

		public override void Flush()
		{
			m_fileStream.Flush();
		}

		public override void Close()
		{
			m_fileStream.Close();
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
