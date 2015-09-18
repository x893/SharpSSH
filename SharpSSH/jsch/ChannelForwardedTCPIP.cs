using System;
using System.IO;
using SharpSsh.java.util;
using SharpSsh.java.net;
using SharpSsh.java.lang;
using System.Collections;

namespace SharpSsh.jsch
{
	public class ChannelForwardedTCPIP : Channel
	{
		static private int LOCAL_WINDOW_SIZE_MAX = 0x100000;
		static private int LOCAL_MAXIMUM_PACKET_SIZE = 0x4000;

		internal static ArrayList m_pool = new ArrayList();

		internal SocketFactory m_factory = null;
		internal string m_target;
		internal int m_lport;
		internal int m_rport;

		internal ChannelForwardedTCPIP()
			: base()
		{
			LocalWindowSizeMax = LOCAL_WINDOW_SIZE_MAX;
			LocalWindowSize = LOCAL_WINDOW_SIZE_MAX;
			LocalPacketSize = LOCAL_MAXIMUM_PACKET_SIZE;
		}

		public override void Init()
		{
			try
			{
				m_io = new IO();
				if (m_lport == -1)
				{
					Class c = Class.ForName(m_target);
					ForwardedTCPIPDaemon daemon = (ForwardedTCPIPDaemon)c.Instance();
					daemon.setChannel(this);
					Object[] foo = getPort(m_session, m_rport);
					daemon.setArg((Object[])foo[3]);
					new Thread(daemon).Start();
					m_connected = true;
				}
				else
				{
					Socket socket = (m_factory == null)
									? new Socket(m_target, m_lport)
									: m_factory.createSocket(m_target, m_lport);
					socket.setTcpNoDelay(true);
					m_io.setInputStream(socket.getInputStream());
					m_io.setOutputStream(socket.getOutputStream());
					m_connected = true;
				}
			}
			catch (Exception e)
			{
				throw new Exception(string.Format("Target={0}, Port={1}", m_target, m_lport), e);
			}
		}

		public override void Run()
		{
			m_thread = Thread.currentThread();
			Buffer buf = new Buffer(m_rmpsize);
			Packet packet = new Packet(buf);
			int i = 0;
			try
			{
				while (m_thread != null && m_io != null && m_io.m_ins != null)
				{
					i = m_io.m_ins.Read(buf.m_buffer,
						14,
						buf.m_buffer.Length - 14
						- 32 - 20 // padding and mac
						);
					if (i <= 0)
					{
						eof();
						break;
					}
					packet.reset();
					if (m_close)
						break;
					buf.putByte((byte)Session.SSH_MSG_CHANNEL_DATA);
					buf.putInt(m_recipient);
					buf.putInt(i);
					buf.skip(i);
					m_session.write(packet, this, i);
				}
			}
			catch (Exception)
			{ }

			disconnect();
		}
		internal override void getData(Buffer buf)
		{
			Recipient = buf.getInt();
			RemoteWindowSize = buf.getInt();
			RemotePacketSize = buf.getInt();
			byte[] addr = buf.getString();
			int port = buf.getInt();
			byte[] orgaddr = buf.getString();
			int orgport = buf.getInt();

			lock (m_pool)
			{
				for (int i = 0; i < m_pool.Count; i++)
				{
					Object[] foo = (Object[])(m_pool[i]);
					if (foo[0] != m_session)
						continue;
					if ((int)foo[1] != port)
						continue;
					m_rport = port;
					m_target = (string)foo[2];
					if (foo[3] == null || (foo[3] is Object[]))
						m_lport = -1;
					else
						m_lport = (int)foo[3];

					if (foo.Length >= 5)
						m_factory = ((SocketFactory)foo[4]);
					break;
				}
				if (m_target == null)
					throw new Exception("Target is null");
			}
		}

		internal static Object[] getPort(Session session, int rport)
		{
			lock (m_pool)
			{
				for (int i = 0; i < m_pool.Count; i++)
				{
					Object[] bar = (Object[])(m_pool[i]);
					if (bar[0] != session)
						continue;
					if ((int)bar[1] != rport)
						continue;
					return bar;
				}
				return null;
			}
		}

		internal static string[] getPortForwarding(Session session)
		{
			ArrayList foo = new ArrayList();
			lock (m_pool)
			{
				for (int i = 0; i < m_pool.Count; i++)
				{
					Object[] bar = (Object[])(m_pool[i]);
					if (bar[0] != session)
						continue;
					if (bar[3] == null)
						foo.Add(bar[1] + ":" + bar[2] + ":");
					else
						foo.Add(bar[1] + ":" + bar[2] + ":" + bar[3]);
				}
			}
			string[] bar2 = new string[foo.Count];
			for (int i = 0; i < foo.Count; i++)
			{
				bar2[i] = (string)(foo[i]);
			}
			return bar2;
		}

		internal static void addPort(Session session, int port, string target, int lport, SocketFactory factory)
		{
			lock (m_pool)
			{
				if (getPort(session, port) != null)
					throw new JSchException("PortForwardingR: remote port " + port + " is already registered.");

				Object[] foo = new Object[5];
				foo[0] = session;
				foo[1] = port;
				foo[2] = target;
				foo[3] = lport;
				foo[4] = factory;
				m_pool.Add(foo);
			}
		}
		internal static void addPort(Session session, int port, string daemon, Object[] arg)
		{
			lock (m_pool)
			{
				if (getPort(session, port) != null)
				{
					throw new JSchException("PortForwardingR: remote port " + port + " is already registered.");
				}
				Object[] foo = new Object[4];
				foo[0] = session;
				foo[1] = port;
				foo[2] = daemon;
				foo[3] = arg;
				m_pool.Add(foo);
			}
		}
		internal static void delPort(ChannelForwardedTCPIP c)
		{
			delPort(c.m_session, c.m_rport);
		}
		internal static void delPort(Session session, int rport)
		{
			lock (m_pool)
			{
				Object[] foo = null;
				for (int i = 0; i < m_pool.Count; i++)
				{
					Object[] bar = (Object[])(m_pool[i]);
					if (bar[0] != session)
						continue;
					if ((int)bar[1] != rport)
						continue;
					foo = bar;
					break;
				}
				if (foo == null)
					return;
				m_pool.Remove(foo);
			}

			Buffer buf = new Buffer(100);
			Packet packet = new Packet(buf);

			try
			{
				packet.reset();
				buf.putByte((byte)80 /*SSH_MSG_GLOBAL_REQUEST*/);
				buf.putString("cancel-tcpip-forward");
				buf.putByte((byte)0);
				buf.putString("0.0.0.0");
				buf.putInt(rport);
				session.write(packet);
			}
			catch (Exception)
			{ }
		}
		internal static void delPort(Session session)
		{
			int[] rport = null;
			int count = 0;
			lock (m_pool)
			{
				rport = new int[m_pool.Count];
				for (int i = 0; i < m_pool.Count; i++)
				{
					Object[] bar = (Object[])(m_pool[i]);
					if (bar[0] == session)
						rport[count++] = (int)bar[1];
				}
			}
			for (int i = 0; i < count; i++)
				delPort(session, rport[i]);
		}

		public int getRemotePort()
		{
			return m_rport;
		}
		void setSocketFactory(SocketFactory factory)
		{
			this.m_factory = factory;
		}
	}
}
