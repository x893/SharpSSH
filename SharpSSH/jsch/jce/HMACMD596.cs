using System;
using System.Security.Cryptography;

namespace SharpSsh.jsch.jce
{
	public class HMACMD596 : MAC
	{
		private const string m_name = "hmac-md5-96";
		private const int m_bsize = 12;
		private Org.Mentalis.Security.Cryptography.HMAC m_mentalis_mac;
		private CryptoStream m_cs;
		private byte[] m_tmp = new byte[4];
		private byte[] m_buf = new byte[12];

		public int BlockSize { get { return m_bsize; } }
		public string Name { get { return m_name; } }

		public void init(byte[] key)
		{
			if (key.Length > 16)
			{
				byte[] tmp = new byte[16];
				Array.Copy(key, 0, tmp, 0, 16);
				key = tmp;
			}
			m_mentalis_mac = new Org.Mentalis.Security.Cryptography.HMAC(new MD5CryptoServiceProvider(), key);
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
			Array.Copy(m_mentalis_mac.Hash, 0, m_buf, 0, 12);
			return m_buf;
		}
	}
}
