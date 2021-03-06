using System;
using System.Security.Cryptography;

namespace SharpSsh.jsch.jce
{
	public class AES128CBC : Cipher
	{
		private int m_mode;
		private const int m_ivsize = 16;
		private const int m_bsize = 16;
		private RijndaelManaged m_rijndael;
		private ICryptoTransform m_cipher;

		public override int IVSize { get { return m_ivsize; } }
		public override int BlockSize { get { return m_bsize; } }

		public override void init(int mode, byte[] key, byte[] iv)
		{
			m_mode = mode;
			m_rijndael = new RijndaelManaged();
			m_rijndael.Mode = CipherMode.CBC;
			m_rijndael.Padding = PaddingMode.None;

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
					? m_rijndael.CreateEncryptor(key, iv)
					: m_rijndael.CreateDecryptor(key, iv)
					);
			}
			catch (Exception)
			{
				m_cipher = null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="inputBuffer"></param>
		/// <param name="inputOffset"></param>
		/// <param name="inputCount"></param>
		/// <param name="outputBuffer"></param>
		/// <param name="outputOffset"></param>
		public override void update(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
		{
			m_cipher.TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
		}

		public override string ToString()
		{
			return "aes128-cbc";
		}
	}
}
