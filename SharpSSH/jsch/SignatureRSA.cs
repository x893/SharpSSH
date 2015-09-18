using System;

namespace SharpSsh.jsch
{
	public interface SignatureRSA
	{
		void init();
		void setPubKey(byte[] e, byte[] n);
		void setPrvKey(byte[] e, byte[] n, byte[] d, byte[] p, byte[] q, byte[] dp, byte[] dq, byte[] c);
		void update(byte[] H);
		bool verify(byte[] sig);
		byte[] sign();
	}
}
