using System;

namespace SharpSsh.jsch
{
	public class KeyPairDSA : KeyPair
	{
		private byte[] m_P_array;
		private byte[] m_Q_array;
		private byte[] m_G_array;
		private byte[] m_pub_array;
		private byte[] m_prv_array;
		private int m_key_size = 1024;
		private static byte[] sshdss = Util.getBytes("ssh-dss");
		private static byte[] begin = Util.getBytes("-----BEGIN DSA PRIVATE KEY-----");
		private static byte[] end = Util.getBytes("-----END DSA PRIVATE KEY-----");

		public KeyPairDSA(JSch jsch)
			: base(jsch)
		{
		}

		internal override void generate(int key_size)
		{
			this.m_key_size = key_size;
			try
			{
				Type t = Type.GetType(m_jsch.getConfig("keypairgen.dsa"));
				KeyPairGenDSA keypairgen = (KeyPairGenDSA)(Activator.CreateInstance(t));
				keypairgen.init(key_size);
				m_P_array = keypairgen.P;
				m_Q_array = keypairgen.Q;
				m_G_array = keypairgen.G;
				m_pub_array = keypairgen.Y;
				m_prv_array = keypairgen.X;

				keypairgen = null;
			}
			catch (Exception e)
			{
				throw new JSchException(e.ToString());
			}
		}

		internal override byte[] getBegin() { return begin; }
		internal override byte[] getEnd() { return end; }

		internal override byte[] getPrivateKey()
		{
			int content =
				1 + countLength(1) + 1 +                           // INTEGER
				1 + countLength(m_P_array.Length) + m_P_array.Length + // INTEGER  P
				1 + countLength(m_Q_array.Length) + m_Q_array.Length + // INTEGER  Q
				1 + countLength(m_G_array.Length) + m_G_array.Length + // INTEGER  G
				1 + countLength(m_pub_array.Length) + m_pub_array.Length + // INTEGER  pub
				1 + countLength(m_prv_array.Length) + m_prv_array.Length;  // INTEGER  prv

			int total =
				1 + countLength(content) + content;   // SEQUENCE

			byte[] plain = new byte[total];
			int index = 0;
			index = writeSEQUENCE(plain, index, content);
			index = writeINTEGER(plain, index, new byte[1]);  // 0
			index = writeINTEGER(plain, index, m_P_array);
			index = writeINTEGER(plain, index, m_Q_array);
			index = writeINTEGER(plain, index, m_G_array);
			index = writeINTEGER(plain, index, m_pub_array);
			index = writeINTEGER(plain, index, m_prv_array);
			return plain;
		}

		internal override bool parse(byte[] plain)
		{
			try
			{
				if (vendor == VENDOR_FSECURE)
				{
					if (plain[0] != 0x30)
					{              // FSecure
						Buffer buf = new Buffer(plain);
						buf.getInt();
						m_P_array = buf.getMPIntBits();
						m_G_array = buf.getMPIntBits();
						m_Q_array = buf.getMPIntBits();
						m_pub_array = buf.getMPIntBits();
						m_prv_array = buf.getMPIntBits();
						return true;
					}
					return false;
				}

				int index = 0;
				int Length = 0;

				if (plain[index] != 0x30) return false;
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
				m_P_array = new byte[Length];
				Array.Copy(plain, index, m_P_array, 0, Length);
				index += Length;

				index++;
				Length = plain[index++] & 0xff;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f; Length = 0;
					while (foo-- > 0) { Length = (Length << 8) + (plain[index++] & 0xff); }
				}
				m_Q_array = new byte[Length];
				Array.Copy(plain, index, m_Q_array, 0, Length);
				index += Length;

				index++;
				Length = plain[index++] & 0xff;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f; Length = 0;
					while (foo-- > 0) { Length = (Length << 8) + (plain[index++] & 0xff); }
				}
				m_G_array = new byte[Length];
				Array.Copy(plain, index, m_G_array, 0, Length);
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
			}
			catch
			{
				return false;
			}
			return true;
		}

		public override byte[] getPublicKeyBlob()
		{
			byte[] blob = base.getPublicKeyBlob();
			if (blob != null)
				return blob;

			if (m_P_array == null)
				return null;

			Buffer buf = new Buffer(sshdss.Length + 4 +
									m_P_array.Length + 4 +
									m_Q_array.Length + 4 +
									m_G_array.Length + 4 +
									m_pub_array.Length + 4);
			buf.putString(sshdss);
			buf.putString(m_P_array);
			buf.putString(m_Q_array);
			buf.putString(m_G_array);
			buf.putString(m_pub_array);
			return buf.m_buffer;
		}

		internal override byte[] getKeyTypeName() { return sshdss; }
		public override int getKeyType() { return DSA; }

		public override int getKeySize() { return m_key_size; }
		public override void dispose()
		{
			base.dispose();
			m_P_array = null;
			m_Q_array = null;
			m_G_array = null;
			m_pub_array = null;
			m_prv_array = null;
		}
	}
}
