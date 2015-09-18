using System;
using SharpSsh.java.lang;
using Str = SharpSsh.java.StringEx;

namespace SharpSsh.jsch
{
	public class ChannelSession : Channel
	{
		private static byte[] _session = new Str("session").getBytes();
		public ChannelSession()
			: base()
		{
			m_type = _session;
			m_io = new IO();
		}

		public override void Run()
		{
			Buffer buf = new Buffer(m_rmpsize);
			Packet packet = new Packet(buf);
			int i = -1;
			try
			{
				while (isConnected() &&
					m_thread != null &&
					m_io != null &&
					m_io.m_ins != null)
				{
					i = m_io.m_ins.Read(buf.m_buffer,
									14,
									buf.m_buffer.Length - 14
									- 32 - 20 // padding and mac
									);
					if (i == 0) continue;
					if (i == -1)
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
			catch (Exception) { }
			m_thread = null;
		}
	}
}
