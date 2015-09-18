using System;

namespace SharpSsh
{
	/// <summary>
	/// Summary description for SshTransferProtocolBase.
	/// </summary>
	public abstract class SshTransferProtocolBase : SshBase, ITransferProtocol
	{
		public SshTransferProtocolBase(string host, string user, string password)
			: base(host, user, password)
		{
		}

		public SshTransferProtocolBase(string host, string user)
			: base(host, user)
		{
		}

		#region ITransferProtocol Members

		public abstract void Get(string fromFilePath, string toFilePath);
		public abstract void Put(string fromFilePath, string toFilePath);
		public abstract void Mkdir(string directory);
		public abstract void Cancel();

		/// <summary>
		/// Triggered when transfer is starting
		/// </summary>
		public event FileTransferEvent OnTransferStart;
		/// <summary>
		/// Triggered when transfer ends
		/// </summary>
		public event FileTransferEvent OnTransferEnd;
		/// <summary>
		/// Triggered on every interval with the transfer progress iformation.
		/// </summary>
		public event FileTransferEvent OnTransferProgress;

		/// <summary>
		/// Sends a notification that a file transfer has started
		/// </summary>
		/// <param name="src">The source file to transferred</param>
		/// <param name="dst">Transfer destination</param>
		/// <param name="totalBytes">Total bytes to transfer</param>
		/// <param name="msg">A transfer message</param>
		protected void SendStartMessage(string src, string dst, int totalBytes, string msg)
		{
			if (OnTransferStart != null)
				OnTransferStart(src, dst, 0, totalBytes, msg);
		}

		/// <summary>
		/// Sends a notification that a file transfer has ended
		/// </summary>
		/// <param name="src">The source file to transferred</param>
		/// <param name="dst">Transfer destination</param>
		/// <param name="transferredBytes">Transferred Bytes</param>
		/// <param name="totalBytes">Total bytes to transfer</param>
		/// <param name="msg">A transfer message</param>
		protected void SendEndMessage(string src, string dst, int transferredBytes, int totalBytes, string msg)
		{
			if (OnTransferEnd != null)
				OnTransferEnd(src, dst, transferredBytes, totalBytes, msg);
		}

		/// <summary>
		/// Sends a transfer progress notification
		/// </summary>
		/// <param name="src">The source file to transferred</param>
		/// <param name="dst">Transfer destination</param>
		/// <param name="transferredBytes">Transferred Bytes</param>
		/// <param name="totalBytes">Total bytes to transfer</param>
		/// <param name="msg">A transfer message</param>
		protected void SendProgressMessage(string src, string dst, int transferredBytes, int totalBytes, string msg)
		{
			if (OnTransferProgress != null)
			{
				OnTransferProgress(src, dst, transferredBytes, totalBytes, msg);
			}
		}

		#endregion
	}
}