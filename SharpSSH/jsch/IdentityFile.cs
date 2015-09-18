using System;
using System.IO;

namespace SharpSsh.jsch
{
	internal class IdentityFile : Identity
	{
		private const int ERROR = 0;
		private const int RSA = 1;
		private const int DSS = 2;
		internal const int UNKNOWN = 3;

		private const int OPENSSH = 0;
		private const int FSECURE = 1;
		private const int PUTTY = 2;

		private string m_identity;
		private byte[] m_key;
		private byte[] m_iv;
		private JSch m_jsch;
		private HASH m_hash;
		private byte[] m_encoded_data;

		private Cipher m_cipher;

		// DSA
		private byte[] m_P_array;
		private byte[] m_Q_array;
		private byte[] m_G_array;
		private byte[] m_pub_array;
		private byte[] m_prv_array;

		// RSA
		private byte[] m_n_array;   // modulus
		private byte[] m_e_array;   // public exponent
		private byte[] m_d_array;   // private exponent

		private byte[] m_p_array;
		private byte[] m_q_array;
		private byte[] m_dmp1_array;
		private byte[] m_dmq1_array;
		private byte[] m_iqmp_array;

		private string m_algname_DSS = "ssh-dss";
		private string malgname_RSA = "ssh-rsa";

		private int m_type = ERROR;
		private int m_keytype = OPENSSH;

		private byte[] m_publickeyblob = null;

		private bool m_encrypted = true;

		internal IdentityFile(string identity, JSch jsch)
		{
			m_identity = identity;
			m_jsch = jsch;
			try
			{
				Type c = Type.GetType(jsch.getConfig("3des-cbc"));
				m_cipher = (Cipher)Activator.CreateInstance(c);
				m_key = new byte[m_cipher.BlockSize];   // 24
				m_iv = new byte[m_cipher.IVSize];       // 8
				c = Type.GetType(jsch.getConfig("md5"));
				m_hash = (HASH)(Activator.CreateInstance(c));
				m_hash.init();
				FileInfo file = new FileInfo(identity);
				FileStream fis = File.OpenRead(identity);
				byte[] buf = new byte[(int)(file.Length)];
				int len = fis.Read(buf, 0, buf.Length);
				fis.Close();

				int i = 0;
				while (i < len)
				{
					if (buf[i] == 'B' && buf[i + 1] == 'E' && buf[i + 2] == 'G' && buf[i + 3] == 'I')
					{
						i += 6;
						if (buf[i] == 'D' && buf[i + 1] == 'S' && buf[i + 2] == 'A')
							m_type = DSS;
						else if (buf[i] == 'R' && buf[i + 1] == 'S' && buf[i + 2] == 'A')
							m_type = RSA;
						else if (buf[i] == 'S' && buf[i + 1] == 'S' && buf[i + 2] == 'H')
						{   // FSecure
							m_type = UNKNOWN;
							m_keytype = FSECURE;
						}
						else
							throw new JSchException("invaid privatekey: " + identity);

						i += 3;
						continue;
					}
					if (buf[i] == 'C' && buf[i + 1] == 'B' && buf[i + 2] == 'C' && buf[i + 3] == ',')
					{
						i += 4;
						for (int ii = 0; ii < m_iv.Length; ii++)
							m_iv[ii] = (byte)(((a2b(buf[i++]) << 4) & 0xf0) + (a2b(buf[i++]) & 0xf));
						continue;
					}
					if (buf[i] == '\r'
					&& i + 1 < buf.Length && buf[i + 1] == '\n'
						)
					{
						i++;
						continue;
					}
					if (buf[i] == 0x0a && i + 1 < buf.Length)
					{
						if (buf[i + 1] == '\n')
						{
							i += 2;
							break;
						}
						if (buf[i + 1] == '\r'
						&& i + 2 < buf.Length
						&& buf[i + 2] == '\n'
							)
						{
							i += 3; break;
						}
						bool inheader = false;
						for (int j = i + 1; j < buf.Length; j++)
						{
							if (buf[j] == 0x0a) break;
							//if(buf[j]==0x0d) break;
							if (buf[j] == ':') { inheader = true; break; }
						}
						if (!inheader)
						{
							i++;
							m_encrypted = false;    // no passphrase
							break;
						}
					}
					i++;
				}

				if (m_type == ERROR)
					throw new JSchException("invaid privatekey: " + identity);

				int start = i;
				while (i < len)
				{
					if (buf[i] == 0x0a)
					{
						bool xd = (buf[i - 1] == 0x0d);
						Array.Copy(buf, i + 1,
							buf,
							i - (xd ? 1 : 0),
							len - i - 1 - (xd ? 1 : 0)
							);
						if (xd)
							len--;
						len--;
						continue;
					}
					if (buf[i] == '-')
						break;
					i++;
				}
				m_encoded_data = Util.fromBase64(buf, start, i - start);

				if (m_encoded_data.Length > 4 &&            // FSecure
					m_encoded_data[0] == (byte)0x3f &&
					m_encoded_data[1] == (byte)0x6f &&
					m_encoded_data[2] == (byte)0xf9 &&
					m_encoded_data[3] == (byte)0xeb)
				{
					Buffer _buf = new Buffer(m_encoded_data);
					_buf.getInt();  // 0x3f6ff9be
					_buf.getInt();
					byte[] _type = _buf.getString();
					byte[] _cipher = _buf.getString();
					string s_cipher = System.Text.Encoding.Default.GetString(_cipher);
					if (s_cipher.Equals("3des-cbc"))
					{
						_buf.getInt();
						byte[] foo = new byte[m_encoded_data.Length - _buf.OffSet];
						_buf.getByte(foo);
						m_encoded_data = foo;
						m_encrypted = true;
						throw new JSchException("unknown privatekey format: " + identity);
					}
					else if (s_cipher.Equals("none"))
					{
						_buf.getInt();
						m_encrypted = false;
						byte[] foo = new byte[m_encoded_data.Length - _buf.OffSet];
						_buf.getByte(foo);
						m_encoded_data = foo;
					}
				}

				try
				{
					file = new FileInfo(identity + ".pub");
					fis = File.OpenRead(identity + ".pub");
					buf = new byte[(int)(file.Length)];
					len = fis.Read(buf, 0, buf.Length);
					fis.Close();
				}
				catch
				{
					return;
				}

				if (buf.Length > 4 &&             // FSecure's public key
					buf[0] == '-' && buf[1] == '-' && buf[2] == '-' && buf[3] == '-'
					)
				{
					i = 0;
					do
					{
						i++;
					} while (buf.Length > i && buf[i] != 0x0a);
					if (buf.Length <= i)
						return;

					while (true)
					{
						if (buf[i] == 0x0a)
						{
							bool inheader = false;
							for (int j = i + 1; j < buf.Length; j++)
							{
								if (buf[j] == 0x0a)
									break;
								if (buf[j] == ':')
								{
									inheader = true;
									break;
								}
							}
							if (!inheader)
							{
								i++;
								break;
							}
						}
						i++;
					}
					if (buf.Length <= i)
						return;

					start = i;
					while (i < len)
					{
						if (buf[i] == 0x0a)
						{
							Array.Copy(buf, i + 1, buf, i, len - i - 1);
							len--;
							continue;
						}
						if (buf[i] == '-') { break; }
						i++;
					}
					m_publickeyblob = Util.fromBase64(buf, start, i - start);

					if (m_type == UNKNOWN)
					{
						if (m_publickeyblob[8] == 'd')
							m_type = DSS;
						else if (m_publickeyblob[8] == 'r')
							m_type = RSA;
					}
				}
				else
				{
					if (buf[0] != 's' || buf[1] != 's' || buf[2] != 'h' || buf[3] != '-')
						return;
					i = 0;
					while (i < len)
					{
						if (buf[i] == ' ')
							break;
						i++;
					}
					i++;
					if (i >= len)
						return;
					start = i;
					while (i < len)
					{
						if (buf[i] == ' ')
							break;
						i++;
					}
					m_publickeyblob = Util.fromBase64(buf, start, i - start);
				}

			}
			catch (Exception e)
			{
				if (e is JSchException)
					throw (JSchException)e;
				throw new JSchException(e.ToString());
			}
		}

		public string AlgName
		{
			get
			{
				if (m_type == RSA)
					return malgname_RSA;
				return m_algname_DSS;
			}
		}

		public bool setPassphrase(string _passphrase)
		{
			try
			{
				if (m_encrypted)
				{
					if (_passphrase == null)
						return false;
					byte[] passphrase = System.Text.Encoding.Default.GetBytes(_passphrase);
					int hsize = m_hash.BlockSize;
					byte[] hn = new byte[
						m_key.Length / hsize * hsize
						+ (m_key.Length % hsize == 0 ? 0 : hsize)
						];
					byte[] tmp = null;
					if (m_keytype == OPENSSH)
					{
						for (int index = 0; index + hsize <= hn.Length;)
						{
							if (tmp != null)
								m_hash.update(tmp, 0, tmp.Length);
							m_hash.update(passphrase, 0, passphrase.Length);
							m_hash.update(m_iv, 0, m_iv.Length);
							tmp = m_hash.digest();
							Array.Copy(tmp, 0, hn, index, tmp.Length);
							index += tmp.Length;
						}
						Array.Copy(hn, 0, m_key, 0, m_key.Length);
					}
					else if (m_keytype == FSECURE)
					{
						for (int index = 0; index + hsize <= hn.Length;)
						{
							if (tmp != null)
								m_hash.update(tmp, 0, tmp.Length);
							m_hash.update(passphrase, 0, passphrase.Length);
							tmp = m_hash.digest();
							Array.Copy(tmp, 0, hn, index, tmp.Length);
							index += tmp.Length;
						}
						Array.Copy(hn, 0, m_key, 0, m_key.Length);
					}
				}
				if (decrypt())
				{
					m_encrypted = false;
					return true;
				}
				m_P_array = m_Q_array = m_G_array = m_pub_array = m_prv_array = null;
				return false;
			}
			catch (Exception e)
			{
				if (e is JSchException)
					throw (JSchException)e;
				throw new JSchException(e.ToString());
			}
		}

		public byte[] PublicKeyBlob
		{
			get
			{
				if (m_publickeyblob != null)
					return m_publickeyblob;
				if (m_type == RSA)
					return getPublicKeyBlob_rsa();
				return getPublicKeyBlob_dss();
			}
		}

		byte[] getPublicKeyBlob_rsa()
		{
			if (m_e_array == null)
				return null;
			Buffer buf = new Buffer(
				malgname_RSA.Length + 4 +
				m_e_array.Length + 4 +
				m_n_array.Length + 4);
			buf.putString(System.Text.Encoding.Default.GetBytes(malgname_RSA));
			buf.putString(m_e_array);
			buf.putString(m_n_array);
			return buf.m_buffer;
		}

		byte[] getPublicKeyBlob_dss()
		{
			if (m_P_array == null)
				return null;
			Buffer buf = new Buffer(
				m_algname_DSS.Length + 4 +
				m_P_array.Length + 4 +
				m_Q_array.Length + 4 +
				m_G_array.Length + 4 +
				m_pub_array.Length + 4
				);
			buf.putString(System.Text.Encoding.Default.GetBytes(m_algname_DSS));
			buf.putString(m_P_array);
			buf.putString(m_Q_array);
			buf.putString(m_G_array);
			buf.putString(m_pub_array);
			return buf.m_buffer;
		}

		public byte[] getSignature(Session session, byte[] data)
		{
			if (m_type == RSA)
				return getSignature_rsa(session, data);
			return getSignature_dss(session, data);
		}

		byte[] getSignature_rsa(Session session, byte[] data)
		{
			try
			{
				Type t = Type.GetType(m_jsch.getConfig("signature.rsa"));
				SignatureRSA rsa = (SignatureRSA)Activator.CreateInstance(t);

				rsa.init();
				rsa.setPrvKey(m_e_array, m_n_array, m_d_array, m_p_array, m_q_array, m_dmp1_array, m_dmq1_array, m_iqmp_array);
				rsa.update(data);
				byte[] sig = rsa.sign();
				Buffer buf = new Buffer(malgname_RSA.Length + 4 + sig.Length + 4);
				buf.putString(System.Text.Encoding.Default.GetBytes(malgname_RSA));
				buf.putString(sig);
				return buf.m_buffer;
			}
			catch (Exception) { }
			return null;
		}

		byte[] getSignature_dss(Session session, byte[] data)
		{
			try
			{
				Type t = Type.GetType(m_jsch.getConfig("signature.dss"));
				SignatureDSA dsa = (SignatureDSA)(Activator.CreateInstance(t));
				dsa.init();
				dsa.setPrvKey(m_prv_array, m_P_array, m_Q_array, m_G_array);
				dsa.update(data);
				byte[] sig = dsa.sign();
				Buffer buf = new Buffer(m_algname_DSS.Length + 4 + sig.Length + 4);
				buf.putString(System.Text.Encoding.Default.GetBytes(m_algname_DSS));
				buf.putString(sig);
				return buf.m_buffer;
			}
			catch (Exception) { }
			return null;
		}

		public bool decrypt()
		{
			if (m_type == RSA)
				return decrypt_rsa();
			return decrypt_dss();
		}

		bool decrypt_rsa()
		{
			try
			{
				byte[] plain;
				if (m_encrypted)
				{
					if (m_keytype == OPENSSH)
					{
						m_cipher.init(Cipher.DECRYPT_MODE, m_key, m_iv);
						plain = new byte[m_encoded_data.Length];
						m_cipher.update(m_encoded_data, 0, m_encoded_data.Length, plain, 0);
					}
					else if (m_keytype == FSECURE)
					{
						for (int i = 0; i < m_iv.Length; i++)
							m_iv[i] = 0;
						m_cipher.init(Cipher.DECRYPT_MODE, m_key, m_iv);
						plain = new byte[m_encoded_data.Length];
						m_cipher.update(m_encoded_data, 0, m_encoded_data.Length, plain, 0);
					}
					else
						return false;
				}
				else
				{
					if (m_n_array != null)
						return true;
					plain = m_encoded_data;
				}

				if (m_keytype == FSECURE)
				{              // FSecure   
					Buffer buf = new Buffer(plain);
					int foo = buf.getInt();
					if (plain.Length != foo + 4)
						return false;

					m_e_array = buf.getMPIntBits();
					m_d_array = buf.getMPIntBits();
					m_n_array = buf.getMPIntBits();
					byte[] u_array = buf.getMPIntBits();
					m_p_array = buf.getMPIntBits();
					m_q_array = buf.getMPIntBits();
					return true;
				}

				int index = 0;
				int Length = 0;

				if (plain[index] != 0x30)
					return false;
				index++; // SEQUENCE
				Length = plain[index++] & 0xFF;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f;
					Length = 0;
					while (foo-- > 0)
						Length = (Length << 8) + (plain[index++] & 0xff);
				}

				if (plain[index] != 0x02)
					return false;

				index++; // INTEGER
				Length = plain[index++] & 0xFF;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f;
					Length = 0;
					while (foo-- > 0)
						Length = (Length << 8) + (plain[index++] & 0xff);
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
					int foo = Length & 0x7f;
					Length = 0;
					while (foo-- > 0)
						Length = (Length << 8) + (plain[index++] & 0xff);
				}

				m_e_array = new byte[Length];
				Array.Copy(plain, index, m_e_array, 0, Length);
				index += Length;
				index++;
				Length = plain[index++] & 0xff;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f;
					Length = 0;
					while (foo-- > 0)
						Length = (Length << 8) + (plain[index++] & 0xff);
				}

				m_d_array = new byte[Length];
				Array.Copy(plain, index, m_d_array, 0, Length);
				index += Length;
				index++;
				Length = plain[index++] & 0xFF;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f;
					Length = 0;
					while (foo-- > 0)
						Length = (Length << 8) + (plain[index++] & 0xff);
				}

				m_p_array = new byte[Length];
				Array.Copy(plain, index, m_p_array, 0, Length);
				index += Length;
				index++;
				Length = plain[index++] & 0xFF;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f;
					Length = 0;
					while (foo-- > 0)
						Length = (Length << 8) + (plain[index++] & 0xff);
				}
				m_q_array = new byte[Length];
				Array.Copy(plain, index, m_q_array, 0, Length);
				index += Length;
				index++;
				Length = plain[index++] & 0xff;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f;
					Length = 0;
					while (foo-- > 0)
						Length = (Length << 8) + (plain[index++] & 0xff);
				}

				m_dmp1_array = new byte[Length];
				Array.Copy(plain, index, m_dmp1_array, 0, Length);
				index += Length;
				index++;
				Length = plain[index++] & 0xFF;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f;
					Length = 0;
					while (foo-- > 0)
						Length = (Length << 8) + (plain[index++] & 0xff);
				}

				m_dmq1_array = new byte[Length];
				Array.Copy(plain, index, m_dmq1_array, 0, Length);
				index += Length;
				index++;
				Length = plain[index++] & 0xFF;
				if ((Length & 0x80) != 0)
				{
					int foo = Length & 0x7f; Length = 0;
					while (foo-- > 0) { Length = (Length << 8) + (plain[index++] & 0xff); }
				}
				m_iqmp_array = new byte[Length];
				Array.Copy(plain, index, m_iqmp_array, 0, Length);
				index += Length;
			}
			catch
			{
				return false;
			}
			return true;
		}

		bool decrypt_dss()
		{
			try
			{
				byte[] plain;
				if (m_encrypted)
				{
					if (m_keytype == OPENSSH)
					{
						m_cipher.init(Cipher.DECRYPT_MODE, m_key, m_iv);
						plain = new byte[m_encoded_data.Length];
						m_cipher.update(m_encoded_data, 0, m_encoded_data.Length, plain, 0);
					}
					else if (m_keytype == FSECURE)
					{
						for (int i = 0; i < m_iv.Length; i++)
							m_iv[i] = 0;
						m_cipher.init(Cipher.DECRYPT_MODE, m_key, m_iv);
						plain = new byte[m_encoded_data.Length];
						m_cipher.update(m_encoded_data, 0, m_encoded_data.Length, plain, 0);
					}
					else
						return false;
				}
				else
				{
					if (m_P_array != null)
						return true;
					plain = m_encoded_data;
				}

				if (m_keytype == FSECURE)
				{              // FSecure   
					Buffer buf = new Buffer(plain);
					int foo = buf.getInt();
					if (plain.Length != foo + 4)
						return false;

					m_P_array = buf.getMPIntBits();
					m_G_array = buf.getMPIntBits();
					m_Q_array = buf.getMPIntBits();
					m_pub_array = buf.getMPIntBits();
					m_prv_array = buf.getMPIntBits();
					return true;
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

		public bool isEncrypted { get { return m_encrypted; } }
		public string Name { get { return m_identity; } }

		private int writeSEQUENCE(byte[] buf, int index, int len)
		{
			buf[index++] = 0x30;
			index = writeLength(buf, index, len);
			return index;
		}
		private int writeINTEGER(byte[] buf, int index, byte[] data)
		{
			buf[index++] = 0x02;
			index = writeLength(buf, index, data.Length);
			Array.Copy(data, 0, buf, index, data.Length);
			index += data.Length;
			return index;
		}

		private int countLength(int i_len)
		{
			uint len = (uint)i_len;
			int i = 1;
			if (len <= 0x7f)
				return i;
			while (len > 0)
			{
				len >>= 8;
				i++;
			}
			return i;
		}

		private int writeLength(byte[] data, int index, int i_len)
		{
			int len = i_len;
			int i = countLength(len) - 1;
			if (i == 0)
			{
				data[index++] = (byte)len;
				return index;
			}
			data[index++] = (byte)(0x80 | i);
			int j = index + i;
			while (i > 0)
			{
				data[index + i - 1] = (byte)(len & 0xff);
				len >>= 8;
				i--;
			}
			return j;
		}

		private byte a2b(byte c)
		{
			if ('0' <= c && c <= '9')
				return (byte)(c - '0');
			if ('a' <= c && c <= 'z')
				return (byte)(c - 'a' + 10);
			return (byte)(c - 'A' + 10);
		}
		private byte b2a(byte c)
		{
			if (0 <= c && c <= 9)
				return (byte)(c + '0');
			return (byte)(c - 10 + 'A');
		}
	}
}
