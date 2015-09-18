using System;
using Text = System.Text;
using Str = System.String;

namespace SharpSsh.java
{
	/// <summary>
	/// Summary description for String.
	/// </summary>
	public class StringEx
	{
		string m_s;

		public StringEx(string s)
		{
			m_s = s;
		}

		public StringEx(object o)
			: this(o.ToString())
		{
		}

		public StringEx(byte[] arr)
			: this(getString(arr))
		{
		}

		public StringEx(byte[] arr, int offset, int len)
			: this(getString(arr, offset, len))
		{
		}

		public static implicit operator StringEx(string str)
		{
			if (str == null) return null;
			return new StringEx(str);
		}

		public static implicit operator Str(StringEx str)
		{
			if (str == null) return null;
			return str.ToString();
		}

		public static SharpSsh.java.StringEx operator +(SharpSsh.java.StringEx str1, SharpSsh.java.StringEx str2)
		{
			return new SharpSsh.java.StringEx(str1.ToString() + str2.ToString());
		}

		public byte[] getBytes()
		{
			return StringEx.getBytes(this);
		}

		public override string ToString()
		{
			return m_s;
		}

		public StringEx toLowerCase()
		{
			return this.ToString().ToLower();
		}

		public bool startsWith(string prefix)
		{
			return this.ToString().StartsWith(prefix);
		}

		public int indexOf(string sub)
		{
			return this.ToString().IndexOf(sub);
		}

		public int indexOf(char sub)
		{
			return this.ToString().IndexOf(sub);
		}

		public int indexOf(char sub, int i)
		{
			return this.ToString().IndexOf(sub, i);
		}

		public char charAt(int i)
		{
			return m_s[i];
		}

		public StringEx substring(int start, int end)
		{
			int len = end - start;
			return this.ToString().Substring(start, len);
		}

		public StringEx subString(int start, int len)
		{
			return substring(start, len);
		}

		public StringEx substring(int len)
		{
			return this.ToString().Substring(len);
		}

		public StringEx subString(int len)
		{
			return substring(len);
		}

		public int Length()
		{
			return this.ToString().Length;
		}

		public int length()
		{
			return Length();
		}

		public bool endsWith(string str)
		{
			return m_s.EndsWith(str);
		}

		public int lastIndexOf(string str)
		{
			return m_s.LastIndexOf(str);
		}

		public int lastIndexOf(char c)
		{
			return m_s.LastIndexOf(c);
		}

		public bool equals(object o)
		{
			return this.ToString().Equals(o.ToString());
		}

		public override bool Equals(object obj)
		{
			return this.equals(obj);
		}

		public override int GetHashCode()
		{
			return m_s.GetHashCode();
		}

		public static string getString(byte[] arr)
		{
			return getString(arr, 0, arr.Length);
		}

		public static string getString(byte[] arr, int offset, int len)
		{
			return Text.Encoding.Default.GetString(arr, offset, len);
		}

		public static string getStringUTF8(byte[] arr)
		{
			return getStringUTF8(arr, 0, arr.Length);
		}

		public static string getStringUTF8(byte[] arr, int offset, int len)
		{
			return Text.Encoding.UTF8.GetString(arr, offset, len);
		}

		public static byte[] getBytes(string str)
		{
			return getBytesUTF8(str);
		}
		public static byte[] getBytesUTF8(string str)
		{
			return Text.Encoding.UTF8.GetBytes(str);
		}
	}
}
