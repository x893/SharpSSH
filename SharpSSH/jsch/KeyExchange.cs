using System;
using SharpSsh.java;

namespace SharpSsh.jsch
{
	public abstract class KeyExchange
	{
		internal const int PROPOSAL_KEX_ALGS = 0;
		internal const int PROPOSAL_SERVER_HOST_KEY_ALGS = 1;
		internal const int PROPOSAL_ENC_ALGS_CTOS = 2;
		internal const int PROPOSAL_ENC_ALGS_STOC = 3;
		internal const int PROPOSAL_MAC_ALGS_CTOS = 4;
		internal const int PROPOSAL_MAC_ALGS_STOC = 5;
		internal const int PROPOSAL_COMP_ALGS_CTOS = 6;
		internal const int PROPOSAL_COMP_ALGS_STOC = 7;
		internal const int PROPOSAL_LANG_CTOS = 8;
		internal const int PROPOSAL_LANG_STOC = 9;
		internal const int PROPOSAL_MAX = 10;

		internal static string kex = "diffie-hellman-group1-sha1";
		internal static string server_host_key = "ssh-rsa,ssh-dss";
		internal static string enc_c2s = "blowfish-cbc";
		internal static string enc_s2c = "blowfish-cbc";
		internal static string mac_c2s = "hmac-md5";
		internal static string mac_s2c = "hmac-md5";
		internal static string lang_c2s = "";
		internal static string lang_s2c = "";

		public const int STATE_END = 0;

		public string[] m_guess = null;
		protected Session m_session = null;
		protected HASH m_sha = null;
		protected byte[] m_K = null;
		protected byte[] m_H = null;
		protected byte[] m_K_S = null;

		public abstract void init(Session session, byte[] V_S, byte[] V_C, byte[] I_S, byte[] I_C);
		public abstract bool next(Buffer buf);
		public abstract string getKeyType();
		public abstract int getState();

		internal static string[] guess(byte[] I_S, byte[] I_C)
		{
			string[] guess = new string[PROPOSAL_MAX];
			Buffer sb = new Buffer(I_S);
			sb.OffSet = 17;
			Buffer cb = new Buffer(I_C);
			cb.OffSet = 17;

			for (int i = 0; i < PROPOSAL_MAX; i++)
			{
				byte[] sp = sb.getString();  // server proposal
				byte[] cp = cb.getString();  // client proposal

				int j = 0;
				int k = 0;

				while (j < cp.Length)
				{
					while (j < cp.Length && cp[j] != ',')
						j++;
					if (k == j)
						return null;

					string algorithm = Util.getString(cp, k, j - k);
					int l = 0;
					int m = 0;
					while (l < sp.Length)
					{
						while (l < sp.Length && sp[l] != ',')
							l++;
						if (m == l)
							return null;
						if (algorithm.Equals(Util.getString(sp, m, l - m)))
						{
							guess[i] = algorithm;
							goto BREAK;
						}
						l++;
						m = l;
					}
					j++;
					k = j;
				}

			BREAK:
				if (j == 0)
					guess[i] = "";
				else if (guess[i] == null)
					return null;
			}
			return guess;
		}

		public string getFingerPrint()
		{
			HASH hash = null;
			try
			{
				Type t = Type.GetType(m_session.getConfig("md5"));
				hash = (HASH)(Activator.CreateInstance(t));
			}
			catch (Exception) { }
			return Util.getFingerPrint(hash, getHostKey());
		}
		internal byte[] getK() { return m_K; }
		internal byte[] getH() { return m_H; }
		internal HASH getHash() { return m_sha; }
		internal byte[] getHostKey() { return m_K_S; }
	}
}
