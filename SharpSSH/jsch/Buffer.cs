using System;
using System.Text;

namespace SharpSsh.jsch
{
	public class Buffer
	{
		static byte[] m_tmp = new byte[4];
		internal byte[] m_buffer;
		internal int m_index;
		internal int m_s;

		public Buffer(int size)
		{
			m_buffer = new byte[size];
			m_index = 0;
			m_s = 0;
		}
		public Buffer(byte[] buffer)
		{
			m_buffer = buffer;
			m_index = 0;
			m_s = 0;
		}
		public Buffer()
			: this(1024 * 10 * 2)
		{
		}

		public void putByte(byte foo)
		{
			m_buffer[m_index++] = foo;
		}
		public void putByte(byte[] foo)
		{
			putByte(foo, 0, foo.Length);
		}
		public void putByte(byte[] foo, int begin, int length)
		{
			Array.Copy(foo, begin, m_buffer, m_index, length);
			m_index += length;
		}
		public void putString(byte[] foo)
		{
			putString(foo, 0, foo.Length);
		}
		public void putString(string foo)
		{
			putString(Encoding.UTF8.GetBytes(foo));
		}

		public void putString(byte[] foo, int begin, int length)
		{
			putInt(length);
			putByte(foo, begin, length);
		}
		public void putInt(int v)
		{
			uint val = (uint)v;
			m_tmp[0] = (byte)(val >> 24);
			m_tmp[1] = (byte)(val >> 16);
			m_tmp[2] = (byte)(val >> 8);
			m_tmp[3] = (byte)(val);
			Array.Copy(m_tmp, 0, m_buffer, m_index, 4);
			m_index += 4;
		}
		public void putLong(long v)
		{
			ulong val = (ulong)v;
			m_tmp[0] = (byte)(val >> 56);
			m_tmp[1] = (byte)(val >> 48);
			m_tmp[2] = (byte)(val >> 40);
			m_tmp[3] = (byte)(val >> 32);
			Array.Copy(m_tmp, 0, m_buffer, m_index, 4);
			m_tmp[0] = (byte)(val >> 24);
			m_tmp[1] = (byte)(val >> 16);
			m_tmp[2] = (byte)(val >> 8);
			m_tmp[3] = (byte)(val);
			Array.Copy(m_tmp, 0, m_buffer, m_index + 4, 4);
			m_index += 8;
		}
		internal void skip(int n)
		{
			m_index += n;
		}
		internal void putPad(int n)
		{
			while (n > 0)
			{
				m_buffer[m_index++] = (byte)0;
				n--;
			}
		}
		public void putMPInt(byte[] foo)
		{
			int i = foo.Length;
			if ((foo[0] & 0x80) != 0)
			{
				i++;
				putInt(i);
				putByte(0);
			}
			else
				putInt(i);
			putByte(foo);
		}

		public int Length { get { return m_index - m_s; } }

		public int OffSet
		{
			get { return m_s; }
			set { m_s = value; }
		}

		public long getLong()
		{
			long foo = getInt() & 0xffffffffL;
			foo = ((foo << 32)) | (getInt() & 0xffffffffL);
			return foo;
		}

		public int getInt()
		{
			return (int)((((uint)getShort() << 16) & 0xffff0000) | ((uint)getShort() & 0xffff));
		}

		internal int getShort()
		{
			return (((getByte() << 8) & 0xff00) | (getByte() & 0xFF));
		}

		public int getByte()
		{
			return (m_buffer[m_s++] & 0xFF);
		}

		public void getByte(byte[] foo)
		{
			getByte(foo, 0, foo.Length);
		}

		void getByte(byte[] foo, int start, int len)
		{
			Array.Copy(m_buffer, m_s, foo, start, len);
			m_s += len;
		}

		public int getByte(int len)
		{
			int foo = m_s;
			m_s += len;
			return foo;
		}

		public byte[] getMPInt()
		{
			int i = getInt();
			byte[] foo = new byte[i];
			getByte(foo, 0, i);
			return foo;
		}

		public byte[] getMPIntBits()
		{
			int bits = getInt();
			int bytes = (bits + 7) / 8;
			byte[] foo = new byte[bytes];
			getByte(foo, 0, bytes);
			if ((foo[0] & 0x80) != 0)
			{
				byte[] bar = new byte[foo.Length + 1];
				bar[0] = 0; // ??
				Array.Copy(foo, 0, bar, 1, foo.Length);
				foo = bar;
			}
			return foo;
		}

		public byte[] getString()
		{
			int i = getInt();
			byte[] foo = new byte[i];
			getByte(foo, 0, i);
			return foo;
		}

		internal byte[] getString(int[] start, int[] len)
		{
			int i = getInt();
			start[0] = getByte(i);
			len[0] = i;
			return m_buffer;
		}

		public void reset()
		{
			m_index = 0;
			m_s = 0;
		}

		public void shift()
		{
			if (m_s == 0) return;
			Array.Copy(m_buffer, m_s, m_buffer, 0, m_index - m_s);
			m_index = m_index - m_s;
			m_s = 0;
		}

		internal void rewind()
		{
			m_s = 0;
		}
	}
}
