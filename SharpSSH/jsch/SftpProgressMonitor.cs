using System;

namespace SharpSsh.jsch
{
	public abstract class SftpProgressMonitor
	{
		public enum SfrpOperation
		{
			PUT = 0,
			GET = 1
		}
		public abstract void Init(SfrpOperation op, string src, string dest, long max);
		public abstract bool Count(long count);
		public abstract void End();
	}
}
