using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Net = System.Net;
using Sock = System.Net.Sockets.Socket;

namespace SharpSsh.java.net
{
	/// <summary>
	/// Summary description for Socket.
	/// </summary>
	public class Socket
	{
		internal Sock m_sock;

		protected void SetSocketOption(SocketOptionLevel level, SocketOptionName name, int option)
		{
			try
			{
				m_sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, option);
			}
			catch { }
		}

		public Socket(string host, int port)
		{
			IPEndPoint ep = new IPEndPoint(Dns.GetHostEntry(host).AddressList[0], port);
			m_sock = new Sock(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			m_sock.Connect(ep);
		}

		public Socket(Sock sock)
		{
			m_sock = sock;
		}

		public Stream getInputStream()
		{
			return new Net.Sockets.NetworkStream(m_sock);
		}

		public Stream getOutputStream()
		{
			return new Net.Sockets.NetworkStream(m_sock);
		}

		public bool isConnected()
		{
			return m_sock.Connected;
		}

		public void setTcpNoDelay(bool b)
		{
			SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, (b) ? 1 : 0);
		}

		public void setSoTimeout(int t)
		{
			SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, t);
			SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, t);
		}

		public void Close()
		{
			m_sock.Close();
		}

		public InetAddress getInetAddress()
		{
			return new InetAddress(((IPEndPoint)m_sock.RemoteEndPoint).Address);
		}

		public int Port
		{
			get { return ((IPEndPoint)m_sock.RemoteEndPoint).Port; }
		}
	}
}

