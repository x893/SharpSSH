using System;
using SharpSsh.jsch;
using System.Collections;

namespace SharpSsh
{
	public class Sftp : SshTransferProtocolBase
	{
		private MyProgressMonitor m_monitor;
		private bool cancelled = false;

		public Sftp(string sftpHost, string user, string password)
			: base(sftpHost, user, password)
		{
			Init();
		}

		public Sftp(string sftpHost, string user)
			: base(sftpHost, user)
		{
			Init();
		}

		private void Init()
		{
			m_monitor = new MyProgressMonitor(this);
		}

		protected override string ChannelType
		{
			get { return "sftp"; }
		}

		private ChannelSftp SftpChannel
		{
			get { return (ChannelSftp)m_channel; }
		}

		public override void Cancel()
		{
			cancelled = true;
		}

		public void Get(string fromFilePath)
		{
			Get(fromFilePath, ".");
		}

		public void Get(string[] fromFilePaths)
		{
			for (int i = 0; i < fromFilePaths.Length; i++)
				Get(fromFilePaths[i]);
		}

		public void Get(string[] fromFilePaths, string toDirPath)
		{
			for (int i = 0; i < fromFilePaths.Length; i++)
				Get(fromFilePaths[i], toDirPath);
		}

		public override void Get(string fromFilePath, string toFilePath)
		{
			cancelled = false;
			SftpChannel.get(fromFilePath, toFilePath, m_monitor, ChannelSftp.ChannelSftpModes.OVERWRITE);
		}

		//Put

		public void Put(string fromFilePath)
		{
			Put(fromFilePath, ".");
		}

		public void Put(string[] fromFilePaths)
		{
			for (int i = 0; i < fromFilePaths.Length; i++)
				Put(fromFilePaths[i]);
		}

		public void Put(string[] fromFilePaths, string toDirPath)
		{
			for (int i = 0; i < fromFilePaths.Length; i++)
				Put(fromFilePaths[i], toDirPath);
		}

		public override void Put(string fromFilePath, string toFilePath)
		{
			cancelled = false;
			SftpChannel.put(fromFilePath, toFilePath, m_monitor, ChannelSftp.ChannelSftpModes.OVERWRITE);
		}

		public override void Mkdir(string directory)
		{
			SftpChannel.mkdir(directory);
		}

		public ArrayList GetFileList(string path)
		{
			ArrayList list = new ArrayList();
			foreach (SharpSsh.jsch.ChannelSftp.LsEntry entry in SftpChannel.ls(path))
				list.Add(entry.Filename.ToString());
			return list;
		}

		#region ProgressMonitor Implementation
		private class MyProgressMonitor : SftpProgressMonitor
		{
			private long m_transferred = 0;
			private long m_total = 0;
			private int m_elapsed = -1;
			private Sftp m_sftp;
			private string m_src;
			private string m_dest;
			private System.Timers.Timer m_timer;

			public MyProgressMonitor(Sftp sftp)
			{
				m_sftp = sftp;
			}

			public override void Init(SfrpOperation op, string src, string dest, long max)
			{
				m_src = src;
				m_dest = dest;
				m_elapsed = 0;
				m_total = max;
				m_timer = new System.Timers.Timer(1000);
				m_timer.Start();
				m_timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);

				m_sftp.SendStartMessage(
					src,
					dest,
					(int)m_total,
					(op == SfrpOperation.GET)
					? "Downloading " + System.IO.Path.GetFileName(src) + "..."
					: "Uploading " + System.IO.Path.GetFileName(src) + "..."
					);
			}
			public override bool Count(long c)
			{
				m_transferred += c;
				m_sftp.SendProgressMessage(
					m_src,
					m_dest,
					(int)m_transferred,
					(int)m_total,
					"Transfering... [Elapsed time: " + m_elapsed + "]"
					);
				return !m_sftp.cancelled;
			}
			public override void End()
			{
				m_timer.Stop();
				m_timer.Dispose();
				m_sftp.SendEndMessage(
					m_src,
					m_dest,
					(int)m_transferred,
					(int)m_total,
					"Done in " + m_elapsed + " seconds!"
					);
				m_transferred = 0;
				m_total = 0;
				m_elapsed = -1;
				m_src = null;
				m_dest = null;
			}

			private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
			{
				this.m_elapsed++;
			}
		}

		#endregion ProgressMonitor Implementation
	}
}
