using System;
using System.Security.Cryptography;

namespace SharpSsh.jsch.jce
{
	public class SignatureRSA : SharpSsh.jsch.SignatureRSA
	{
		RSAParameters m_RSAKeyInfo;
		SHA1CryptoServiceProvider m_sha1;
		CryptoStream m_cs;

		public void init()
		{
			m_sha1 = new SHA1CryptoServiceProvider();
			m_cs = new CryptoStream(System.IO.Stream.Null, m_sha1, CryptoStreamMode.Write);
		}

		public void setPubKey(byte[] e, byte[] n)
		{
			m_RSAKeyInfo.Modulus = Util.StripLeadingZeros(n);
			m_RSAKeyInfo.Exponent = e;
		}

		public void setPrvKey(byte[] d, byte[] n)
		{
			m_RSAKeyInfo.D = d;
			m_RSAKeyInfo.Modulus = n;
		}

		public void setPrvKey(byte[] e, byte[] n, byte[] d, byte[] p, byte[] q, byte[] dp, byte[] dq, byte[] c)
		{
			m_RSAKeyInfo.Exponent = e;
			m_RSAKeyInfo.D = Util.StripLeadingZeros(d);
			m_RSAKeyInfo.Modulus = Util.StripLeadingZeros(n);
			m_RSAKeyInfo.P = Util.StripLeadingZeros(p);
			m_RSAKeyInfo.Q = Util.StripLeadingZeros(q);
			m_RSAKeyInfo.DP = Util.StripLeadingZeros(dp);
			m_RSAKeyInfo.DQ = Util.StripLeadingZeros(dq);
			m_RSAKeyInfo.InverseQ = Util.StripLeadingZeros(c);
		}

		public void setPrvKey(RSAParameters keyInfo)
		{
			m_RSAKeyInfo = keyInfo;
		}

		public byte[] sign()
		{
			m_cs.Close();
			RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
			RSA.ImportParameters(m_RSAKeyInfo);
			RSAPKCS1SignatureFormatter RSAFormatter = new RSAPKCS1SignatureFormatter(RSA);
			RSAFormatter.SetHashAlgorithm("SHA1");

			return RSAFormatter.CreateSignature(m_sha1);
		}

		public void update(byte[] foo)
		{
			m_cs.Write(foo, 0, foo.Length);
		}

		public bool verify(byte[] sig)
		{
			m_cs.Close();
			RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
			RSA.ImportParameters(m_RSAKeyInfo);
			RSAPKCS1SignatureDeformatter RSADeformatter = new RSAPKCS1SignatureDeformatter(RSA);
			RSADeformatter.SetHashAlgorithm("SHA1");


			long i = 0;
			long j = 0;
			byte[] tmp;

			if (sig[0] == 0 && sig[1] == 0 && sig[2] == 0)
			{
				long i1 = (sig[i++] << 24) & 0xff000000;
				long i2 = (sig[i++] << 16) & 0x00ff0000;
				long i3 = (sig[i++] << 8) & 0x0000ff00;
				long i4 = (sig[i++]) & 0x000000ff;
				j = i1 | i2 | i3 | i4;

				i += j;

				i1 = (sig[i++] << 24) & 0xff000000;
				i2 = (sig[i++] << 16) & 0x00ff0000;
				i3 = (sig[i++] << 8) & 0x0000ff00;
				i4 = (sig[i++]) & 0x000000ff;
				j = i1 | i2 | i3 | i4;

				tmp = new byte[j];
				Array.Copy(sig, i, tmp, 0, j);
				sig = tmp;
			}
			return RSADeformatter.VerifySignature(m_sha1, sig);
		}
	}
}

