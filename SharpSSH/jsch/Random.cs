using System;

namespace SharpSsh.jsch
{
	public interface Random
	{
		void fill(byte[] foo, int start, int len);
	}
}
