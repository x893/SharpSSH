using System;
using SharpSsh.java.net;
using System.IO;

namespace SharpSsh.jsch
{
	public interface SocketFactory
	{
		Socket createSocket(string host, int port);
		Stream getInputStream(Socket socket);
		Stream getOutputStream(Socket socket);
	}
}
