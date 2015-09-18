using System;

namespace SharpSsh.jsch
{
	/// <summary>
	/// Summary description for JSchException.
	/// </summary>
	public class JSchAuthCancelException : JSchException
	{
		public JSchAuthCancelException()
			: base()
		{
		}

		public JSchAuthCancelException(string msg)
			: base(msg)
		{
		}
	}
}