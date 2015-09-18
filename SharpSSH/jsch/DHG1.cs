using System;

namespace SharpSsh.jsch
{
	public class DHG1 : KeyExchange
	{
		internal static byte[] m_g = new byte[] { 2 };
		internal static byte[] m_p = new byte[]{
			(byte)0x00,
			(byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,
			(byte)0xC9,(byte)0x0F,(byte)0xDA,(byte)0xA2,(byte)0x21,(byte)0x68,(byte)0xC2,(byte)0x34,
			(byte)0xC4,(byte)0xC6,(byte)0x62,(byte)0x8B,(byte)0x80,(byte)0xDC,(byte)0x1C,(byte)0xD1,
			(byte)0x29,(byte)0x02,(byte)0x4E,(byte)0x08,(byte)0x8A,(byte)0x67,(byte)0xCC,(byte)0x74,
			(byte)0x02,(byte)0x0B,(byte)0xBE,(byte)0xA6,(byte)0x3B,(byte)0x13,(byte)0x9B,(byte)0x22,
			(byte)0x51,(byte)0x4A,(byte)0x08,(byte)0x79,(byte)0x8E,(byte)0x34,(byte)0x04,(byte)0xDD,
			(byte)0xEF,(byte)0x95,(byte)0x19,(byte)0xB3,(byte)0xCD,(byte)0x3A,(byte)0x43,(byte)0x1B,
			(byte)0x30,(byte)0x2B,(byte)0x0A,(byte)0x6D,(byte)0xF2,(byte)0x5F,(byte)0x14,(byte)0x37,
			(byte)0x4F,(byte)0xE1,(byte)0x35,(byte)0x6D,(byte)0x6D,(byte)0x51,(byte)0xC2,(byte)0x45,
			(byte)0xE4,(byte)0x85,(byte)0xB5,(byte)0x76,(byte)0x62,(byte)0x5E,(byte)0x7E,(byte)0xC6,
			(byte)0xF4,(byte)0x4C,(byte)0x42,(byte)0xE9,(byte)0xA6,(byte)0x37,(byte)0xED,(byte)0x6B,
			(byte)0x0B,(byte)0xFF,(byte)0x5C,(byte)0xB6,(byte)0xF4,(byte)0x06,(byte)0xB7,(byte)0xED,
			(byte)0xEE,(byte)0x38,(byte)0x6B,(byte)0xFB,(byte)0x5A,(byte)0x89,(byte)0x9F,(byte)0xA5,
			(byte)0xAE,(byte)0x9F,(byte)0x24,(byte)0x11,(byte)0x7C,(byte)0x4B,(byte)0x1F,(byte)0xE6,
			(byte)0x49,(byte)0x28,(byte)0x66,(byte)0x51,(byte)0xEC,(byte)0xE6,(byte)0x53,(byte)0x81,
			(byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF,(byte)0xFF
		};

		internal const int SSH_MSG_KEXDH_INIT = 30;
		internal const int SSH_MSG_KEXDH_REPLY = 31;

		internal const int RSA = 0;
		internal const int DSS = 1;
		private int m_type = 0;
		private int m_state;
		internal DH m_dh;

		internal byte[] m_V_S;
		internal byte[] m_V_C;
		internal byte[] m_I_S;
		internal byte[] m_I_C;

		internal byte[] m_e;

		private Buffer m_buf;
		private Packet m_packet;

		public override void init(Session session, byte[] V_S, byte[] V_C, byte[] I_S, byte[] I_C)
		{
			m_session = session;
			m_V_S = V_S;
			m_V_C = V_C;
			m_I_S = I_S;
			m_I_C = I_C;
			try
			{
				Type t = Type.GetType(session.getConfig("sha-1"));
				m_sha = (HASH)(Activator.CreateInstance(t));
				m_sha.init();
			}
			catch (Exception ex)
			{
				throw ex;
			}

			m_buf = new Buffer();
			m_packet = new Packet(m_buf);

			try
			{
				Type t = Type.GetType(session.getConfig("dh"));
				m_dh = (DH)(Activator.CreateInstance(t));
				m_dh.init();
			}
			catch (Exception ee)
			{
				throw ee;
			}

			m_dh.P = m_p;
			m_dh.G = m_g;

			// The client responds with:
			// byte  SSH_MSG_KEXDH_INIT(30)
			// mpint e <- g^x mod p
			//         x is a random number (1 < x < (p-1)/2)

			m_e = m_dh.E;

			m_packet.reset();
			m_buf.putByte((byte)SSH_MSG_KEXDH_INIT);
			m_buf.putMPInt(m_e);
			session.write(m_packet);

			m_state = SSH_MSG_KEXDH_REPLY;
		}

		public override bool next(Buffer _buf)
		{
			int i, j;
			bool result = false;
			switch (m_state)
			{
				case SSH_MSG_KEXDH_REPLY:
					// The server responds with:
					// byte      SSH_MSG_KEXDH_REPLY(31)
					// string    server public host key and certificates (K_S)
					// mpint     f
					// string    signature of H
					j = _buf.getInt();
					j = _buf.getByte();
					j = _buf.getByte();
					if (j != 31)
					{
						result = false;
						break;
					}

					m_K_S = _buf.getString();
					// K_S is server_key_blob, which includes ....
					// string ssh-dss
					// impint p of dsa
					// impint q of dsa
					// impint g of dsa
					// impint pub_key of dsa

					byte[] f = _buf.getMPInt();
					byte[] sig_of_H = _buf.getString();

					m_dh.F = f;
					m_K = m_dh.K;

					//The hash H is computed as the HASH hash of the concatenation of the
					//following:
					// string    V_C, the client's version string (CR and NL excluded)
					// string    V_S, the server's version string (CR and NL excluded)
					// string    I_C, the payload of the client's SSH_MSG_KEXINIT
					// string    I_S, the payload of the server's SSH_MSG_KEXINIT
					// string    K_S, the host key
					// mpint     e, exchange value sent by the client
					// mpint     f, exchange value sent by the server
					// mpint     K, the shared secret
					// This value is called the exchange hash, and it is used to authenti-
					// cate the key exchange.
					m_buf.reset();
					m_buf.putString(m_V_C); m_buf.putString(m_V_S);
					m_buf.putString(m_I_C); m_buf.putString(m_I_S);
					m_buf.putString(m_K_S);
					m_buf.putMPInt(m_e); m_buf.putMPInt(f);
					m_buf.putMPInt(m_K);
					byte[] foo = new byte[m_buf.Length];
					m_buf.getByte(foo);
					m_sha.update(foo, 0, foo.Length);
					m_H = m_sha.digest();

					i = 0;
					j = 0;
					j = (int)((m_K_S[i++] << 24) & 0xff000000) | ((m_K_S[i++] << 16) & 0x00ff0000) |
						((m_K_S[i++] << 8) & 0x0000ff00) | ((m_K_S[i++]) & 0x000000ff);
					string alg = Util.getString(m_K_S, i, j);
					i += j;

					result = false;

					if (alg.Equals("ssh-rsa"))
					{
						byte[] tmp;
						byte[] ee;
						byte[] n;

						m_type = RSA;

						j = (int)((m_K_S[i++] << 24) & 0xff000000) | ((m_K_S[i++] << 16) & 0x00ff0000) |
							((m_K_S[i++] << 8) & 0x0000ff00) | ((m_K_S[i++]) & 0x000000ff);
						tmp = new byte[j]; Array.Copy(m_K_S, i, tmp, 0, j); i += j;
						ee = tmp;
						j = (int)((m_K_S[i++] << 24) & 0xff000000) | ((m_K_S[i++] << 16) & 0x00ff0000) |
							((m_K_S[i++] << 8) & 0x0000ff00) | ((m_K_S[i++]) & 0x000000ff);
						tmp = new byte[j]; Array.Copy(m_K_S, i, tmp, 0, j); i += j;
						n = tmp;

						//	SignatureRSA sig=new SignatureRSA();
						//	sig.init();

						SignatureRSA sig = null;
						try
						{
							Type t = Type.GetType(m_session.getConfig("signature.rsa"));
							sig = (SignatureRSA)(Activator.CreateInstance(t));
							sig.init();
						}
						catch (Exception) { }

						sig.setPubKey(ee, n);
						sig.update(m_H);
						result = sig.verify(sig_of_H);
					}
					else if (alg.Equals("ssh-dss"))
					{
						byte[] q = null;
						byte[] tmp;
						byte[] p;
						byte[] g;

						m_type = DSS;

						j = (int)((m_K_S[i++] << 24) & 0xff000000) | ((m_K_S[i++] << 16) & 0x00ff0000) |
							((m_K_S[i++] << 8) & 0x0000ff00) | ((m_K_S[i++]) & 0x000000ff);
						tmp = new byte[j]; Array.Copy(m_K_S, i, tmp, 0, j); i += j;
						p = tmp;
						j = (int)((m_K_S[i++] << 24) & 0xff000000) | ((m_K_S[i++] << 16) & 0x00ff0000) |
							((m_K_S[i++] << 8) & 0x0000ff00) | ((m_K_S[i++]) & 0x000000ff);
						tmp = new byte[j]; Array.Copy(m_K_S, i, tmp, 0, j); i += j;
						q = tmp;
						j = (int)((m_K_S[i++] << 24) & 0xff000000) | ((m_K_S[i++] << 16) & 0x00ff0000) |
							((m_K_S[i++] << 8) & 0x0000ff00) | ((m_K_S[i++]) & 0x000000ff);
						tmp = new byte[j]; Array.Copy(m_K_S, i, tmp, 0, j); i += j;
						g = tmp;
						j = (int)((m_K_S[i++] << 24) & 0xff000000) | ((m_K_S[i++] << 16) & 0x00ff0000) |
							((m_K_S[i++] << 8) & 0x0000ff00) | ((m_K_S[i++]) & 0x000000ff);
						tmp = new byte[j]; Array.Copy(m_K_S, i, tmp, 0, j); i += j;
						f = tmp;

						//	SignatureDSA sig=new SignatureDSA();
						//	sig.init();
						SignatureDSA sig = null;
						try
						{
							Type t = Type.GetType(m_session.getConfig("signature.dss"));
							sig = (SignatureDSA)(Activator.CreateInstance(t));
							sig.init();
						}
						catch (Exception) { }

						sig.setPubKey(f, p, q, g);
						sig.update(m_H);
						result = sig.verify(sig_of_H);
					}
					else
					{
						throw new Exception("unknow alg");
					}
					m_state = STATE_END;
					break;
			}
			return result;
		}

		public override string getKeyType()
		{
			if (m_type == DSS)
				return "DSA";
			return "RSA";
		}

		public override int getState() { return m_state; }
	}
}
