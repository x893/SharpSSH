using System;

namespace SharpSsh.jsch
{
	abstract class UserAuth
	{
		public virtual bool start(Session session)
		{
			Packet packet = session.m_packet;
			Buffer buf = session.m_buf;
			// send
			// byte      SSH_MSG_SERVICE_REQUEST(5)
			// string    service name "ssh-userauth"
			packet.reset();
			buf.putByte((byte)Session.SSH_MSG_SERVICE_REQUEST);
			buf.putString(Util.getBytes("ssh-userauth"));
			session.write(packet);

			// receive
			// byte      SSH_MSG_SERVICE_ACCEPT(6)
			// string    service name
			buf = session.read(buf);
			return buf.m_buffer[5] == 6;
		}
	}
}
