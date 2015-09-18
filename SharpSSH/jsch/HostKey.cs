using System;

namespace SharpSsh.jsch
{
	public class HostKey
	{
		public enum HostKeyTypes
		{
			SSHDSS = 0,
			SSHRSA = 1,
			UNKNOWN = 2
		};
		private static byte[] sshdss = System.Text.Encoding.Default.GetBytes("ssh-dss");
		private static byte[] sshrsa = System.Text.Encoding.Default.GetBytes("ssh-rsa");

		internal string m_host;
		internal HostKeyTypes m_type;
		internal byte[] m_key;
		public HostKey(string host, byte[] key)
		{
			m_host = host;
			m_key = key;
			if (key[8] == 'd')
				m_type = HostKeyTypes.SSHDSS;
			else if (key[8] == 'r')
				m_type = HostKeyTypes.SSHRSA;
			else
				throw new JSchException("Invalid key type");
		}
		internal HostKey(string host, HostKeyTypes type, byte[] key)
		{
			m_host = host;
			m_type = type;
			m_key = key;
		}
		public string Host { get { return m_host; } }
		public string getType()
		{
			if (m_type == HostKeyTypes.SSHDSS) { return System.Text.Encoding.Default.GetString(sshdss); }
			if (m_type == HostKeyTypes.SSHRSA) { return System.Text.Encoding.Default.GetString(sshrsa); }
			return "UNKNOWN";
		}
		public string getKey()
		{
			return Convert.ToBase64String(m_key, 0, m_key.Length);
		}
		public string getFingerPrint(JSch jsch)
		{
			HASH hash = null;
			try
			{
				hash = (HASH)Activator.CreateInstance(Type.GetType(jsch.getConfig("md5")));
			}
			catch (Exception e) { Console.Error.WriteLine("getFingerPrint: " + e); }
			return Util.getFingerPrint(hash, m_key);
		}
	}
}
