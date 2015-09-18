using System;
using System.Security.Cryptography;

namespace SharpSsh.jsch.jce
{
	public class Random : SharpSsh.jsch.Random
	{
		private byte[] m_tmp = new byte[16];
		private RNGCryptoServiceProvider m_rand;
		private static int m_times = 0;

		public Random()
		{
			m_rand = new RNGCryptoServiceProvider();
		}

		public void fill(byte[] foo, int start, int len)
		{
			try
			{
				if (len > m_tmp.Length) { m_tmp = new byte[len]; }
				m_rand.GetBytes(m_tmp);
				Array.Copy(m_tmp, 0, foo, start, len);
			}
			catch (Exception)
			{
				m_times++;
			}
		}
	}
}

