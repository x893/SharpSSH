using System;

namespace SharpSsh.jsch
{
	class RequestX11 : Request
	{
		public void setCookie(string cookie)
		{
			ChannelX11.Cookie = Util.getBytes(cookie);
		}

		public void request(Session session, Channel channel)
		{
			Buffer buf = new Buffer();
			Packet packet = new Packet(buf);

			// byte      SSH_MSG_CHANNEL_REQUEST(98)
			// uint32 recipient channel
			// string request type        // "x11-req"
			// boolean want reply         // 0
			// boolean   single connection
			// string    x11 authentication protocol // "MIT-MAGIC-COOKIE-1".
			// string    x11 authentication cookie
			// uint32    x11 screen number
			packet.reset();
			buf.putByte((byte)Session.SSH_MSG_CHANNEL_REQUEST);
			buf.putInt(channel.Recipient);
			buf.putString(Util.getBytes("x11-req"));
			buf.putByte((byte)(waitForReply() ? 1 : 0));
			buf.putByte((byte)0);
			buf.putString(Util.getBytes("MIT-MAGIC-COOKIE-1"));
			buf.putString(ChannelX11.getFakedCookie(session));
			buf.putInt(0);
			session.write(packet);
		}
		public bool waitForReply() { return false; }
	}
}
