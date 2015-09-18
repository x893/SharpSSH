using System;

namespace SharpSsh.jsch
{
	public interface UIKeyboardInteractive
	{
		string[] promptKeyboardInteractive(string destination,
			string name,
			string instruction,
			string[] prompt,
			bool[] echo);
	}
}
