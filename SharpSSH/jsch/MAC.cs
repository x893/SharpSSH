using System;

namespace SharpSsh.jsch
{
	public interface MAC
	{
		int BlockSize { get; }
		string Name { get; }

		void init(byte[] key);
		void update(byte[] foo, int start, int len);
		void update(int foo);
		byte[] doFinal();
	}
}
