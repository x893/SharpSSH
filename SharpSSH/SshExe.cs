using System;
using SharpSsh.jsch;
using System.Text;

namespace SharpSsh
{
	/// <summary>
	/// Summary description for SshExe.
	/// </summary>
	public class SshExec : SshBase
	{
		public SshExec(string host, string user, string password)
			: base(host, user, password)
		{
		}

		public SshExec(string host, string user)
			: base(host, user)
		{
		}

		protected override string ChannelType
		{
			get { return "exec"; }
		}

		/// <summary>
		///This function is empty, so no channel is connected
		///on session connect 
		/// </summary>
		protected override void ConnectChannel()
		{
		}

		protected ChannelExec GetChannelExec(string command)
		{
			ChannelExec exeChannel = (ChannelExec)m_session.openChannel("exec");
			exeChannel.setCommand(command);
			return exeChannel;
		}

		public string RunCommand(string command)
		{
			m_channel = GetChannelExec(command);
			System.IO.Stream s = m_channel.getInputStream();
			m_channel.connect();
			byte[] buff = new byte[1024];
			StringBuilder res = new StringBuilder();
			int c = 0;
			while (true)
			{
				c = s.Read(buff, 0, buff.Length);
				if (c == -1) break;
				res.Append(Encoding.ASCII.GetString(buff, 0, c));
				//Console.WriteLine(res);
			}
			m_channel.disconnect();
			return res.ToString();
		}

		public int RunCommand(string command, ref string StdOut, ref string StdErr)
		{
			StdOut = "";
			StdErr = "";
			m_channel = GetChannelExec(command);
			System.IO.Stream stdout = m_channel.getInputStream();
			System.IO.Stream stderr = ((ChannelExec)m_channel).ErrStream;
			m_channel.connect();
			byte[] buff = new byte[1024];
			StringBuilder sbStdOut = new StringBuilder();
			StringBuilder sbStdErr = new StringBuilder();
			int o = 0; int e = 0;
			while (true)
			{
				if (o != -1) o = stdout.Read(buff, 0, buff.Length);
				if (o != -1) StdOut += sbStdOut.Append(Encoding.ASCII.GetString(buff, 0, o));
				if (e != -1) e = stderr.Read(buff, 0, buff.Length);
				if (e != -1) StdErr += sbStdErr.Append(Encoding.ASCII.GetString(buff, 0, e));
				if ((o == -1) && (e == -1)) break;
			}
			m_channel.disconnect();

			return m_channel.ExitStatus;
		}

		public ChannelExec ChannelExec
		{
			get { return (ChannelExec)this.m_channel; }
		}
	}
}
