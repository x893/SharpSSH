using System;
using System.Security.Cryptography;

namespace SharpSsh.jsch.jce
{
	public class HMACSHA1 : MAC
	{
		private const string m_name = "hmac-sha1";
		private const int m_bsize = 20;
		private Org.Mentalis.Security.Cryptography.HMAC m_mentalis_mac;
		private CryptoStream m_cs;
		private byte[] m_tmp = new byte[4];

		public int BlockSize { get { return m_bsize; } }
		public string Name { get { return m_name; } }

		public void init(byte[] key)
		{
			if (key.Length > m_bsize)
			{
				byte[] tmp = new byte[m_bsize];
				Array.Copy(key, 0, tmp, 0, m_bsize);
				key = tmp;
			}
			m_mentalis_mac = new Org.Mentalis.Security.Cryptography.HMAC(new SHA1CryptoServiceProvider(), key);
			m_cs = new CryptoStream(System.IO.Stream.Null, m_mentalis_mac, CryptoStreamMode.Write);
		}

		public void update(int i)
		{
			m_tmp[0] = (byte)(i >> 24);
			m_tmp[1] = (byte)(i >> 16);
			m_tmp[2] = (byte)(i >> 8);
			m_tmp[3] = (byte)i;
			update(m_tmp, 0, 4);
		}

		public void update(byte[] foo, int s, int l)
		{
			m_cs.Write(foo, s, l);
		}

		public byte[] doFinal()
		{
			m_cs.Close();
			byte[] result = m_mentalis_mac.Hash;
			byte[] key = m_mentalis_mac.Key;
			m_mentalis_mac.Clear();
			init(key);

			return result;
		}
	}
}
