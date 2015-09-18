using System;

namespace SharpSsh.jsch
{
	public abstract class HostKeyRepository
	{
		internal const int OK = 0;
		internal const int NOT_INCLUDED = 1;
		internal const int CHANGED = 2;

		public abstract int check(string host, byte[] key);
		public abstract void add(string host, byte[] key, UserInfo ui);
		public abstract void remove(string host, string type);
		public abstract void remove(string host, string type, byte[] key);
		public abstract string getKnownHostsRepositoryID();
		public abstract HostKey[] getHostKey();
		public abstract HostKey[] getHostKey(string host, string type);
	}
}
