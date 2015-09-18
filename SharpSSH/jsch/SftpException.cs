using System;

namespace SharpSsh.jsch
{
	public class SftpException : Exception
	{
		public int m_id;
		public string m_message;

		public SftpException(SharpSsh.jsch.ChannelSftp.ChannelSftpResult id, string message)
			: this((int)id, message)
		{
		}

		public SftpException(int id, string message)
			: base()
		{
			m_id = id;
			m_message = message;
		}

		public override string ToString()
		{
			return m_message;
		}
	}
}