using System;
using System.IO;
using SharpSsh.java;
using SharpSsh.java.io;
using SharpSsh.java.util;
using SharpSsh.java.net;
using SharpSsh.java.lang;
using SharpSsh.jsch;
using System.Text;

namespace SharpSsh.jsch
{
	public class ProxyHTTP : IProxy
	{
		private static int DEFAULTPORT = 80;
		private string m_proxy_host;
		private int m_proxy_port;
		private JStream m_ins;
		private JStream m_outs;
		private Socket m_socket;

		private string m_user;
		private string m_passwd;

		public ProxyHTTP(string proxy_host)
		{
			int port = DEFAULTPORT;
			string host = proxy_host;
			if (proxy_host.IndexOf(':') != -1)
			{
				try
				{
					host = proxy_host.Substring(0, proxy_host.IndexOf(':'));
					port = int.Parse(proxy_host.Substring(proxy_host.IndexOf(':') + 1));
				}
				catch (Exception)
				{ }
			}
			m_proxy_host = host;
			m_proxy_port = port;
		}

		public ProxyHTTP(string proxy_host, int proxy_port)
		{
			m_proxy_host = proxy_host;
			m_proxy_port = proxy_port;
		}

		public Stream InputStream { get { return m_ins.Stream; } }
		public Stream OutputStream { get { return m_outs.Stream; } }
		public Socket Socket { get { return m_socket; } }

		public void setUserPasswd(string user, string passwd)
		{
			m_user = user;
			m_passwd = passwd;
		}

		public void connect(SocketFactory socket_factory, string host, int port, int timeout)
		{
			try
			{
				if (socket_factory == null)
				{
					m_socket = Util.createSocket(m_proxy_host, m_proxy_port, timeout);
					m_ins = new JStream(m_socket.getInputStream());
					m_outs = new JStream(m_socket.getOutputStream());
				}
				else
				{
					m_socket = socket_factory.createSocket(m_proxy_host, m_proxy_port);
					m_ins = new JStream(socket_factory.getInputStream(m_socket));
					m_outs = new JStream(socket_factory.getOutputStream(m_socket));
				}
				if (timeout > 0)
				{
					m_socket.setSoTimeout(timeout);
				}
				m_socket.setTcpNoDelay(true);

				m_outs.Write("CONNECT " + host + ":" + port + " HTTP/1.0\r\n");

				if (m_user != null && m_passwd != null)
				{
					m_outs.Write("Proxy-Authorization: Basic ");
					m_outs.Write(Util.toBase64(m_user + ":" + m_passwd));
					m_outs.Write("\r\n");
				}

				m_outs.Write("\r\n");
				m_outs.flush();

				int foo = 0;

				StringBuilder sb = new StringBuilder();
				while (foo >= 0)
				{
					foo = m_ins.ReadByte();
					if (foo != '\r')
					{
						sb.Append((char)foo);
						continue;
					}
					foo = m_ins.ReadByte();
					if (foo == '\n')
						break;
				}
				if (foo < 0)
					throw new System.IO.IOException();

				string response = sb.ToString();
				string reason = "Unknow reason";
				int code = -1;
				try
				{
					foo = response.IndexOf(' ');
					int bar = response.IndexOf(' ', foo + 1);
					code = int.Parse(response.Substring(foo + 1, bar));
					reason = response.Substring(bar + 1);
				}
				catch (Exception)
				{ }
				if (code != 200)
					throw new System.IO.IOException("proxy error: " + reason);

				int count = 0;
				while (true)
				{
					count = 0;
					while (foo >= 0)
					{
						foo = m_ins.ReadByte();
						if (foo != '\r')
						{
							count++;
							continue;
						}
						foo = m_ins.ReadByte();
						if (foo == '\n')
							break;
					}
					if (foo < 0)
						throw new System.IO.IOException();

					if (count == 0) break;
				}
			}
			catch (RuntimeException e)
			{
				throw e;
			}
			catch (Exception e)
			{
				try
				{
					if (m_socket != null)
						m_socket.Close();
				}
				catch
				{ }
				throw e;
			}
		}

		public void close()
		{
			try
			{
				if (m_ins != null)
					m_ins.close();
				if (m_outs != null)
					m_outs.close();
				if (m_socket != null)
					m_socket.Close();
			}
			catch (Exception)
			{ }
			m_ins = null;
			m_outs = null;
			m_socket = null;
		}
		public static int DefaultPort
		{
			get { return DEFAULTPORT; }
		}
	}
}
