using System;
using System.Security.Cryptography;

namespace SharpSsh.jsch.jce
{
	public class TripleDESCBC : SharpSsh.jsch.Cipher
	{
		private const int m_ivsize = 8;
		private const int m_bsize = 24;
		private TripleDES m_triDes;
		private ICryptoTransform m_cipher;

		public override int IVSize { get { return m_ivsize; } }
		public override int BlockSize { get { return m_bsize; } }

		public override void init(int mode, byte[] key, byte[] iv)
		{
			m_triDes = new TripleDESCryptoServiceProvider();
			m_triDes.Mode = CipherMode.CBC;
			m_triDes.Padding = PaddingMode.None;

			byte[] tmp;
			if (iv.Length > m_ivsize)
			{
				tmp = new byte[m_ivsize];
				Array.Copy(iv, 0, tmp, 0, tmp.Length);
				iv = tmp;
			}
			if (key.Length > m_bsize)
			{
				tmp = new byte[m_bsize];
				Array.Copy(key, 0, tmp, 0, tmp.Length);
				key = tmp;
			}

			try
			{
				m_cipher = (mode == ENCRYPT_MODE
					? m_triDes.CreateEncryptor(key, iv)
					: m_triDes.CreateDecryptor(key, iv)
					);
			}
			catch (Exception)
			{
				m_cipher = null;
			}
		}

		public override void update(byte[] foo, int s1, int len, byte[] bar, int s2)
		{
			m_cipher.TransformBlock(foo, s1, len, bar, s2);
		}

		public override string ToString()
		{
			return "3des-cbc";
		}
	}
}

