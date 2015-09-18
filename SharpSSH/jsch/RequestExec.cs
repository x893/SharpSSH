using System;

namespace SharpSsh.jsch
{
	internal class RequestExec : Request
	{
		private string m_command = "";
		internal RequestExec(string foo)
		{
			m_command = foo;
		}

		public void request(Session session, Channel channel)
		{
			Packet packet = session.m_packet;
			Buffer buf = session.m_buf;
			// send
			// byte     SSH_MSG_CHANNEL_REQUEST(98)
			// uint32 recipient channel
			// string request type       // "exec"
			// boolean want reply        // 0
			// string command
			packet.reset();
			buf.putByte((byte)Session.SSH_MSG_CHANNEL_REQUEST);
			buf.putInt(channel.Recipient);
			buf.putString("exec");
			buf.putByte((byte)(waitForReply() ? 1 : 0));
			buf.putString(m_command);
			session.write(packet);
		}

		public bool waitForReply() { return false; }
	}
}
