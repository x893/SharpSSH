using System;
using System.IO;

namespace SharpSsh.jsch
{
	internal class OutputStreamPut : java.io.OutputStream
	{
		private byte[] m_data = new byte[1];
		private ChannelSftp m_sftp;
		private byte[] m_handle;
		private long[] m_offset;
		private SftpProgressMonitor m_monitor;
		private bool m_init = true;
		private int[] m_ackid = new int[1];
		private int m_startid = 0;
		private int mm_ackid = 0;
		private int m_ackcount = 0;
		private ChannelSftp.Header header = new ChannelSftp.Header();

		internal OutputStreamPut(ChannelSftp sftp, byte[] handle, long[] _offset, SftpProgressMonitor monitor)
			: base()
		{
			m_sftp = sftp;
			m_handle = handle;
			m_offset = _offset;
			m_monitor = monitor;
		}

		public override void Write(byte[] src, int start, int length)
		{
			if (m_init)
			{
				m_startid = m_sftp.Seq;
				mm_ackid = m_sftp.Seq;
				m_init = false;
			}

			try
			{
				int len = length;
				while (len > 0)
				{
					int sent = m_sftp.sendWRITE(m_handle, m_offset[0], src, start, len);
					m_offset[0] += sent;
					start += sent;
					len -= sent;
					if ((m_sftp.Seq - 1) == m_startid || m_sftp.IO.m_ins.Available() >= 1024)
					{
						while (m_sftp.IO.m_ins.Available() > 0)
						{
							if (m_sftp.checkStatus(m_ackid, header))
							{
								mm_ackid = m_ackid[0];
								if (m_startid > mm_ackid || mm_ackid > m_sftp.Seq - 1)
									throw new SftpException(ChannelSftp.ChannelSftpResult.SSH_FX_FAILURE, "");
								m_ackcount++;
							}
							else
								break;
						}
					}
				}
				if (m_monitor != null && !m_monitor.Count(length))
				{
					close();
					throw new IOException("canceled");
				}
			}
			catch (IOException e)
			{
				throw e;
			}
			catch (Exception e)
			{
				throw new IOException(e.ToString());
			}
		}

		public override void Close()
		{
			if (!m_init)
			{
				try
				{
					int _ackcount = m_sftp.Seq - m_startid;
					while (_ackcount > m_ackcount)
					{
						if (!m_sftp.checkStatus(null, header))
							break;
						m_ackcount++;
					}
				}
				catch (SftpException e)
				{
					throw new IOException(e.ToString());
				}
			}

			if (m_monitor != null)
				m_monitor.End();
			try
			{
				m_sftp._sendCLOSE(m_handle, header);
			}
			catch (IOException e)
			{
				throw e;
			}
			catch (Exception e)
			{
				throw new IOException(e.ToString());
			}
		}
	}
}
