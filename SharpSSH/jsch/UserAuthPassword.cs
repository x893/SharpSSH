using System;

namespace SharpSsh.jsch
{
	class UserAuthPassword : UserAuth
	{
		private UserInfo m_userinfo;

		internal UserAuthPassword(UserInfo userinfo)
		{
			m_userinfo = userinfo;
		}

		public override bool start(Session session)
		{
			Packet packet = session.m_packet;
			Buffer buf = session.m_buf;
			string username = session.m_username;
			string password = session.m_password;
			string dest = username + "@" + session.m_host;
			if (session.m_port != SharpSsh.SshBase.SSH_TCP_PORT)
				dest += (":" + session.m_port);

			while (true)
			{
				if (password == null)
				{
					if (m_userinfo == null)
						return false;
					if (!m_userinfo.promptPassword("Password for " + dest))
						throw new JSchAuthCancelException("password");
					password = m_userinfo.Password;
					if (password == null)
						throw new JSchAuthCancelException("password");
				}

				byte[] _username = null;
				try { _username = Util.getBytesUTF8(username); }
				catch
				{
					_username = Util.getBytes(username);
				}

				byte[] _password = null;
				try { _password = Util.getBytesUTF8(password); }
				catch
				{
					_password = Util.getBytes(password);
				}

				// send
				// byte      SSH_MSG_USERAUTH_REQUEST(50)
				// string    user name
				// string    service name ("ssh-connection")
				// string    "password"
				// boolen    FALSE
				// string    plaintext password (ISO-10646 UTF-8)
				packet.reset();
				buf.putByte((byte)Session.SSH_MSG_USERAUTH_REQUEST);
				buf.putString(_username);
				buf.putString(Util.getBytes("ssh-connection"));
				buf.putString(Util.getBytes("password"));
				buf.putByte((byte)0);
				buf.putString(_password);
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
						if (partial_success != 0)
							throw new JSchPartialAuthException(Util.getString(foo));
						break;
					}
					else
						return false;
				}
				password = null;
			}
		}
	}
}
