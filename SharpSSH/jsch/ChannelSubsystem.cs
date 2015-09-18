using System;
using SharpSsh.java.net;
using SharpSsh.java.lang;

namespace SharpSsh.jsch
{
	public class ChannelSubsystem : ChannelSession
	{
		bool m_xforwading = false;
		bool m_pty = false;
		bool m_want_reply = true;
		string m_subsystem = "";

		public override void setXForwarding(bool foo) { m_xforwading = true; }
		public void setPty(bool foo) { m_pty = foo; }
		public void setWantReply(bool foo) { m_want_reply = foo; }
		public void setSubsystem(string foo) { m_subsystem = foo; }

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
				request = new RequestSubsystem();
				((RequestSubsystem)request).request(m_session, this, m_subsystem, m_want_reply);
			}
			catch (Exception e)
			{
				if (e is JSchException) { throw (JSchException)e; }
				throw new JSchException("ChannelSubsystem");
			}
			Thread thread = new Thread(this);
			thread.Name = "Subsystem for " + m_session.m_host;
			thread.Start();
		}

		public override void Init()
		{
			m_io.setInputStream(m_session.m_In);
			m_io.setOutputStream(m_session.m_Out);
		}
		public void setErrStream(System.IO.Stream outs)
		{
			setExtOutputStream(outs);
		}
		public java.io.InputStream getErrStream()
		{
			return getExtInputStream();
		}
	}
}
