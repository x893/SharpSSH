using System;

namespace SharpSsh.jsch
{
	public abstract class HASH
	{
		public abstract void init();
		public abstract int BlockSize { get; }
		public abstract void update(byte[] foo, int start, int len);
		public abstract byte[] digest();
	}
}

