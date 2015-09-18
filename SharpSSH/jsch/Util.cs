using SharpSsh.java;
using System;
using System.Threading;
using System.Text;

namespace SharpSsh.jsch
{
	public class Util
	{
		private static string[] m_chars = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };

		/// <summary>
		/// Converts a time_t to DateTime
		/// </summary>
		public static DateTime Time_T2DateTime(uint time_t)
		{
			long win32FileTime = 10000000 * (long)time_t + 116444736000000000;
			return DateTime.FromFileTimeUtc(win32FileTime).ToLocalTime();
		}

		private static byte[] b64 = Encoding.Default.GetBytes("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=");
		private static byte val(byte foo)
		{
			if (foo == '=')
				return 0;
			for (int j = 0; j < b64.Length; j++)
				if (foo == b64[j]) return (byte)j;
			return 0;
		}

		internal static byte[] fromBase64(byte[] buf, int start, int length)
		{
			byte[] foo = new byte[length];
			int j = 0;
			int l = length;
			for (int i = start; i < start + length; i += 4)
			{
				foo[j] = (byte)((val(buf[i]) << 2) | ((val(buf[i + 1]) & 0x30) >> 4));
				if (buf[i + 2] == (byte)'=')
				{
					j++;
					break;
				}
				foo[j + 1] = (byte)(((val(buf[i + 1]) & 0x0f) << 4) | ((val(buf[i + 2]) & 0x3c) >> 2));
				if (buf[i + 3] == (byte)'=')
				{
					j += 2;
					break;
				}
				foo[j + 2] = (byte)(((val(buf[i + 2]) & 0x03) << 6) | (val(buf[i + 3]) & 0x3f));
				j += 3;
			}
			byte[] bar = new byte[j];
			Array.Copy(foo, 0, bar, 0, j);
			return bar;
		}

		internal static byte[] toBase64(string msg)
		{
			return toBase64(Util.getBytesUTF8(msg));
		}

		internal static byte[] toBase64(byte[] buf)
		{
			return toBase64(buf, 0, buf.Length);
		}

		internal static byte[] toBase64(byte[] buf, int start, int length)
		{

			byte[] tmp = new byte[length * 2];
			int i, j, k;

			int foo = (length / 3) * 3 + start;
			i = 0;
			for (j = start; j < foo; j += 3)
			{
				k = (buf[j] >> 2) & 0x3f;
				tmp[i++] = b64[k];
				k = (buf[j] & 0x03) << 4 | (buf[j + 1] >> 4) & 0x0f;
				tmp[i++] = b64[k];
				k = (buf[j + 1] & 0x0f) << 2 | (buf[j + 2] >> 6) & 0x03;
				tmp[i++] = b64[k];
				k = buf[j + 2] & 0x3f;
				tmp[i++] = b64[k];
			}

			foo = (start + length) - foo;
			if (foo == 1)
			{
				k = (buf[j] >> 2) & 0x3f;
				tmp[i++] = b64[k];
				k = ((buf[j] & 0x03) << 4) & 0x3f;
				tmp[i++] = b64[k];
				tmp[i++] = (byte)'=';
				tmp[i++] = (byte)'=';
			}
			else if (foo == 2)
			{
				k = (buf[j] >> 2) & 0x3f;
				tmp[i++] = b64[k];
				k = (buf[j] & 0x03) << 4 | (buf[j + 1] >> 4) & 0x0f;
				tmp[i++] = b64[k];
				k = ((buf[j + 1] & 0x0f) << 2) & 0x3f;
				tmp[i++] = b64[k];
				tmp[i++] = (byte)'=';
			}
			byte[] bar = new byte[i];
			Array.Copy(tmp, 0, bar, 0, i);
			return bar;
		}

		internal static string[] split(string foo, string split)
		{
			byte[] buf = Util.getBytes(foo);
			System.Collections.ArrayList bar = new System.Collections.ArrayList();
			int start = 0;
			int index;
			while (true)
			{
				index = foo.IndexOf(split, start);
				if (index >= 0)
				{
					bar.Add(Util.getString(buf, start, index - start));
					start = index + 1;
					continue;
				}
				bar.Add(Util.getString(buf, start, buf.Length - start));
				break;
			}
			string[] result = new String[bar.Count];
			for (int i = 0; i < result.Length; i++)
				result[i] = (string)(bar[i]);
			return result;
		}

		internal static bool glob(byte[] pattern, byte[] name)
		{
			return glob(pattern, 0, name, 0);
		}

		private static bool glob(byte[] pattern, int pattern_index, byte[] name, int name_index)
		{
			int patternlen = pattern.Length;
			if (patternlen == 0)
				return false;
			int namelen = name.Length;
			int i = pattern_index;
			int j = name_index;
			while (i < patternlen && j < namelen)
			{
				if (pattern[i] == '\\')
				{
					if (i + 1 == patternlen)
						return false;
					i++;
					if (pattern[i] != name[j])
						return false;
					i++; j++;
					continue;
				}
				if (pattern[i] == '*')
				{
					if (patternlen == i + 1)
						return true;
					i++;
					byte foo = pattern[i];
					while (j < namelen)
						if (foo == name[j] && glob(pattern, i, name, j))
							return true;
					j++;
					return false;
				}
				if (pattern[i] == '?')
				{
					i++; j++;
					continue;
				}
				if (pattern[i] != name[j])
					return false;
				i++;
				j++;
				continue;
			}
			if (i == patternlen && j == namelen)
				return true;
			return false;
		}

		internal static string getFingerPrint(HASH hash, byte[] data)
		{
			try
			{
				hash.init();
				hash.update(data, 0, data.Length);
				byte[] foo = hash.digest();
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				uint bar;
				for (int i = 0; i < foo.Length; i++)
				{
					bar = (byte)(foo[i] & 0xff);
					sb.Append(m_chars[(bar >> 4) & 0xf]);
					sb.Append(m_chars[(bar) & 0xf]);
					if (i + 1 < foo.Length)
						sb.Append(":");
				}
				return sb.ToString();
			}
			catch
			{
				return "???";
			}
		}

		internal static SharpSsh.java.net.Socket createSocket(string host, int port, int timeout)
		{
			SharpSsh.java.net.Socket socket = null;
			string message = "";
			if (timeout == 0)
			{
				try
				{
					socket = new SharpSsh.java.net.Socket(host, port);
					return socket;
				}
				catch (Exception e)
				{
					message = e.ToString();
					throw new JSchException(message);
				}
			}

			SharpSsh.java.net.Socket[] sockp = new SharpSsh.java.net.Socket[1];
			Thread currentThread = Thread.CurrentThread;
			Exception[] ee = new Exception[1];
			message = "";
			createSocketRun runnable = new createSocketRun(sockp, ee, host, port);
			Thread thread = new Thread(new ThreadStart(runnable.run));
			thread.Name = "Opening Socket " + host;
			thread.Start();
			try
			{
				thread.Join(timeout);
				message = "timeout: ";
			}
			catch (ThreadInterruptedException)
			{ }

			if (sockp[0] != null && sockp[0].isConnected())
				socket = sockp[0];
			else
			{
				message += "socket is not established";
				if (ee[0] != null)
					message = ee[0].ToString();

				thread.Interrupt();
				thread = null;
				throw new JSchException(message);
			}
			return socket;
		}

		private class createSocketRun
		{
			SharpSsh.java.net.Socket[] m_sockp;
			Exception[] m_ee;
			string m_host;
			int m_port;

			public createSocketRun(SharpSsh.java.net.Socket[] sockp, Exception[] ee, string host, int port)
			{
				m_sockp = sockp;
				m_ee = ee;
				m_host = host;
				m_port = port;
			}

			public void run()
			{
				m_sockp[0] = null;
				try
				{
					m_sockp[0] = new SharpSsh.java.net.Socket(m_host, m_port);
				}
				catch (Exception e)
				{
					m_ee[0] = e;
					if (m_sockp[0] != null && m_sockp[0].isConnected())
					{
						try
						{
							m_sockp[0].Close();
						}
						catch (Exception)
						{ }
					}
					m_sockp[0] = null;
				}
			}
		}

		internal static bool array_equals(byte[] foo, byte[] bar)
		{
			int i = foo.Length;
			if (i != bar.Length)
				return false;
			for (int j = 0; j < i; j++)
			{
				if (foo[j] != bar[j])
					return false;
			}
			return true;
		}

		public static string getString(byte[] arr, int offset, int len)
		{
			return Encoding.Default.GetString(arr, offset, len);
		}
		public static string getStringUTF8(byte[] arr, int offset, int len)
		{
			return Encoding.UTF8.GetString(arr, offset, len);
		}

		public static string getString(byte[] arr)
		{
			return getString(arr, 0, arr.Length);
		}
		public static string getStringUTF8(byte[] arr)
		{
			return getStringUTF8(arr, 0, arr.Length);
		}

		public static byte[] getBytes(string str)
		{
			return Encoding.Default.GetBytes(str);
		}

		public static byte[] getBytesUTF8(string str)
		{
			return Encoding.UTF8.GetBytes(str);
		}

		public static bool RegionMatches(string orig, bool ignoreCase, int toffset, string other, int ooffset, int len)
		{
			char[] ta = new char[orig.Length];
			char[] pa = new char[other.Length];
			orig.CopyTo(0, ta, 0, orig.Length);
			int to = toffset;
			other.CopyTo(0, pa, 0, other.Length);
			int po = ooffset;

			// Note: toffset, ooffset, or len might be near -1>>>1.
			if ((ooffset < 0)
			|| (toffset < 0)
			|| (toffset > (long)orig.Length - len)
			|| (ooffset > (long)other.Length - len)
				)
				return false;

			while (len-- > 0)
			{
				char c1 = ta[to++];
				char c2 = pa[po++];
				if (c1 == c2)
					continue;

				if (ignoreCase)
				{
					// If characters don't match but case may be ignored,
					// try converting both characters to uppercase.
					// If the results match, then the comparison scan should
					// continue.
					char u1 = char.ToUpper(c1);
					char u2 = char.ToUpper(c2);
					if (u1 == u2)
						continue;

					// Unfortunately, conversion to uppercase does not work properly
					// for the Georgian alphabet, which has strange rules about case
					// conversion.  So we need to make one last check before
					// exiting.
					if (char.ToLower(u1) == char.ToLower(u2))
						continue;
				}
				return false;
			}
			return true;
		}

		public static uint ToUInt32(byte[] ptr, int Index)
		{
			uint ui = 0;

			ui = ((uint)ptr[Index++]) << 24;
			ui += ((uint)ptr[Index++]) << 16;
			ui += ((uint)ptr[Index++]) << 8;
			ui += (uint)ptr[Index++];

			return ui;
		}

		public static int ToInt32(byte[] ptr, int Index)
		{
			return (int)ToUInt32(ptr, Index);
		}

		public static ushort ToUInt16(byte[] ptr, int Index)
		{
			ushort u = 0;

			u = (ushort)ptr[Index++];
			u *= 256;
			u += (ushort)ptr[Index++];

			return u;
		}
		public static bool ArrayContains(Object[] arr, Object o)
		{
			for (int i = 0; i < arr.Length; i++)
				if (arr[i].Equals(o))
					return true;
			return false;
		}
		public static bool ArrayContains(char[] arr, char c)
		{
			for (int i = 0; i < arr.Length; i++)
				if (arr[i] == c)
					return true;
			return false;
		}
		public static bool ArrayContains(char[] arr, char c, int count)
		{
			for (int i = 0; i < count; i++)
				if (arr[i] == c)
					return true;
			return false;
		}

		/**
		* Utility method to delete the leading zeros from the modulus.
		* @param a modulus
		* @return modulus
		*/
		public static byte[] StripLeadingZeros(byte[] a)
		{
			int lastZero = -1;
			for (int i = 0; i < a.Length; i++)
				if (a[i] == 0)
					lastZero = i;
				else
					break;

			lastZero++;
			byte[] result = new byte[a.Length - lastZero];
			Array.Copy(a, lastZero, result, 0, result.Length);
			return result;
		}

		/*
		Based on: http://www.orlingrabbe.com/dsa_java.htm
		And on: http://groups.google.com/group/comp.lang.java.security/browse_thread/thread/8f3bb93f9348434f/a1681ec252084483?lnk=st&q=ASN.1+signature+dsa+format&rnum=5&hl=en#a1681ec252084483
		If you're asking about the SHA/DSA signature format from 
		java.security.Signature, it looks like this, for example: 

		% od -tx1 sig 
		0000000 30 2c 02 14 4f 01 01 92 24 7c b3 c7 af 76 71 92 
		0000020 9c db c7 fe 1f 4e 42 4f 02 14 78 f3 63 d5 a3 0e 
		0000040 be 7d 92 7c 43 8d 0f 5f 02 df ee 35 9e b2 


		It's DER format, with 0x30 meaning SEQUENCE, 0x2c the length of the 
		sequence, 0x02 meaning INTEGER, and 0x14 the length of the integer. 
		The first integer (as a byte array) is r, and the second one is s. 

		*/
		public static byte[] FixDsaSig(byte[] sig)
		{
			byte[] newSig = new byte[40];
			Array.Copy(sig, 4, newSig, 0, 20);
			int index;
			if (sig.Length == 46)
				index = 26;
			else if (sig.Length == 47)
				index = 27;
			else
				throw new Exception("Can't fix DSA Signature.");
			Array.Copy(sig, index, newSig, 20, 20);
			return newSig;
		}

		public static void Print(string name, byte[] data)
		{
			Console.WriteLine();
			Console.Write(name + ": ");
			Console.WriteLine(Hex(data));
			Console.WriteLine();
		}

		public static string Hex(byte[] arr)
		{
			string hex = "0x";
			for (int i = 0; i < arr.Length; i++)
			{
				string mbyte = arr[i].ToString("X");
				if (mbyte.Length == 1)
					mbyte = "0" + mbyte;
				hex += mbyte;
			}
			return hex;
		}

		public static void Dump(string fileName, byte[] bytes)
		{
			System.IO.FileStream s = new System.IO.FileStream(fileName, System.IO.FileMode.OpenOrCreate);
			s.Write(bytes, 0, bytes.Length);
			s.Flush();
			s.Close();
		}

		internal static string Unquote(string path)
		{
			int path_len = path.Length;
			int idx = 0;
			while (idx < path_len)
			{
				if (path[idx] == '\\')
				{
					if (idx + 1 == path_len)
						break;
					if (idx > 0)
						path = path.Substring(0, idx) + path.Substring(idx + 1);
					else
						path = path.Substring(idx + 1);
					path_len--;
				}
				else
					idx++;
			}
			return path;
		}
	}
}
