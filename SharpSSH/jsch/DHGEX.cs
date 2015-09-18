using System;

namespace SharpSsh.jsch
{
	public class DHGEX : KeyExchange
	{

		internal const int SSH_MSG_KEX_DH_GEX_GROUP = 31;
		internal const int SSH_MSG_KEX_DH_GEX_INIT = 32;
		internal const int SSH_MSG_KEX_DH_GEX_REPLY = 33;

		internal static int m_min = 1024;

		//  static int min=512;
		internal static int m_preferred = 1024;
		internal static int m_max = 1024;

		//  static int preferred=1024;
		//  static int max=2000;

		internal const int RSA = 0;
		internal const int DSS = 1;
		private int m_type = 0;

		private int m_state;

		//  com.jcraft.jsch.DH dh;
		internal DH m_dh;

		internal byte[] m_V_S;
		internal byte[] m_V_C;
		internal byte[] m_I_S;
		internal byte[] m_I_C;

		private Buffer m_buf;
		private Packet m_packet;

		private byte[] m_p;
		private byte[] m_g;
		private byte[] m_e;

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
			catch (Exception) { }

			m_buf = new Buffer();
			m_packet = new Packet(m_buf);

			try
			{
				Type t = Type.GetType(session.getConfig("dh"));
				m_dh = (DH)(Activator.CreateInstance(t));
				m_dh.init();
			}
			catch (Exception e)
			{
				throw e;
			}

			m_packet.reset();
			m_buf.putByte((byte)0x22);
			m_buf.putInt(m_min);
			m_buf.putInt(m_preferred);
			m_buf.putInt(m_max);
			session.write(m_packet);

			m_state = SSH_MSG_KEX_DH_GEX_GROUP;
		}

		public override bool next(Buffer _buf)
		{
			int i, j;
			bool result = false;
			switch (m_state)
			{
				case SSH_MSG_KEX_DH_GEX_GROUP:
					// byte  SSH_MSG_KEX_DH_GEX_GROUP(31)
					// mpint p, safe prime
					// mpint g, generator for subgroup in GF (p)
					_buf.getInt();
					_buf.getByte();
					j = _buf.getByte();
					if (j != 31)
					{
						Console.WriteLine("type: must be 31 " + j);
						result = false;
					}

					m_p = _buf.getMPInt();
					m_g = _buf.getMPInt();
					m_dh.P = m_p;
					m_dh.G = m_g;

					// The client responds with:
					// byte  SSH_MSG_KEX_DH_GEX_INIT(32)
					// mpint e <- g^x mod p
					//         x is a random number (1 < x < (p-1)/2)

					m_e = m_dh.E;

					m_packet.reset();
					m_buf.putByte((byte)0x20);
					m_buf.putMPInt(m_e);
					m_session.write(m_packet);

					m_state = SSH_MSG_KEX_DH_GEX_REPLY;
					result = true;
					break;

				case SSH_MSG_KEX_DH_GEX_REPLY:
					// The server responds with:
					// byte      SSH_MSG_KEX_DH_GEX_REPLY(33)
					// string    server public host key and certificates (K_S)
					// mpint     f
					// string    signature of H
					j = _buf.getInt();
					j = _buf.getByte();
					j = _buf.getByte();
					if (j != 33)
					{
						//!!! Console.WriteLine("type: must be 33 " + j);
						result = false;
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
					// uint32    min, minimal size in bits of an acceptable group
					// uint32   n, preferred size in bits of the group the server should send
					// uint32    max, maximal size in bits of an acceptable group
					// mpint     p, safe prime
					// mpint     g, generator for subgroup
					// mpint     e, exchange value sent by the client
					// mpint     f, exchange value sent by the server
					// mpint     K, the shared secret
					// This value is called the exchange hash, and it is used to authenti-
					// cate the key exchange.

					m_buf.reset();
					m_buf.putString(m_V_C); m_buf.putString(m_V_S);
					m_buf.putString(m_I_C); m_buf.putString(m_I_S);
					m_buf.putString(m_K_S);
					m_buf.putInt(m_min); m_buf.putInt(m_preferred); m_buf.putInt(m_max);
					m_buf.putMPInt(m_p); m_buf.putMPInt(m_g); m_buf.putMPInt(m_e); m_buf.putMPInt(f);
					m_buf.putMPInt(m_K);

					byte[] foo = new byte[m_buf.Length];
					m_buf.getByte(foo);
					m_sha.update(foo, 0, foo.Length);
					m_sha.digest();

					i = 0;
					j = 0;
					j = (int)((m_K_S[i++] << 24) & 0xff000000) | ((m_K_S[i++] << 16) & 0x00ff0000) | ((m_K_S[i++] << 8) & 0x0000ff00) | ((m_K_S[i++]) & 0x000000ff);
					string alg = Util.getString(m_K_S, i, j);
					i += j;

					if (alg.Equals("ssh-rsa"))
					{
						byte[] tmp;
						byte[] ee;
						byte[] n;

						m_type = RSA;

						j = (int)((m_K_S[i++] << 24) & 0xff000000) | ((m_K_S[i++] << 16) & 0x00ff0000) | ((m_K_S[i++] << 8) & 0x0000ff00) | ((m_K_S[i++]) & 0x000000ff);
						tmp = new byte[j]; Array.Copy(m_K_S, i, tmp, 0, j); i += j;
						ee = tmp;
						j = (int)((m_K_S[i++] << 24) & 0xff000000) | ((m_K_S[i++] << 16) & 0x00ff0000) | ((m_K_S[i++] << 8) & 0x0000ff00) | ((m_K_S[i++]) & 0x000000ff);
						tmp = new byte[j]; Array.Copy(m_K_S, i, tmp, 0, j); i += j;
						n = tmp;

						SignatureRSA sig = null;
						try
						{
							Type t = Type.GetType(m_session.getConfig("signature.rsa"));
							sig = (SignatureRSA)(Activator.CreateInstance(t));
							sig.init();
						}
						catch (Exception eee)
						{
							Console.WriteLine(eee);
						}

						sig.setPubKey(ee, n);
						sig.update(m_H);
						result = sig.verify(sig_of_H);
					}
					else if (alg.Equals("ssh-dss"))
					{
						byte[] q = null;
						byte[] tmp;

						m_type = DSS;

						j = (int)
								((m_K_S[i++] << 24) & 0xff000000) |
								((m_K_S[i++] << 16) & 0x00ff0000) |
								((m_K_S[i++] << 8) & 0x0000ff00) |
								((m_K_S[i++]) & 0x000000ff);
						tmp = new byte[j];
						Array.Copy(m_K_S, i, tmp, 0, j);
						i += j;
						m_p = tmp;
						j = (int)
								((m_K_S[i++] << 24) & 0xff000000) |
								((m_K_S[i++] << 16) & 0x00ff0000) |
								((m_K_S[i++] << 8) & 0x0000ff00) |
								((m_K_S[i++]) & 0x000000ff);
						tmp = new byte[j];
						Array.Copy(m_K_S, i, tmp, 0, j);
						i += j;
						q = tmp;
						j = (int)
								((m_K_S[i++] << 24) & 0xff000000) |
								((m_K_S[i++] << 16) & 0x00ff0000) |
								((m_K_S[i++] << 8) & 0x0000ff00) |
								((m_K_S[i++]) & 0x000000ff);
						tmp = new byte[j];
						Array.Copy(m_K_S, i, tmp, 0, j);
						i += j;
						m_g = tmp;
						j = (int)
								((m_K_S[i++] << 24) & 0xff000000) |
								((m_K_S[i++] << 16) & 0x00ff0000) |
								((m_K_S[i++] << 8) & 0x0000ff00) |
								((m_K_S[i++]) & 0x000000ff);
						tmp = new byte[j];
						Array.Copy(m_K_S, i, tmp, 0, j);
						i += j;
						f = tmp;

						SignatureDSA sig = null;
						try
						{
							Type t = Type.GetType(m_session.getConfig("signature.dss"));
							sig = (SignatureDSA)(Activator.CreateInstance(t));
							sig.init();
						}
						catch (Exception ee)
						{
							Console.WriteLine(ee);
						}

						sig.setPubKey(f, m_p, q, m_g);
						sig.update(m_H);
						result = sig.verify(sig_of_H);
					}
					else
					{
						//!!! Console.WriteLine("unknow alg");
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
