using System.Security.Cryptography;

namespace SharpSsh.jsch.jce
{
	public class KeyPairGenRSA : SharpSsh.jsch.KeyPairGenRSA
	{
		private byte[] m_d;
		private byte[] m_e;
		private byte[] m_n;

		private byte[] m_c; //  coefficient
		private byte[] m_ep; // exponent p
		private byte[] m_eq; // exponent q
		private byte[] m_p;  // prime p
		private byte[] m_q;  // prime q

		private RSAParameters m_RSAKeyInfo;

		public void init(int key_size)
		{
			RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(key_size);
			m_RSAKeyInfo = rsa.ExportParameters(true);

			m_d = m_RSAKeyInfo.D;
			m_e = m_RSAKeyInfo.Exponent;
			m_n = m_RSAKeyInfo.Modulus;

			m_c = m_RSAKeyInfo.InverseQ;
			m_ep = m_RSAKeyInfo.DP;
			m_eq = m_RSAKeyInfo.DQ;
			m_p = m_RSAKeyInfo.P;
			m_q = m_RSAKeyInfo.Q;
		}

		public byte[] D { get { return m_d; } }
		public byte[] E { get { return m_e; } }
		public byte[] N { get { return m_n; } }
		public byte[] C { get { return m_c; } }
		public byte[] EP { get { return m_ep; } }
		public byte[] EQ { get { return m_eq; } }
		public byte[] P { get { return m_p; } }
		public byte[] Q { get { return m_q; } }

		public RSAParameters KeyInfo
		{
			get { return m_RSAKeyInfo; }
		}
	}
}
