using System;
using System.IO;
using System.Threading;
using SharpSsh.java.net;
using SharpSsh.java.lang;
using InetAddress = SharpSsh.java.net.InetAddress;
using System.Collections;
using System.Collections.Generic;

namespace SharpSsh.jsch
{
	class PortWatcher : SharpSsh.java.lang.IRunnable
	{
		private static List<PortWatcher> m_pool = new List<PortWatcher>();

		internal Session session;
		internal int lport;
		internal int rport;
		internal string host;
		internal InetAddress boundaddress;
		internal IRunnable thread;
		internal ServerSocket ss;

		internal static string[] getPortForwarding(Session session)
		{
			ArrayList foo = new ArrayList();
			lock (m_pool)
			{
				for (int i = 0; i < m_pool.Count; i++)
				{
					PortWatcher p = m_pool[i];
					if (p.session == session)
						foo.Add(p.lport + ":" + p.host + ":" + p.rport);
				}
			}
			string[] bar = new String[foo.Count];
			for (int i = 0; i < foo.Count; i++)
			{
				bar[i] = (String)(foo[i]);
			}
			return bar;
		}

		internal static PortWatcher getPort(Session session, string address, int lport)
		{
			InetAddress addr;
			try
			{
				addr = InetAddress.getByName(address);
			}
			catch (Exception)
			{
				throw new JSchException("PortForwardingL: invalid address " + address + " specified.");
			}
			lock (m_pool)
			{
				for (int i = 0; i < m_pool.Count; i++)
				{
					PortWatcher p = m_pool[i];
					if (p.session == session && p.lport == lport)
					{
						if (p.boundaddress.isAnyLocalAddress() ||
							p.boundaddress.equals(addr))
							return p;
					}
				}
				return null;
			}
		}
		internal static PortWatcher addPort(Session session, string address, int lport, string host, int rport, ServerSocketFactory ssf)
		{
			if (getPort(session, address, lport) != null)
			{
				throw new JSchException("PortForwardingL: local port " + address + ":" + lport + " is already registered.");
			}
			PortWatcher pw = new PortWatcher(session, address, lport, host, rport, ssf);
			m_pool.Add(pw);
			return pw;
		}
		internal static void delPort(Session session, string address, int lport)
		{
			PortWatcher pw = getPort(session, address, lport);
			if (pw == null)
			{
				throw new JSchException("PortForwardingL: local port " + address + ":" + lport + " is not registered.");
			}
			pw.delete();
			m_pool.Remove(pw);
		}
		internal static void delPort(Session session)
		{
			lock (m_pool)
			{
				PortWatcher[] foo = new PortWatcher[m_pool.Count];
				int count = 0;
				for (int i = 0; i < m_pool.Count; i++)
				{
					PortWatcher p = m_pool[i];
					if (p.session == session)
					{
						p.delete();
						foo[count++] = p;
					}
				}
				for (int i = 0; i < count; i++)
				{
					PortWatcher p = foo[i];
					m_pool.Remove(p);
				}
			}
		}

		internal PortWatcher(Session session,
			string address, int lport,
			string host, int rport,
			ServerSocketFactory factory)
		{
			this.session = session;
			this.lport = lport;
			this.host = host;
			this.rport = rport;
			try
			{
				boundaddress = InetAddress.getByName(address);
				ss = (factory == null) ?
					new ServerSocket(lport, 0, boundaddress) :
					factory.createServerSocket(lport, 0, boundaddress);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw new JSchException("PortForwardingL: local port " + address + ":" + lport + " cannot be bound.");
			}
		}

		public void Run()
		{
			Buffer buf = new Buffer(300); // ??
			Packet packet = new Packet(buf);
			thread = this;
			try
			{
				while (thread != null)
				{
					Socket socket = ss.Accept();
					socket.setTcpNoDelay(true);
					Stream In = socket.getInputStream();
					Stream Out = socket.getOutputStream();
					ChannelDirectTCPIP channel = new ChannelDirectTCPIP();
					channel.Init();
					channel.setInputStream(In);
					channel.setOutputStream(Out);
					session.addChannel(channel);
					((ChannelDirectTCPIP)channel).Host = host;
					((ChannelDirectTCPIP)channel).Port = rport;
					((ChannelDirectTCPIP)channel).OrgIPAddress = socket.getInetAddress().getHostAddress();
					((ChannelDirectTCPIP)channel).OrgPort = socket.Port;
					channel.connect();
					if (channel.ExitStatus != -1)
					{
					}
				}
			}
			catch { }

			delete();
		}

		internal void delete()
		{
			thread = null;
			try
			{
				if (ss != null) ss.Close();
				ss = null;
			}
			catch (Exception)
			{ }
		}
	}
}
