using System;

namespace SharpSsh.jsch
{
	public interface DH
	{
		void init();
		byte[] P { set; }
		byte[] G { set; }
		byte[] F { set; }
		byte[] E { get; }
		byte[] K { get; }
	}
}
