using System;
using System.IO;
using System.Runtime.CompilerServices;


namespace SharpSsh.jsch
{
	public abstract class KeyPair
	{
		public const int ERROR = 0;
		public const int DSA = 1;
		public const int RSA = 2;
		public const int UNKNOWN = 3;

		internal const int VENDOR_OPENSSH = 0;
		internal const int VENDOR_FSECURE = 1;
		internal int vendor = VENDOR_OPENSSH;

		private static byte[] cr = Util.getBytes("\n");

		public static KeyPair genKeyPair(JSch jsch, int type)
		{
			return genKeyPair(jsch, type, 1024);
		}
		public static KeyPair genKeyPair(JSch jsch, int type, int key_size)
		{
			KeyPair kpair = null;
			if (type == DSA)
				kpair = new KeyPairDSA(jsch);
			else if (type == RSA)
				kpair = new KeyPairRSA(jsch);
			if (kpair != null)
				kpair.generate(key_size);
			return kpair;
		}

		internal abstract void generate(int key_size);

		internal abstract byte[] getBegin();
		internal abstract byte[] getEnd();
		public abstract int getKeySize();

		internal JSch m_jsch = null;
		private Cipher m_cipher;
		private HASH m_hash;
		private Random m_random;

		private byte[] passphrase;
		static byte[][] header = {
									Util.getBytes( "Proc-Type: 4,ENCRYPTED"),
									Util.getBytes("DEK-Info: DES-EDE3-CBC,")
								 };


		public KeyPair(JSch jsch)
		{
			m_jsch = jsch;
		}

		internal abstract byte[] getPrivateKey();

		private void Write(Stream s, byte[] arr)
		{
			s.Write(arr, 0, arr.Length);
		}

		public void writePrivateKey(Stream outs)
		{
			byte[] plain = getPrivateKey();
			byte[][] _iv = new byte[1][];
			byte[] encoded = encrypt(plain, _iv);
			byte[] iv = _iv[0];
			byte[] prv = Util.toBase64(encoded, 0, encoded.Length);

			try
			{
				Write(outs, getBegin()); Write(outs, cr);
				if (passphrase != null)
				{
					Write(outs, header[0]); Write(outs, cr);
					Write(outs, header[1]);
					for (int j = 0; j < iv.Length; j++)
					{
						outs.WriteByte(b2a((byte)((iv[j] >> 4) & 0x0f)));
						outs.WriteByte(b2a((byte)(iv[j] & 0x0f)));
					}
					Write(outs, cr);
					Write(outs, cr);
				}
				int i = 0;
				while (i < prv.Length)
				{
					if (i + 64 < prv.Length)
					{
						outs.Write(prv, i, 64);
						Write(outs, cr);
						i += 64;
						continue;
					}
					outs.Write(prv, i, prv.Length - i);
					Write(outs, cr);
					break;
				}
				Write(outs, getEnd()); Write(outs, cr);
			}
			catch (Exception) { }
		}

		private static byte[] space = Util.getBytes(" ");

		internal abstract byte[] getKeyTypeName();
		public abstract int getKeyType();

		public virtual byte[] getPublicKeyBlob() { return publickeyblob; }

		public void writePublicKey(Stream outs, string comment)
		{
			byte[] pubblob = getPublicKeyBlob();
			byte[] pub = Util.toBase64(pubblob, 0, pubblob.Length);
			try
			{
				Write(outs, getKeyTypeName()); Write(outs, space);
				outs.Write(pub, 0, pub.Length); Write(outs, space);
				Write(outs, Util.getBytes(comment));
				Write(outs, cr);
			}
			catch (Exception) { }
		}

		public void writePublicKey(string name, string comment)
		{
			FileStream fos = new FileStream(name, FileMode.OpenOrCreate);
			writePublicKey(fos, comment);
			fos.Close();
		}

		public void writeSECSHPublicKey(Stream outs, string comment)
		{
			byte[] pubblob = getPublicKeyBlob();
			byte[] pub = Util.toBase64(pubblob, 0, pubblob.Length);
			try
			{
				Write(outs, Util.getBytes("---- BEGIN SSH2 PUBLIC KEY ----")); Write(outs, cr);
				Write(outs, Util.getBytes("Comment: \"" + comment + "\"")); Write(outs, cr);
				int index = 0;
				while (index < pub.Length)
				{
					int len = 70;
					if ((pub.Length - index) < len) len = pub.Length - index;
					outs.Write(pub, index, len); Write(outs, cr);
					index += len;
				}
				Write(outs, Util.getBytes("---- END SSH2 PUBLIC KEY ----")); Write(outs, cr);
			}
			catch (Exception) { }
		}

		public void writeSECSHPublicKey(string name, string comment)
		{
			FileStream fos = new FileStream(name, FileMode.OpenOrCreate);
			writeSECSHPublicKey(fos, comment);
			fos.Close();
		}


		public void writePrivateKey(string name)
		{
			FileStream fos = new FileStream(name, FileMode.OpenOrCreate);
			writePrivateKey(fos);
			fos.Close();
		}

		public string getFingerPrint()
		{
			if (m_hash == null) m_hash = genHash();
			byte[] kblob = getPublicKeyBlob();
			if (kblob == null) return null;
			return getKeySize() + " " + Util.getFingerPrint(m_hash, kblob);
		}

		private byte[] encrypt(byte[] plain, byte[][] _iv)
		{
			if (passphrase == null) return plain;

			if (m_cipher == null) m_cipher = genCipher();
			byte[] iv = _iv[0] = new byte[m_cipher.IVSize];

			if (m_random == null) m_random = genRandom();
			m_random.fill(iv, 0, iv.Length);

			byte[] key = genKey(passphrase, iv);
			byte[] encoded = plain;
			int bsize = m_cipher.BlockSize;
			if (encoded.Length % bsize != 0)
			{
				byte[] foo = new byte[(encoded.Length / bsize + 1) * bsize];
				Array.Copy(encoded, 0, foo, 0, encoded.Length);
				encoded = foo;
			}

			try
			{
				m_cipher.init(Cipher.ENCRYPT_MODE, key, iv);
				m_cipher.update(encoded, 0, encoded.Length, encoded, 0);
			}
			catch (Exception) { }
			return encoded;
		}

		internal abstract bool parse(byte[] data);

		private byte[] decrypt(byte[] data, byte[] passphrase, byte[] iv)
		{
			/*
			if(iv==null){  // FSecure
			  iv=new byte[8];
			  for(int i=0; i<iv.Length; i++)iv[i]=0;
			}
			*/
			try
			{
				byte[] key = genKey(passphrase, iv);
				m_cipher.init(Cipher.DECRYPT_MODE, key, iv);
				byte[] plain = new byte[data.Length];
				m_cipher.update(data, 0, data.Length, plain, 0);
				return plain;
			}
			catch (Exception) { }
			return null;
		}

		internal int writeSEQUENCE(byte[] buf, int index, int len)
		{
			buf[index++] = 0x30;
			index = writeLength(buf, index, len);
			return index;
		}
		internal int writeINTEGER(byte[] buf, int index, byte[] data)
		{
			buf[index++] = 0x02;
			index = writeLength(buf, index, data.Length);
			Array.Copy(data, 0, buf, index, data.Length);
			index += data.Length;
			return index;
		}

		internal int countLength(int len)
		{
			int i = 1;
			if (len <= 0x7f) return i;
			while (len > 0)
			{
				len >>= 8;
				i++;
			}
			return i;
		}

		internal int writeLength(byte[] data, int index, int len)
		{
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

		private Random genRandom()
		{
			if (m_random == null)
			{
				try
				{
					Type t = Type.GetType(m_jsch.getConfig("random"));
					m_random = (Random)Activator.CreateInstance(t);
				}
				catch (Exception) { }
			}
			return m_random;
		}

		private HASH genHash()
		{
			try
			{
				Type t = Type.GetType(m_jsch.getConfig("md5"));
				m_hash = (HASH)Activator.CreateInstance(t);
				m_hash.init();
			}
			catch//(Exception e)
			{
			}
			return m_hash;
		}
		private Cipher genCipher()
		{
			try
			{
				Type t;
				t = Type.GetType(m_jsch.getConfig("3des-cbc"));
				m_cipher = (Cipher)(Activator.CreateInstance(t));
			}
			catch//(Exception e)
			{
			}
			return m_cipher;
		}

		/*
		  hash is MD5
		  h(0) <- hash(passphrase, iv);
		  h(n) <- hash(h(n-1), passphrase, iv);
		  key <- (h(0),...,h(n))[0,..,key.Length];
		*/
		[MethodImpl(MethodImplOptions.Synchronized)]
		internal byte[] genKey(byte[] passphrase, byte[] iv)
		{
			if (m_cipher == null) m_cipher = genCipher();
			if (m_hash == null) m_hash = genHash();

			byte[] key = new byte[m_cipher.BlockSize];
			int hsize = m_hash.BlockSize;
			byte[] hn = new byte[key.Length / hsize * hsize +
				(key.Length % hsize == 0 ? 0 : hsize)];
			try
			{
				byte[] tmp = null;
				if (vendor == VENDOR_OPENSSH)
				{
					for (int index = 0; index + hsize <= hn.Length;)
					{
						if (tmp != null) { m_hash.update(tmp, 0, tmp.Length); }
						m_hash.update(passphrase, 0, passphrase.Length);
						m_hash.update(iv, 0, iv.Length);
						tmp = m_hash.digest();
						Array.Copy(tmp, 0, hn, index, tmp.Length);
						index += tmp.Length;
					}
					Array.Copy(hn, 0, key, 0, key.Length);
				}
				else if (vendor == VENDOR_FSECURE)
				{
					for (int index = 0; index + hsize <= hn.Length;)
					{
						if (tmp != null) { m_hash.update(tmp, 0, tmp.Length); }
						m_hash.update(passphrase, 0, passphrase.Length);
						tmp = m_hash.digest();
						Array.Copy(tmp, 0, hn, index, tmp.Length);
						index += tmp.Length;
					}
					Array.Copy(hn, 0, key, 0, key.Length);
				}
			}
			catch (Exception) { }
			return key;
		}

		public void setPassphrase(string passphrase)
		{
			if (passphrase == null || passphrase.Length == 0)
			{
				setPassphrase((byte[])null);
			}
			else
			{
				setPassphrase(Util.getBytes(passphrase));
			}
		}
		public void setPassphrase(byte[] passphrase)
		{
			if (passphrase != null && passphrase.Length == 0)
				passphrase = null;
			this.passphrase = passphrase;
		}

		private bool encrypted = false;
		private byte[] data = null;
		private byte[] iv = null;
		private byte[] publickeyblob = null;

		public bool isEncrypted() { return encrypted; }
		public bool decrypt(string _passphrase)
		{
			byte[] passphrase = Util.getBytes(_passphrase);
			byte[] foo = decrypt(data, passphrase, iv);
			if (parse(foo))
			{
				encrypted = false;
			}
			return !encrypted;
		}

		public static KeyPair load(JSch jsch, string prvkey)
		{
			string pubkey = prvkey + ".pub";
			//			if(!new File(pubkey).exists())
			if (!File.Exists(pubkey))
			{
				pubkey = null;
			}
			return load(jsch, prvkey, pubkey);
		}
		public static KeyPair load(JSch jsch, string prvkey, string pubkey)
		{

			byte[] iv = new byte[8];       // 8
			bool encrypted = true;
			byte[] data = null;

			byte[] publickeyblob = null;

			int type = ERROR;
			int vendor = VENDOR_OPENSSH;

			try
			{
				//File file=new File(prvkey);
				FileStream fis = File.OpenRead(prvkey);
				byte[] buf = new byte[(int)(fis.Length)];
				int len = fis.Read(buf, 0, buf.Length);
				fis.Close();

				int i = 0;

				while (i < len)
				{
					if (buf[i] == 'B' && buf[i + 1] == 'E' && buf[i + 2] == 'G' && buf[i + 3] == 'I')
					{
						i += 6;
						if (buf[i] == 'D' && buf[i + 1] == 'S' && buf[i + 2] == 'A') { type = DSA; }
						else if (buf[i] == 'R' && buf[i + 1] == 'S' && buf[i + 2] == 'A') { type = RSA; }
						else if (buf[i] == 'S' && buf[i + 1] == 'S' && buf[i + 2] == 'H')
						{ // FSecure
							type = UNKNOWN;
							vendor = VENDOR_FSECURE;
						}
						else
						{
							//System.outs.println("invalid format: "+identity);
							throw new JSchException("invaid privatekey: " + prvkey);
						}
						i += 3;
						continue;
					}
					if (buf[i] == 'C' && buf[i + 1] == 'B' && buf[i + 2] == 'C' && buf[i + 3] == ',')
					{
						i += 4;
						for (int ii = 0; ii < iv.Length; ii++)
						{
							iv[ii] = (byte)(((a2b(buf[i++]) << 4) & 0xf0) + (a2b(buf[i++]) & 0xf));
						}
						continue;
					}
					if (buf[i] == 0x0d &&
						i + 1 < buf.Length && buf[i + 1] == 0x0a)
					{
						i++;
						continue;
					}
					if (buf[i] == 0x0a && i + 1 < buf.Length)
					{
						if (buf[i + 1] == 0x0a) { i += 2; break; }
						if (buf[i + 1] == 0x0d &&
							i + 2 < buf.Length && buf[i + 2] == 0x0a)
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
							encrypted = false;    // no passphrase
							break;
						}
					}
					i++;
				}

				if (type == ERROR)
				{
					throw new JSchException("invaid privatekey: " + prvkey);
				}

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
						if (xd) len--;
						len--;
						continue;
					}
					if (buf[i] == '-') { break; }
					i++;
				}
				data = Util.fromBase64(buf, start, i - start);

				if (data.Length > 4 &&            // FSecure
					data[0] == (byte)0x3f &&
					data[1] == (byte)0x6f &&
					data[2] == (byte)0xf9 &&
					data[3] == (byte)0xeb)
				{

					Buffer _buf = new Buffer(data);
					_buf.getInt();  // 0x3f6ff9be
					_buf.getInt();
					byte[] _type = _buf.getString();
					byte[] _cipher = _buf.getString();
					string cipher = Util.getString(_cipher);
					if (cipher.Equals("3des-cbc"))
					{
						_buf.getInt();
						byte[] foo = new byte[data.Length - _buf.OffSet];
						_buf.getByte(foo);
						data = foo;
						encrypted = true;
						throw new JSchException("unknown privatekey format: " + prvkey);
					}
					else if (cipher.Equals("none"))
					{
						_buf.getInt();
						_buf.getInt();

						encrypted = false;

						byte[] foo = new byte[data.Length - _buf.OffSet];
						_buf.getByte(foo);
						data = foo;
					}
				}

				if (pubkey != null)
				{
					try
					{
						fis = File.OpenRead(pubkey);
						buf = new byte[(int)(fis.Length)];
						len = fis.Read(buf, 0, buf.Length);
						fis.Close();

						if (buf.Length > 4 &&             // FSecure's public key
							buf[0] == '-' && buf[1] == '-' && buf[2] == '-' && buf[3] == '-')
						{

							bool valid = true;
							i = 0;
							do { i++; } while (buf.Length > i && buf[i] != 0x0a);
							if (buf.Length <= i) { valid = false; }

							while (valid)
							{
								if (buf[i] == 0x0a)
								{
									bool inheader = false;
									for (int j = i + 1; j < buf.Length; j++)
									{
										if (buf[j] == 0x0a) break;
										if (buf[j] == ':') { inheader = true; break; }
									}
									if (!inheader)
									{
										i++;
										break;
									}
								}
								i++;
							}
							if (buf.Length <= i) { valid = false; }

							start = i;
							while (valid && i < len)
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
							if (valid)
							{
								publickeyblob = Util.fromBase64(buf, start, i - start);
								if (type == UNKNOWN)
								{
									if (publickeyblob[8] == 'd') { type = DSA; }
									else if (publickeyblob[8] == 'r') { type = RSA; }
								}
							}
						}
						else
						{
							if (buf[0] == 's' && buf[1] == 's' && buf[2] == 'h' && buf[3] == '-')
							{
								i = 0;
								while (i < len) { if (buf[i] == ' ')break; i++; } i++;
								if (i < len)
								{
									start = i;
									while (i < len) { if (buf[i] == ' ')break; i++; }
									publickeyblob = Util.fromBase64(buf, start, i - start);
								}
							}
						}
					}
					catch
					{ }
				}
			}
			catch (Exception e)
			{
				if (e is JSchException) throw (JSchException)e;
				throw new JSchException(e.ToString());
			}

			KeyPair kpair = null;
			if (type == DSA) { kpair = new KeyPairDSA(jsch); }
			else if (type == RSA) { kpair = new KeyPairRSA(jsch); }

			if (kpair != null)
			{
				kpair.encrypted = encrypted;
				kpair.publickeyblob = publickeyblob;
				kpair.vendor = vendor;

				if (encrypted)
				{
					kpair.iv = iv;
					kpair.data = data;
				}
				else
				{
					if (kpair.parse(data))
					{
						return kpair;
					}
					else
					{
						throw new JSchException("invaid privatekey: " + prvkey);
					}
				}
			}

			return kpair;
		}

		static private byte a2b(byte c)
		{
			if ('0' <= c && c <= '9')
				return (byte)(c - '0');
			return (byte)(c - 'a' + 10);
		}
		static private byte b2a(byte c)
		{
			if (0 <= c && c <= 9) return (byte)(c + '0');
			return (byte)(c - 10 + 'A');
		}

		public virtual void dispose()
		{
			passphrase = null;
		}
	}
}