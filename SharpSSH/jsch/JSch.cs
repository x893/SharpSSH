using System;
using System.IO;
using System.Collections.Generic;
using SharpSsh.java.util;
using System.Collections;

namespace SharpSsh.jsch
{
	public class JSch
	{
		private static StringDictionary m_config;
		private ArrayList m_pool = new ArrayList();
		private HostKeyRepository m_known_hosts = null;
		private List<Identity> m_identities = new List<Identity>();
		private System.Collections.ArrayList m_proxies;

		internal List<Identity> Identities { get { return m_identities; } }

        public static void Init()
		{
			m_config = new StringDictionary(30);

			//  config.Add("kex", "diffie-hellman-group-exchange-sha1");
			m_config.Add("kex", "diffie-hellman-group1-sha1,diffie-hellman-group-exchange-sha1");
			m_config.Add("server_host_key", "ssh-rsa,ssh-dss");
			//	config.Add("server_host_key", "ssh-dss,ssh-rsa");
			//	config.Add("cipher.s2c", "3des-cbc,blowfish-cbc");
			//	config.Add("cipher.c2s", "3des-cbc,blowfish-cbc");

			m_config.Add("cipher.s2c", "3des-cbc,aes128-cbc");
			m_config.Add("cipher.c2s", "3des-cbc,aes128-cbc");

			//	config.Add("mac.s2c", "hmac-md5,hmac-sha1,hmac-sha1-96,hmac-md5-96");
			//	config.Add("mac.c2s", "hmac-md5,hmac-sha1,hmac-sha1-96,hmac-md5-96");
			m_config.Add("mac.s2c", "hmac-md5,hmac-sha1");
			m_config.Add("mac.c2s", "hmac-md5,hmac-sha1");
			m_config.Add("compression.s2c", "none");
			m_config.Add("compression.c2s", "none");
			m_config.Add("lang.s2c", "");
			m_config.Add("lang.c2s", "");

			m_config.Add("diffie-hellman-group-exchange-sha1", "SharpSsh.jsch.DHGEX");
			m_config.Add("diffie-hellman-group1-sha1", "SharpSsh.jsch.DHG1");

			m_config.Add("dh", "SharpSsh.jsch.jce.DH");
			m_config.Add("3des-cbc", "SharpSsh.jsch.jce.TripleDESCBC");
			//config.Add("blowfish-cbc",  "SharpSsh.jsch.jce.BlowfishCBC");
			m_config.Add("hmac-sha1", "SharpSsh.jsch.jce.HMACSHA1");
			m_config.Add("hmac-sha1-96", "SharpSsh.jsch.jce.HMACSHA196");
			m_config.Add("hmac-md5", "SharpSsh.jsch.jce.HMACMD5");
			m_config.Add("hmac-md5-96", "SharpSsh.jsch.jce.HMACMD596");
			m_config.Add("sha-1", "SharpSsh.jsch.jce.SHA1");
			m_config.Add("md5", "SharpSsh.jsch.jce.MD5");
			m_config.Add("signature.dss", "SharpSsh.jsch.jce.SignatureDSA");
			m_config.Add("signature.rsa", "SharpSsh.jsch.jce.SignatureRSA");
			m_config.Add("keypairgen.dsa", "SharpSsh.jsch.jce.KeyPairGenDSA");
			m_config.Add("keypairgen.rsa", "SharpSsh.jsch.jce.KeyPairGenRSA");
			m_config.Add("random", "SharpSsh.jsch.jce.Random");

			m_config.Add("aes128-cbc", "SharpSsh.jsch.jce.AES128CBC");

			//config.Add("zlib", "com.jcraft.jsch.jcraft.Compression");

			m_config.Add("StrictHostKeyChecking", "ask");
		}

		public JSch()
		{
			if (m_config == null)
				Init();
		}

		public Session getSession(string username, string host)
		{
			return getSession(username, host, SharpSsh.SshBase.SSH_TCP_PORT);
		}

		public Session getSession(string username, string host, int port)
		{
			Session s = new Session(this);
			s.setUserName(username);
			s.setHost(host);
			s.setPort(port);
			m_pool.Add(s);
			return s;
		}

		internal bool removeSession(Session session)
		{
			lock (m_pool)
			{	//!!!
				m_pool.Remove(session);
				return true;
			}
		}

		public void setHostKeyRepository(HostKeyRepository foo)
		{
			m_known_hosts = foo;
		}
		public void setKnownHosts(string foo)
		{
			if (m_known_hosts == null) m_known_hosts = new KnownHosts(this);
			if (m_known_hosts is KnownHosts)
			{
				lock (m_known_hosts)
				{
					((KnownHosts)m_known_hosts).setKnownHosts(foo);
				}
			}
		}
		public void setKnownHosts(StreamReader foo)
		{
			if (m_known_hosts == null) m_known_hosts = new KnownHosts(this);
			if (m_known_hosts is KnownHosts)
			{
				lock (m_known_hosts)
				{
					((KnownHosts)m_known_hosts).setKnownHosts(foo);
				}
			}
		}

		public HostKeyRepository getHostKeyRepository()
		{
			if (m_known_hosts == null) m_known_hosts = new KnownHosts(this);
			return m_known_hosts;
		}

		public void addIdentity(string foo)
		{
			addIdentity(foo, (String)null);
		}

		public void addIdentity(string foo, string passPhrase)
		{
			Identity identity = new IdentityFile(foo, this);
			if (passPhrase != null)
				identity.setPassphrase(passPhrase);
			m_identities.Add(identity);
		}
		
		internal string getConfig(string foo)
		{
			return m_config[foo];
		}

		void setProxy(string hosts, IProxy proxy)
		{
			string[] patterns = Util.split(hosts, ",");
			if (m_proxies == null)
				m_proxies = new System.Collections.ArrayList();

			lock (m_proxies)
			{
				for (int i = 0; i < patterns.Length; i++)
					if (proxy == null)
					{
						m_proxies[0] = null;
						m_proxies[0] = System.Text.Encoding.Default.GetBytes(patterns[i]);
					}
					else
					{
						m_proxies.Add(System.Text.Encoding.Default.GetBytes(patterns[i]));
						m_proxies.Add(proxy);
					}
			}
		}
		internal IProxy getProxy(string host)
		{
			if (m_proxies == null)
				return null;
			byte[] _host = System.Text.Encoding.Default.GetBytes(host);
			lock (m_proxies)
			{
				for (int i = 0; i < m_proxies.Count; i += 2)
					if (Util.glob(((byte[])m_proxies[i]), _host))
						return (IProxy)(m_proxies[i + 1]);
			}
			return null;
		}

		internal void removeProxy()
		{
			m_proxies = null;
		}

		public static void setConfig(System.Collections.Hashtable foo)
		{
			lock (m_config)
			{
				System.Collections.IEnumerator e = foo.Keys.GetEnumerator();
				while (e.MoveNext())
				{
					string key = (string)(e.Current);
					m_config.Add(key, (string)(foo[key]));
				}
			}
		}
	}
}
