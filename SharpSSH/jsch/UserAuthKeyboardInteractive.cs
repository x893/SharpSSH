namespace SharpSsh.jsch
{
	class UserAuthKeyboardInteractive : UserAuth
	{
		private UserInfo m_userinfo;

		internal UserAuthKeyboardInteractive(UserInfo userinfo)
		{
			m_userinfo = userinfo;
		}

		public override bool start(Session session)
		{
			Packet packet = session.m_packet;
			Buffer buf = session.m_buf;
			string username = session.m_username;
			string dest = username + "@" + session.m_host;
			if (session.m_port != SharpSsh.SshBase.SSH_TCP_PORT)
				dest += (":" + session.m_port);

			bool cancel = false;

			byte[] _username = null;
			try
			{
				_username = System.Text.Encoding.UTF8.GetBytes(username);
			}
			catch
			{
				_username = Util.getBytes(username);
			}

			while (true)
			{
				// send
				// byte      SSH_MSG_USERAUTH_REQUEST(50)
				// string    user name (ISO-10646 UTF-8, as defined in [RFC-2279])
				// string    service name (US-ASCII) "ssh-userauth" ? "ssh-connection"
				// string    "keyboard-interactive" (US-ASCII)
				// string    language tag (as defined in [RFC-3066])
				// string    submethods (ISO-10646 UTF-8)
				packet.reset();
				buf.putByte((byte)Session.SSH_MSG_USERAUTH_REQUEST);
				buf.putString(_username);
				buf.putString(Util.getBytes("ssh-connection"));
				//buf.putString("ssh-userauth".getBytes());
				buf.putString(Util.getBytes("keyboard-interactive"));
				buf.putString(Util.getBytes(""));
				buf.putString(Util.getBytes(""));
				session.write(packet);

				bool firsttime = true;
			loop:
				while (true)
				{
					// receive
					// byte      SSH_MSG_USERAUTH_SUCCESS(52)
					// string    service name
					try { buf = session.read(buf); }
					catch (JSchException e)
					{
						e.GetType();
						return false;
					}
					catch (System.IO.IOException e)
					{
						e.GetType();
						return false;
					}

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

						if (firsttime)
							throw new JSchException("USERAUTH KI is not supported");
						break;
					}
					if (buf.m_buffer[5] == Session.SSH_MSG_USERAUTH_INFO_REQUEST)
					{
						firsttime = false;
						buf.getInt(); buf.getByte(); buf.getByte();
						string name = Util.getString(buf.getString());
						string instruction = Util.getString(buf.getString());
						string languate_tag = Util.getString(buf.getString());
						int num = buf.getInt();
						string[] prompt = new string[num];
						bool[] echo = new bool[num];
						for (int i = 0; i < num; i++)
						{
							prompt[i] = Util.getString(buf.getString());
							echo[i] = (buf.getByte() != 0);
						}

						string[] response = null;
						if (num > 0
						   || (name.Length > 0 || instruction.Length > 0)
						   )
						{
							UIKeyboardInteractive kbi = (UIKeyboardInteractive)m_userinfo;
							if (m_userinfo != null)
								response = kbi.promptKeyboardInteractive(dest,
												   name,
												   instruction,
												   prompt,
												   echo);
						}

						// byte      SSH_MSG_USERAUTH_INFO_RESPONSE(61)
						// int       num-responses
						// string    response[1] (ISO-10646 UTF-8)
						// ...
						// string    response[num-responses] (ISO-10646 UTF-8)
						packet.reset();
						buf.putByte((byte)Session.SSH_MSG_USERAUTH_INFO_RESPONSE);
						if (num > 0 &&
						   (response == null ||  // cancel
							num != response.Length))
						{
							buf.putInt(0);
							if (response == null)
								cancel = true;
						}
						else
						{
							buf.putInt(num);
							for (int i = 0; i < num; i++)
								buf.putString(Util.getBytes(response[i]));
						}
						session.write(packet);
						if (cancel)
							break;
						goto loop;
					}
					return false;
				}
				if (cancel)
					throw new JSchAuthCancelException("keyboard-interactive");
			}
		}
	}
}
