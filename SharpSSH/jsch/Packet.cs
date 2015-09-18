using System;

namespace SharpSsh.jsch
{
	public class Packet
	{
		private static Random m_random = null;
		internal Buffer m_buffer;
		internal byte[] m_tmp = new byte[4];

		internal static void setRandom(Random foo)
		{
			m_random = foo;
		}

		public Packet(Buffer buffer)
		{
			m_buffer = buffer;
		}

		public void reset()
		{
			m_buffer.m_index = 5;
		}

		internal void padding(int bsize)
		{
			uint len = (uint)m_buffer.m_index;
			int pad = (int)((-len) & (bsize - 1));
			if (pad < bsize)
				pad += bsize;

			len = (uint)(len + pad - 4);
			m_tmp[0] = (byte)(len >> 24);
			m_tmp[1] = (byte)(len >> 16);
			m_tmp[2] = (byte)(len >> 8);
			m_tmp[3] = (byte)(len);
			Array.Copy(m_tmp, 0, m_buffer.m_buffer, 0, 4);
			m_buffer.m_buffer[4] = (byte)pad;
			lock (m_random)
			{
				m_random.fill(m_buffer.m_buffer, m_buffer.m_index, pad);
			}
			m_buffer.skip(pad);
		}

		internal int shift(int len, int mac)
		{
			int dstIndex = len + 5 + 9;
			int pad = (-dstIndex) & 7;
			if (pad < 8) pad += 8;
			dstIndex += pad;
			dstIndex += mac;

			Array.Copy(
				m_buffer.m_buffer,
				len + 5 + 9,
				m_buffer.m_buffer,
				dstIndex,
				m_buffer.m_index - 5 - 9 - len
				);
			m_buffer.m_index = 10;
			m_buffer.putInt(len);
			m_buffer.m_index = len + 5 + 9;
			return dstIndex;
		}

		internal void unshift(byte command, int recipient, int srcIndex, int len)
		{
			Array.Copy(
				m_buffer.m_buffer,
				srcIndex,
				m_buffer.m_buffer,
				5 + 9,
				len
				);
			m_buffer.m_buffer[5] = command;
			m_buffer.m_index = 6;
			m_buffer.putInt(recipient);
			m_buffer.putInt(len);
			m_buffer.m_index = len + 5 + 9;
		}

	}
}
