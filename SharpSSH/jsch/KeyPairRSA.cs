using System;

namespace SharpSsh.jsch
{
	public class KeyPairRSA : KeyPair
	{
		private byte[] m_prv_array;
		private byte[] m_pub_array;
		private byte[] m_n_array;

		private byte[] m_p_array;  // prime p
		private byte[] m_q_array;  // prime q
		private byte[] m_ep_array; // prime exponent p
		private byte[] m_eq_array; // prime exponent q
		private byte[] m_c_array;  // coefficient

		private static byte[] m_begin = Util.getBytes("-----BEGIN RSA PRIVATE KEY-----");
		private static byte[] m_end = Util.getBytes("-----END RSA PRIVATE KEY-----");

		private int m_key_size = 1024;
		private static byte[] m_sshrsa = Util.getBytes("ssh-rsa");

		public KeyPairRSA(JSch jsch)
			: base(jsch)
		{
		}

		internal override void generate(int key_size)
		{
			m_key_size = key_size;
			try
			{
				Type t = Type.GetType(m_jsch.getConfig("keypairgen.rsa"));
				KeyPairGenRSA keypairgen = (KeyPairGenRSA)(Activator.CreateInstance(t));
				keypairgen.init(key_size);
				m_pub_array = keypairgen.E;
				m_prv_array = keypairgen.D;
				m_n_array = keypairgen.N;

				m_p_array = keypairgen.P;
				m_q_array = keypairgen.Q;
				m_ep_array = keypairgen.EP;
				m_eq_array = keypairgen.EQ;
				m_c_array = keypairgen.C;

				keypairgen = null;
			}
			catch (Exception e)
			{
				throw new JSchException(e.ToString());
			}
		}

		internal override byte[] getBegin() { return m_begin; }
		internal override byte[] getEnd() { return m_end; }

		internal override byte[] getPrivateKey()
		{
			int content =
				1 + countLength(1) + 1 +                               // INTEGER
				1 + countLength(m_n_array.Length) + m_n_array.Length + // INTEGER  N
				1 + countLength(m_pub_array.Length) + m_pub_array.Length + // INTEGER  pub
				1 + countLength(m_prv_array.Length) + m_prv_array.Length +  // INTEGER  prv
				1 + countLength(m_p_array.Length) + m_p_array.Length +      // INTEGER  p
				1 + countLength(m_q_array.Length) + m_q_array.Length +      // INTEGER  q
				1 + countLength(m_ep_array.Length) + m_ep_array.Length +    // INTEGER  ep
				1 + countLength(m_eq_array.Length) + m_eq_array.Length +    // INTEGER  eq
				1 + countLength(m_c_array.Length) + m_c_array.Length;      // INTEGER  c

			int total =
				1 + countLength(content) + content;   // SEQUENCE

			byte[] plain = new byte[total];
			int index = 0;
			index = writeSEQUENCE(plain, index, content);
			index = writeINTEGER(plain, index, new byte[1]);  // 0
			index = writeINTEGER(plain, index, m_n_array);
			index = writeINTEGER(plain, index, m_pub_array);
			index = writeINTEGER(plain, index, m_prv_array);
			index = writeINTEGER(plain, index, m_p_array);
			index = writeINTEGER(plain, index, m_q_array);
			index = writeINTEGER(plain, index, m_ep_array);
			index = writeINTEGER(plain, index, m_eq_array);
			index = writeINTEGER(plain, index, m_c_array);
			return plain;
		}

		internal override bool parse(byte[] plain)
		{
			/*
			byte[] p_array;
			byte[] q_array;
			byte[] dmp1_array;
			byte[] dmq1_array;
			byte[] iqmp_array;
			*/
			try
			{
				int index = 0;
				int Length = 0;

				if (vendor == VENDOR_FSECURE)
				{
					if (plain[index] != 0x30)
					{                  // FSecure
						Buffer buf = new Buffer(plain);
						m_pub_array = buf.getMPIntBits();
						m_prv_array = buf.getMPIntBits();
						m_n_array = buf.getMPIntBits();
						byte[] u_array = buf.getMPIntBits();
						m_p_array = buf.getMPIntBits();
						m_q_array = buf.getMPIntBits();
						return true;
					}
					return false;
				}

				index++; // SEQUENCE
				Length = plain[index++] & 0xff;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f; Length = 0;
					while (foo-- > 0) { Length = (Length << 8) + (plain[index++] & 0xff); }
				}

				if (plain[index] != 0x02) return false;
				index++; // INTEGER
				Length = plain[index++] & 0xff;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f; Length = 0;
					while (foo-- > 0) { Length = (Length << 8) + (plain[index++] & 0xff); }
				}
				index += Length;

				index++;
				Length = plain[index++] & 0xff;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f; Length = 0;
					while (foo-- > 0) { Length = (Length << 8) + (plain[index++] & 0xff); }
				}
				m_n_array = new byte[Length];
				Array.Copy(plain, index, m_n_array, 0, Length);
				index += Length;

				index++;
				Length = plain[index++] & 0xff;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f; Length = 0;
					while (foo-- > 0) { Length = (Length << 8) + (plain[index++] & 0xff); }
				}
				m_pub_array = new byte[Length];
				Array.Copy(plain, index, m_pub_array, 0, Length);
				index += Length;

				index++;
				Length = plain[index++] & 0xff;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f; Length = 0;
					while (foo-- > 0) { Length = (Length << 8) + (plain[index++] & 0xff); }
				}
				m_prv_array = new byte[Length];
				Array.Copy(plain, index, m_prv_array, 0, Length);
				index += Length;

				index++;
				Length = plain[index++] & 0xff;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f; Length = 0;
					while (foo-- > 0) { Length = (Length << 8) + (plain[index++] & 0xff); }
				}
				m_p_array = new byte[Length];
				Array.Copy(plain, index, m_p_array, 0, Length);
				index += Length;

				index++;
				Length = plain[index++] & 0xff;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f; Length = 0;
					while (foo-- > 0) { Length = (Length << 8) + (plain[index++] & 0xff); }
				}
				m_q_array = new byte[Length];
				Array.Copy(plain, index, m_q_array, 0, Length);
				index += Length;

				index++;
				Length = plain[index++] & 0xff;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f; Length = 0;
					while (foo-- > 0) { Length = (Length << 8) + (plain[index++] & 0xff); }
				}
				m_ep_array = new byte[Length];
				Array.Copy(plain, index, m_ep_array, 0, Length);
				index += Length;

				index++;
				Length = plain[index++] & 0xff;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f; Length = 0;
					while (foo-- > 0) { Length = (Length << 8) + (plain[index++] & 0xff); }
				}
				m_eq_array = new byte[Length];
				Array.Copy(plain, index, m_eq_array, 0, Length);
				index += Length;

				index++;
				Length = plain[index++] & 0xff;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f; Length = 0;
					while (foo-- > 0) { Length = (Length << 8) + (plain[index++] & 0xff); }
				}
				m_c_array = new byte[Length];
				Array.Copy(plain, index, m_c_array, 0, Length);
				index += Length;
			}
			catch
			{
				return false;
			}
			return true;
		}


		public override byte[] getPublicKeyBlob()
		{
			byte[] foo = base.getPublicKeyBlob();
			if (foo != null) return foo;

			if (m_pub_array == null) return null;

			Buffer buf = new Buffer(m_sshrsa.Length + 4 +
				m_pub_array.Length + 4 +
				m_n_array.Length + 4);
			buf.putString(m_sshrsa);
			buf.putString(m_pub_array);
			buf.putString(m_n_array);
			return buf.m_buffer;
		}

		internal override byte[] getKeyTypeName() { return m_sshrsa; }
		public override int getKeyType() { return RSA; }

		public override int getKeySize() { return m_key_size; }
		public override void dispose()
		{
			base.dispose();
			m_pub_array = null;
			m_prv_array = null;
			m_n_array = null;

			m_p_array = null;
			m_q_array = null;
			m_ep_array = null;
			m_eq_array = null;
			m_c_array = null;
		}
	}
}
