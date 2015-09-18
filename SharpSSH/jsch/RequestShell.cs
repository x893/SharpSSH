using System;

namespace SharpSsh.jsch
{
	class RequestShell : Request
	{
		public void request(Session session, Channel channel)
		{
			Buffer buf = new Buffer();
			Packet packet = new Packet(buf);

			// send
			// byte     SSH_MSG_CHANNEL_REQUEST(98)
			// uint32 recipient channel
			// string request type       // "shell"
			// boolean want reply        // 0
			packet.reset();
			buf.putByte((byte)Session.SSH_MSG_CHANNEL_REQUEST);
			buf.putInt(channel.Recipient);
			buf.putString(Util.getBytes("shell"));
			buf.putByte((byte)(waitForReply() ? 1 : 0));
			session.write(packet);
		}
		public bool waitForReply() { return false; }
	}
}
