using System;
using Org.Mentalis.Security.Cryptography;

namespace SharpSsh.jsch.jce
{
	public class DH : SharpSsh.jsch.DH
	{
		internal byte[] m_p;
		internal byte[] m_g;
		internal byte[] m_e_array;
		internal byte[] m_f;  // your public key
							  // internal byte[] K;  // shared secret key
		internal byte[] m_K_array;
		private DiffieHellman m_dh;

		public void init()
		{
		}

		public byte[] E
		{
			get
			{
				if (m_e_array == null)
				{
					m_dh = new DiffieHellmanManaged(m_p, m_g, 0);
					m_e_array = m_dh.CreateKeyExchange();
				}
				return m_e_array;
			}
		}

		public byte[] K
		{
			get
			{
				if (m_K_array == null)
					m_K_array = m_dh.DecryptKeyExchange(m_f);
				return m_K_array;
			}
		}
		public byte[] P { set { m_p = value; } }
		public byte[] G { set { m_g = value; } }
		public byte[] F { set { m_f = value; } }
		//  void setP(BigInteger p){this.p=p;}
		//  void setG(BigInteger g){this.g=g;}
		//  void setF(BigInteger f){this.f=f;}
	}
}
