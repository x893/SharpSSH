using System;

namespace SharpSsh
{
	public delegate void FileTransferEvent(string src, string dst, int transferredBytes, int totalBytes, string message);

	/// <summary>
	/// Summary description for ITransferProtocol.
	/// </summary>
	public interface ITransferProtocol
	{
		void Connect();
		void Close();
		void Cancel();
		void Get(string fromFilePath, string toFilePath);
		void Put(string fromFilePath, string toFilePath);
		void Mkdir(string directory);
		/// <summary>
		/// Triggered when protocol transfer is starting
		/// </summary>
		event FileTransferEvent OnTransferStart;
		/// <summary>
		/// Triggered when protocol transfer ends
		/// </summary>
		event FileTransferEvent OnTransferEnd;
		/// <summary>
		/// Triggered on every interval with the transfer progress iformation.
		/// </summary>
		event FileTransferEvent OnTransferProgress;
	}
}
