using System;
using System.Net;
using System.Net.Sockets;

namespace SharpSsh.java.net
{
	/// <summary>
	/// Summary description for ServerSocket.
	/// </summary>
	public class ServerSocket : TcpListener
	{
		public ServerSocket(int port, int arg, InetAddress addr)
			: base(addr.IPAddress, port)
		{
			Start();
		}

		public SharpSsh.java.net.Socket Accept()
		{
			return new SharpSsh.java.net.Socket(AcceptSocket());
		}

		public void Close()
		{
			Stop();
		}
	}
}
