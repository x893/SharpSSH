using System;

namespace SharpSsh.jsch
{
	public interface KeyPairGenDSA
	{
		void init(int key_size);
		byte[] X { get; }
		byte[] Y { get; }
		byte[] P { get; }
		byte[] Q { get; }
		byte[] G { get; }
	}
}
