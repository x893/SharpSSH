using System;
using System.Security.Cryptography;

namespace SharpSsh.jsch.jce
{
	public class SHA1 : SharpSsh.jsch.HASH
	{
		internal SHA1CryptoServiceProvider m_md;
		private CryptoStream m_cs;

		public override int BlockSize { get { return 20; } }

		public override void init()
		{
			try
			{
				m_md = new SHA1CryptoServiceProvider();
				m_cs = new CryptoStream(System.IO.Stream.Null, m_md, CryptoStreamMode.Write);
			}
			catch (Exception) { }
		}
		public override void update(byte[] foo, int start, int len)
		{
			m_cs.Write(foo, start, len);
		}

		public override byte[] digest()
		{
			m_cs.Close();
			byte[] result = m_md.Hash;
			m_md.Clear();
			init(); //Reinitiazing hash objects
			return result;
		}
	}
}
