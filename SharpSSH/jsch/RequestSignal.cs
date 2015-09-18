using System;

namespace SharpSsh.jsch
{
	class RequestSignal : Request
	{
		string m_signal = "KILL";

		public void setSignal(string foo) { m_signal = foo; }

		public void request(Session session, Channel channel)
		{
			Buffer buf = new Buffer();
			Packet packet = new Packet(buf);

			packet.reset();
			buf.putByte((byte)Session.SSH_MSG_CHANNEL_REQUEST);
			buf.putInt(channel.Recipient);
			buf.putString(Util.getBytes("signal"));
			buf.putByte((byte)(waitForReply() ? 1 : 0));
			buf.putString(Util.getBytes(m_signal));
			session.write(packet);
		}

		public bool waitForReply() { return false; }
	}
}

