using System;

namespace SharpSsh
{
	/// <summary>
	/// Summary description for SshTransferException.
	/// </summary>
	public class SshTransferException : Exception
	{
		public SshTransferException(string msg)
			: base(msg)
		{
		}
	}
}
