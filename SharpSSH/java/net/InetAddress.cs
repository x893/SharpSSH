using System;
using System.Net;

namespace SharpSsh.java.net
{
	/// <summary>
	/// Summary description for InetAddress.
	/// </summary>
	public class InetAddress
	{
		private IPAddress m_addr;

		public IPAddress IPAddress
		{
			get { return m_addr; }
		}

		public InetAddress(string addr)
		{
			m_addr = IPAddress.Parse(addr);
		}
		public InetAddress(IPAddress addr)
		{
			m_addr = addr;
		}

		public bool isAnyLocalAddress()
		{
			return IPAddress.IsLoopback(m_addr);
		}

		public bool equals(InetAddress addr)
		{
			return addr.ToString().Equals(addr.ToString());
		}

		public bool equals(string addr)
		{
			return addr.ToString().Equals(addr.ToString());
		}

		public override string ToString()
		{
			return m_addr.ToString();
		}

		public override bool Equals(object obj)
		{
			return equals(obj.ToString());
		}

		public string getHostAddress()
		{
			return ToString();
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public static InetAddress getByName(string name)
		{
			return new InetAddress(Dns.GetHostEntry(name).AddressList[0]);
		}
	}
}
