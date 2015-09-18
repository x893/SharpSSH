using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using SharpSsh.java.lang;

namespace SharpSsh.jsch
{
	public class ChannelExec : ChannelSession
	{
		private bool m_xforwading = false;
		private bool m_pty = false;
		private string m_command = "";

		public override void start()
		{
			try
			{
				Request request;

				if (m_xforwading)
				{
					request = new RequestX11();
					request.request(m_session, this);
				}

				if (m_pty)
				{
					request = new RequestPtyReq();
					request.request(m_session, this);
				}

				request = new RequestExec(m_command);
				request.request(m_session, this);
			}
			catch (Exception)
			{
				throw new JSchException("ChannelExec");
			}
			m_thread = new Thread(this);
			m_thread.Name = "Exec thread " + m_session.Host;
			m_thread.Start();
		}

		public void setCommand(string foo)
		{
			m_command = foo;
		}

		public override void Init()
		{
			m_io.setInputStream(m_session.m_In);
			m_io.setOutputStream(m_session.m_Out);
		}

		public Stream ErrStream
		{
			set { setExtOutputStream(value); }
			get { return getExtInputStream(); }
		}
	}
}
