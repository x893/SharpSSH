using System;
using SharpSsh.java.lang;

namespace SharpSsh.jsch
{
	public class ChannelShell : ChannelSession
	{
		internal bool m_xforwading = false;
		internal bool m_pty = true;

		public override void setXForwarding(bool foo) { m_xforwading = foo; }
		public void setPty(bool foo) { m_pty = foo; }

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
				request = new RequestShell();
				request.request(m_session, this);
			}
			catch//(Exception e)
			{
				throw new JSchException("ChannelShell");
			}
			m_thread = new Thread(this);
			m_thread.Name = "Shell for " + m_session.m_host;
			m_thread.Start();
		}

		public override void Init()
		{
			m_io.setInputStream(m_session.m_In);
			m_io.setOutputStream(m_session.m_Out);
		}

		public void setPtySize(int col, int row, int wp, int hp)
		{
			try
			{
				RequestWindowChange request = new RequestWindowChange();
				request.setSize(col, row, wp, hp);
				request.request(m_session, this);
			}
			catch (Exception e)
			{
				throw new JSchException("ChannelShell.setPtySize: " + e.ToString());
			}
		}
	}
}

