using System.Security.Cryptography;

namespace SharpSsh.jsch.jce
{
	public class KeyPairGenDSA : SharpSsh.jsch.KeyPairGenDSA
	{
		private byte[] m_x;
		private byte[] m_y;
		private byte[] m_p;
		private byte[] m_q;
		private byte[] m_g;

		public void init(int key_size)
		{
			DSACryptoServiceProvider dsa = new DSACryptoServiceProvider(key_size);
			DSAParameters DSAKeyInfo = dsa.ExportParameters(true);

			m_x = DSAKeyInfo.X;
			m_y = DSAKeyInfo.Y;
			m_p = DSAKeyInfo.P;
			m_q = DSAKeyInfo.Q;
			m_g = DSAKeyInfo.G;
		}

		public byte[] X { get { return m_x; } }
		public byte[] Y { get { return m_y; } }
		public byte[] P { get { return m_p; } }
		public byte[] Q { get { return m_q; } }
		public byte[] G { get { return m_g; } }
	}
}

