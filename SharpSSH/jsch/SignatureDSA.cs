using System;

namespace SharpSsh.jsch
{
	public interface SignatureDSA
	{
		void init();
		void setPubKey(byte[] y, byte[] p, byte[] q, byte[] g);
		void setPrvKey(byte[] x, byte[] p, byte[] q, byte[] g);
		void update(byte[] H);
		bool verify(byte[] sig);
		byte[] sign();
	}
}