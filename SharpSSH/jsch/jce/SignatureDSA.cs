using System;
using System.Security.Cryptography;

namespace SharpSsh.jsch.jce
{
	public class SignatureDSA : SharpSsh.jsch.SignatureDSA
	{
		DSAParameters m_DSAKeyInfo;
		SHA1CryptoServiceProvider m_sha1;
		CryptoStream m_cs;

		public void init()
		{
			m_sha1 = new SHA1CryptoServiceProvider();
			m_cs = new CryptoStream(System.IO.Stream.Null, m_sha1, CryptoStreamMode.Write);
		}

		public void setPubKey(byte[] y, byte[] p, byte[] q, byte[] g)
		{
			m_DSAKeyInfo.Y = Util.StripLeadingZeros(y);
			m_DSAKeyInfo.P = Util.StripLeadingZeros(p);
			m_DSAKeyInfo.Q = Util.StripLeadingZeros(q);
			m_DSAKeyInfo.G = Util.StripLeadingZeros(g);
		}
		public void setPrvKey(byte[] x, byte[] p, byte[] q, byte[] g)
		{
			m_DSAKeyInfo.X = Util.StripLeadingZeros(x);
			m_DSAKeyInfo.P = Util.StripLeadingZeros(p);
			m_DSAKeyInfo.Q = Util.StripLeadingZeros(q);
			m_DSAKeyInfo.G = Util.StripLeadingZeros(g);
		}

		public byte[] sign()
		{
			m_cs.Close();
			DSACryptoServiceProvider DSA = new DSACryptoServiceProvider();
			DSA.ImportParameters(m_DSAKeyInfo);
			DSASignatureFormatter DSAFormatter = new DSASignatureFormatter(DSA);
			DSAFormatter.SetHashAlgorithm("SHA1");

			byte[] sig = DSAFormatter.CreateSignature(m_sha1);
			return sig;
		}

		public void update(byte[] foo)
		{
			m_cs.Write(foo, 0, foo.Length);
		}

		public bool verify(byte[] sig)
		{
			m_cs.Close();
			DSACryptoServiceProvider DSA = new DSACryptoServiceProvider();
			DSA.ImportParameters(m_DSAKeyInfo);
			DSASignatureDeformatter DSADeformatter = new DSASignatureDeformatter(DSA);
			DSADeformatter.SetHashAlgorithm("SHA1");

			long i = 0;
			long j = 0;
			byte[] tmp;

			//This makes sure sig is always 40 bytes?
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
			return DSADeformatter.VerifySignature(m_sha1, sig);
		}
	}
}
