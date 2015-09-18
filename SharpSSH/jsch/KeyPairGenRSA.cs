using System;

namespace SharpSsh.jsch
{
	public interface KeyPairGenRSA
	{
		void init(int key_size);

		byte[] D { get; }
		byte[] E { get; }
		byte[] N { get; }

		byte[] C { get; }
		byte[] EP { get; }
		byte[] EQ { get; }
		byte[] P { get; }
		byte[] Q { get; }
	}
}
