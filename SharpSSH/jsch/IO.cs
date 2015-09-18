using System;
using System.IO;
using SharpSsh.java.io;

namespace SharpSsh.jsch
{
	public class IO
	{
		internal JStream m_ins;
		internal JStream m_outs;
		internal JStream m_outs_ext;

		private bool m_in_dontclose = false;
		private bool m_out_dontclose = false;
		private bool m_outs_ext_dontclose = false;

		public void setOutputStream(Stream outs)
		{
			if (outs != null)
				m_outs = new JStream(outs);
			else
				m_outs = null;
		}
		public void setOutputStream(Stream outs, bool dontclose)
		{
			m_out_dontclose = dontclose;
			setOutputStream(outs);
		}
		public void setExtOutputStream(Stream outs)
		{
			if (outs != null)
				m_outs_ext = new JStream(outs);
			else
				m_outs_ext = null;
		}
		public void setExtOutputStream(Stream outs, bool dontclose)
		{
			m_outs_ext_dontclose = dontclose;
			setExtOutputStream(outs);
		}
		public void setInputStream(Stream ins)
		{
			//ConsoleStream low buffer patch
			if (ins != null)
			{
				if (ins.GetType() == Type.GetType("System.IO.__ConsoleStream"))
					ins = new Streams.ProtectedConsoleStream(ins);
				else if (ins.GetType() == Type.GetType("System.IO.FileStream"))
					ins = new Streams.ProtectedConsoleStream(ins);
				m_ins = new JStream(ins);
			}
			else
				m_ins = null;
		}

		public void setInputStream(Stream ins, bool dontclose)
		{
			m_in_dontclose = dontclose;
			setInputStream(ins);
		}

		public void put(Packet p)
		{
			m_outs.Write(p.m_buffer.m_buffer, 0, p.m_buffer.m_index);
			m_outs.Flush();
		}
		internal void put(byte[] array, int begin, int length)
		{
			m_outs.Write(array, begin, length);
			m_outs.Flush();
		}
		internal void put_ext(byte[] array, int begin, int length)
		{
			m_outs_ext.Write(array, begin, length);
			m_outs_ext.Flush();
		}

		internal int getByte()
		{
			int res = m_ins.ReadByte() & 0xff;
			return res;
		}

		internal void getByte(byte[] array)
		{
			getByte(array, 0, array.Length);
		}

		internal void getByte(byte[] array, int begin, int length)
		{
			do
			{
				int completed = m_ins.Read(array, begin, length);
				if (completed <= 0)
					throw new IOException("End of IO Stream Read");
				begin += completed;
				length -= completed;
			}
			while (length > 0);
		}

		public void close()
		{
			try
			{
				if (m_ins != null && !m_in_dontclose)
					m_ins.Close();
				m_ins = null;
			}
			catch (Exception)
			{ }

			try
			{
				if (m_outs != null && !m_out_dontclose)
					m_outs.Close();
				m_outs = null;
			}
			catch (Exception)
			{ }

			try
			{
				if (m_outs_ext != null && !m_outs_ext_dontclose)
					m_outs_ext.Close();
				m_outs_ext = null;
			}
			catch (Exception)
			{ }
		}
	}
}
