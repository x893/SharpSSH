using SharpSsh.java;

namespace SharpSsh.jsch
{
	class UserAuthNone : UserAuth
	{
		private string m_methods = null;
		private UserInfo m_userinfo;

		internal UserAuthNone(UserInfo userinfo)
		{
			m_userinfo = userinfo;
		}

		public override bool start(Session session)
		{
			base.start(session);

			Packet packet = session.m_packet;
			Buffer buf = session.m_buf;
			string username = session.m_username;

			byte[] _username = null;
			try
			{
				_username = Util.getBytesUTF8(username);
			}
			catch
			{
				_username = Util.getBytes(username);
			}

			// send
			// byte      SSH_MSG_USERAUTH_REQUEST(50)
			// string    user name
			// string    service name ("ssh-connection")
			// string    "none"
			packet.reset();
			buf.putByte((byte)Session.SSH_MSG_USERAUTH_REQUEST);
			buf.putString(_username);
			buf.putString(Util.getBytes("ssh-connection"));
			buf.putString(Util.getBytes("none"));
			session.write(packet);

		loop:
			while (true)
			{
				// receive
				// byte      SSH_MSG_USERAUTH_SUCCESS(52)
				// string    service name
				buf = session.read(buf);
				if (buf.m_buffer[5] == Session.SSH_MSG_USERAUTH_SUCCESS)
					return true;

				if (buf.m_buffer[5] == Session.SSH_MSG_USERAUTH_BANNER)
				{
					buf.getInt(); buf.getByte(); buf.getByte();
					byte[] _message = buf.getString();
					byte[] lang = buf.getString();
					string message = null;
					try { message = Util.getStringUTF8(_message); }
					catch
					{
						message = Util.getString(_message);
					}
					if (m_userinfo != null)
						m_userinfo.showMessage(message);
					goto loop;
				}
				if (buf.m_buffer[5] == Session.SSH_MSG_USERAUTH_FAILURE)
				{
					buf.getInt(); buf.getByte(); buf.getByte();
					byte[] foo = buf.getString();
					int partial_success = buf.getByte();
					m_methods = Util.getString(foo);
					break;
				}
				else
					throw new JSchException("USERAUTH fail (" + buf.m_buffer[5] + ")");
			}
			return false;
		}

		internal string getMethods()
		{
			return m_methods;
		}
	}
}
