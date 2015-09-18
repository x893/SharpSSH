using System;

namespace SharpSsh.jsch
{
	/// <summary>
	/// Summary description for JSchException.
	/// </summary>
	public class JSchException : Exception
	{
		public JSchException()
			: base()
		{
		}

		public JSchException(string msg)
			: base(msg)
		{
		}
	}
}
