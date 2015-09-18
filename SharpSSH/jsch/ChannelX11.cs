using System;
using System.Net;
using System.Net.Sockets;
using SharpSsh.java.lang;
using System.Collections;

namespace SharpSsh.jsch
{
	internal class ChannelX11 : Channel
	{
		private const int LOCAL_WINDOW_SIZE_MAX = 0x20000;
		private const int LOCAL_MAXIMUM_PACKET_SIZE = 0x4000;

		private static string m_host = "127.0.0.1";
		private static int m_port = 6000;
		private static byte[] m_cookie = null;
		private static byte[] m_table = { (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', 0x61, 0x62, 0x63, 0x64, 0x65, 0x66 };
		private static byte[] m_cookie_hex = null;
		private static Hashtable m_faked_cookie_pool = new Hashtable();
		private static Hashtable m_faked_cookie_hex_pool = new Hashtable();

		private bool m_init = true;
		private Socket m_socket = null;

		internal static byte[] Cookie { set { m_cookie = value; } }

		internal static int revtable(byte foo)
		{
			for (int i = 0; i < m_table.Length; i++)
				if (m_table[i] == foo)
					return i;
			return 0;
		}

		internal static void setCookie(string foo)
		{
			m_cookie_hex = Util.getBytes(foo);
			m_cookie = new byte[16];
			for (int i = 0; i < 16; i++)
				m_cookie[i] = (byte)(((revtable(m_cookie_hex[i * 2]) << 4) & 0xf0) | ((revtable(m_cookie_hex[i * 2 + 1])) & 0xf));
		}

		internal static byte[] getFakedCookie(Session session)
		{
			lock (m_faked_cookie_hex_pool)
			{
				byte[] foo = (byte[])m_faked_cookie_hex_pool[session];
				if (foo == null)
				{
					Random random = Session.m_random;
					foo = new byte[16];
					lock (random)
					{
						random.fill(foo, 0, 16);
					}
					m_faked_cookie_pool.Add(session, foo);
					byte[] bar = new byte[32];
					for (int i = 0; i < 16; i++)
					{
						bar[2 * i] = m_table[(foo[i] >> 4) & 0xf];
						bar[2 * i + 1] = m_table[(foo[i]) & 0xf];
					}
					m_faked_cookie_hex_pool.Add(session, bar);
					foo = bar;
				}
				return foo;
			}
		}

		internal ChannelX11() : base()
		{

			LocalWindowSizeMax = LOCAL_WINDOW_SIZE_MAX;
			LocalWindowSize = LOCAL_WINDOW_SIZE_MAX;
			LocalPacketSize = LOCAL_MAXIMUM_PACKET_SIZE;

			m_type = Util.getBytes("x11");
			try
			{
				IPEndPoint ep = new IPEndPoint(Dns.GetHostEntry(m_host).AddressList[0], m_port);
				m_socket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, 1);
				m_socket.Connect(ep);
				m_io = new IO();
				NetworkStream ns = new NetworkStream(m_socket);
				m_io.setInputStream(ns);
				m_io.setOutputStream(ns);
			}
			catch (Exception) { }
		}

		public override void Run()
		{
			m_thread = Thread.currentThread();
			Buffer buf = new Buffer(m_rmpsize);
			Packet packet = new Packet(buf);
			int i = 0;
			try
			{
				while (m_thread != null)
				{
					i = m_io.m_ins.Read(buf.m_buffer,
						14,
						buf.m_buffer.Length - 14 - 16 - 20 // padding and mac
						);
					if (i <= 0)
					{
						eof();
						break;
					}
					if (m_close) break;
					packet.reset();
					buf.putByte((byte)Session.SSH_MSG_CHANNEL_DATA);
					buf.putInt(m_recipient);
					buf.putInt(i);
					buf.skip(i);
					m_session.write(packet, this, i);
				}
			}
			catch { }
			m_thread = null;
		}

		internal override void write(byte[] foo, int s, int l)
		{
			if (m_init)
			{
				int plen = (foo[s + 6] & 0xff) * 256 + (foo[s + 7] & 0xff);
				int dlen = (foo[s + 8] & 0xff) * 256 + (foo[s + 9] & 0xff);

				if ((foo[s] & 0xff) == 0x42)
				{
				}
				else if ((foo[s] & 0xff) == 0x6c)
				{
					plen = (int)(((uint)plen >> 8) & 0xff) | ((plen << 8) & 0xff00);
					dlen = (int)(((uint)dlen >> 8) & 0xff) | ((dlen << 8) & 0xff00);
				}

				byte[] bar = new byte[dlen];
				Array.Copy(foo, s + 12 + plen + ((-plen) & 3), bar, 0, dlen);
				byte[] faked_cookie = (byte[])m_faked_cookie_pool[m_session];

				if (equals(bar, faked_cookie))
				{
					if (m_cookie != null)
						Array.Copy(m_cookie, 0, foo, s + 12 + plen + ((-plen) & 3), dlen);
				}
				else
				{
					throw new Exception("wrong cookie");
				}
				m_init = false;
			}
			m_io.put(foo, s, l);
		}

		public override void disconnect()
		{
			close();
			m_thread = null;
			try
			{
				if (m_io != null)
				{
					try
					{
						if (m_io.m_ins != null)
							m_io.m_ins.Close();
					}
					catch { }
					try
					{
						if (m_io.m_outs != null)
							m_io.m_outs.Close();
					}
					catch { }
				}
				try
				{
					if (m_socket != null)
						m_socket.Close();
				}
				catch { }
			}
			catch (Exception) { }
			m_io = null;
			Channel.Remove(this);
		}

		private static bool equals(byte[] foo, byte[] bar)
		{
			if (foo.Length != bar.Length)
				return false;
			for (int i = 0; i < foo.Length; i++)
				if (foo[i] != bar[i])
					return false;
			return true;
		}
	}
}
