
using SharpSsh.java;
using SharpSsh.java.lang;

namespace SharpSsh.jsch
{
	public class RequestSubsystem : Request
	{
		private bool m_want_reply = true;
		private string m_subsystem = null;

		public void request(Session session, Channel channel, string subsystem, bool want_reply)
		{
			m_subsystem = subsystem;
			m_want_reply = want_reply;
			request(session, channel);
		}

		public void request(Session session, Channel channel)
		{
			Buffer buf = new Buffer();
			Packet packet = new Packet(buf);

			bool reply = waitForReply();
			if (reply)
				channel.Replay = -1;

			packet.reset();
			buf.putByte((byte)Session.SSH_MSG_CHANNEL_REQUEST);
			buf.putInt(channel.Recipient);
			buf.putString("subsystem");
			buf.putByte((byte)(waitForReply() ? 1 : 0));
			buf.putString(m_subsystem);
			session.write(packet);

			if (reply)
			{
				while (channel.Replay == -1)
				{
					try { Thread.sleep(10); }
					catch { }
				}
				if (channel.Replay == 0)
					throw new JSchException("failed to send subsystem request");
			}
		}
		public bool waitForReply() { return m_want_reply; }
	}
}
