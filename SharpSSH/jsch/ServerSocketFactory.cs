using System;
using SharpSsh.java.net;

namespace SharpSsh.jsch
{
	/// <summary>
	/// Summary description for ServerSocketFactory.
	/// </summary>
	public interface ServerSocketFactory
	{
		ServerSocket createServerSocket(int port, int backlog, InetAddress bindAddr);
	}
}
