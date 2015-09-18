using System;
using System.IO;
using SharpSsh.java.lang;

namespace SharpSsh.jsch
{
	public class ChannelDirectTCPIP : Channel
	{
		private const int LOCAL_WINDOW_SIZE_MAX = 0x20000;
		private const int LOCAL_MAXIMUM_PACKET_SIZE = 0x4000;

		private string m_host;
		private int m_port;

		internal string m_originator_IP_address = "127.0.0.1";
		internal int m_originator_port = 0;

		internal ChannelDirectTCPIP()
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
			}
			catch (Exception e)
			{
				throw e;
			}
		}

		public override void connect()
		{
			try
			{
				if (!m_session.IsConnected())
					throw new JSchException("session is down");

				Buffer buf = new Buffer(150);
				Packet packet = new Packet(buf);

				packet.reset();
				buf.putByte((byte)90);
				buf.putString(Util.getBytes("direct-tcpip"));
				buf.putInt(m_id);
				buf.putInt(m_lwsize);
				buf.putInt(m_lmpsize);
				buf.putString(Util.getBytes(m_host));
				buf.putInt(m_port);
				buf.putString(Util.getBytes(m_originator_IP_address));
				buf.putInt(m_originator_port);
				m_session.write(packet);

				int retry = 1000;
				try
				{
					while (Recipient == -1
						&& m_session.IsConnected()
						&& retry > 0
						&& !m_eof_remote
						)
					{
						Thread.Sleep(50);
						retry--;
					}
				}
				catch { }

				if (!m_session.IsConnected())
					throw new JSchException("session is down");
				if (retry == 0 || this.m_eof_remote)
					throw new JSchException("channel is not opened.");

				m_connected = true;
				m_thread = new Thread(this);
				m_thread.Start();
			}
			catch (Exception e)
			{
				m_io.close();
				m_io = null;
				Channel.Remove(this);
				if (e is JSchException)
					throw (JSchException)e;
			}
		}

		public override void Run()
		{
			Buffer buf = new Buffer(m_rmpsize);
			Packet packet = new Packet(buf);
			int i = 0;
			try
			{
				while (isConnected()
					&& m_thread != null
					&& m_io != null
					&& m_io.m_ins != null
					)
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
					if (m_close) break;
					packet.reset();
					buf.putByte((byte)Session.SSH_MSG_CHANNEL_DATA);
					buf.putInt(m_recipient);
					buf.putInt(i);
					buf.skip(i);
					m_session.write(packet, this, i);
				}
			}
			catch
			{ }
			disconnect();
		}

		public override void setInputStream(Stream ins)
		{
			m_io.setInputStream(ins);
		}

		public override void setOutputStream(Stream outs)
		{
			m_io.setOutputStream(outs);
		}

		public string Host { set { m_host = value; } }
		public int Port { set { m_port = value; } }
		public string OrgIPAddress { set { m_originator_IP_address = value; } }
		public int OrgPort { set { m_originator_port = value; } }
	}
}