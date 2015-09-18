using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpSsh.jsch
{
    public class KnownHosts : HostKeyRepository
    {
        private const string KNOWN_HOSTS = "known_hosts";

        private JSch m_jsch = null;
        private string m_known_hosts = null;
        private List<HostKey> m_pool = null;

        internal KnownHosts(JSch jsch)
            : base()
        {
            m_jsch = jsch;
            m_pool = new List<HostKey>();
        }

        internal void setKnownHosts(string foo)
        {
            try
            {
                m_known_hosts = foo;
                FileStream fis = File.OpenRead(foo);
                setKnownHosts(new StreamReader(fis));
            }
            catch
            { }
        }
        internal void setKnownHosts(StreamReader stream)
        {
            m_pool.Clear();
            StringBuilder sb = new StringBuilder();
            byte i;
            int j;
            bool error = false;
            try
            {
                StreamReader fis = stream;
                string host;
                string key = null;
                HostKey.HostKeyTypes type;
                byte[] buf = new byte[1024];
                int bufl = 0;

            loop:
                while (true)
                {
                    bufl = 0;
                    while (true)
                    {
                        j = fis.Read();
                        if (j == -1) goto break_loop;
                        if (j == 0x0d) continue;
                        if (j == 0x0a) break;
                        buf[bufl++] = (byte)j;
                    }

                    j = 0;
                    while (j < bufl)
                    {
                        i = buf[j];
                        if (i == ' ' || i == '\t')
                        {
                            j++;
                            continue;
                        }
                        if (i == '#')
                        {
                            addInvalidLine(System.Text.Encoding.Default.GetString(buf, 0, bufl));
                            goto loop;
                        }
                        break;
                    }
                    if (j >= bufl)
                    {
                        addInvalidLine(System.Text.Encoding.Default.GetString(buf, 0, bufl));
                        goto loop;
                    }

                    sb.Length = 0;
                    while (j < bufl)
                    {
                        i = buf[j++];
                        if (i == 0x20 || i == '\t') break;
                        sb.Append((char)i);
                    }
                    host = sb.ToString();
                    if (j >= bufl || host.Length == 0)
                    {
                        addInvalidLine(System.Text.Encoding.Default.GetString(buf, 0, bufl));
                        goto loop;
                    }

                    sb.Length = 0;
                    type = HostKey.HostKeyTypes.UNKNOWN;
                    while (j < bufl)
                    {
                        i = buf[j++];
                        if (i == 0x20 || i == '\t')
                            break;
                        sb.Append((char)i);
                    }
                    if (sb.ToString().Equals("ssh-dss"))
                        type = HostKey.HostKeyTypes.SSHDSS;
                    else if (sb.ToString().Equals("ssh-rsa"))
                        type = HostKey.HostKeyTypes.SSHRSA;
                    else
                        j = bufl;

                    if (j >= bufl)
                    {
                        addInvalidLine(Util.getString(buf, 0, bufl));
                        goto loop;
                    }

                    sb.Length = 0;
                    while (j < bufl)
                    {
                        i = buf[j++];
                        if (i == '\r')
                            continue;
                        if (i == '\n')
                            break;
                        sb.Append((char)i);
                    }
                    key = sb.ToString();
                    if (key.Length == 0)
                    {
                        addInvalidLine(Util.getString(buf, 0, bufl));
                        goto loop;
                    }

                    HostKey hk = new HostKey(host, type,
                        Util.fromBase64(Util.getBytes(key), 0,
                        key.Length));
                    m_pool.Add(hk);
                }

            break_loop:

                fis.Close();
                if (error)
                    throw new JSchException("KnownHosts: invalid format");
            }
            catch (Exception e)
            {
                if (e is JSchException)
                    throw (JSchException)e;
                throw new JSchException(e.ToString());
            }
        }
        private void addInvalidLine(string line)
        {
            HostKey hk = new HostKey(line, HostKey.HostKeyTypes.UNKNOWN, null);
            m_pool.Add(hk);
        }
        string getKnownHostsFile() { return m_known_hosts; }
        public override string getKnownHostsRepositoryID() { return m_known_hosts; }

        public override int check(string host, byte[] key)
        {
            HostKey hk;
            int result = NOT_INCLUDED;
            HostKey.HostKeyTypes type = getType(key);
            for (int i = 0; i < m_pool.Count; i++)
            {
                hk = m_pool[i];
                if (isIncluded(hk.m_host, host) && hk.m_type == type)
                {
                    if (Util.array_equals(hk.m_key, key))
                        return OK;
                    else
                        result = CHANGED;
                }
            }
            return result;
        }
        public override void add(string host, byte[] key, UserInfo userinfo)
        {
            HostKey hk;
            HostKey.HostKeyTypes type = getType(key);
            for (int i = 0; i < m_pool.Count; i++)
            {
                hk = m_pool[i];
                if (isIncluded(hk.m_host, host) && hk.m_type == type)
                {
                    /*
							if(Util.array_equals(hk.key, key)){ return; }
							if(hk.host.equals(host)){
							hk.key=key;
							return;
						}
						else{
							hk.host=deleteSubString(hk.host, host);
						break;
						}
					*/
                }
            }
            hk = new HostKey(host, type, key);
            m_pool.Add(hk);

            string bar = getKnownHostsRepositoryID();
            if (userinfo != null &&
                bar != null)
            {
                bool foo = true;
                FileInfo goo = new FileInfo(bar);
                if (!goo.Exists)
                {
                    foo = false;
                    if (userinfo != null)
                    {
                        foo = userinfo.promptYesNo(
                            bar + " does not exist.\n" +
                            "Are you sure you want to create it?"
                            );
                        DirectoryInfo dir = goo.Directory;
                        if (foo && dir != null && !dir.Exists)
                        {
                            foo = userinfo.promptYesNo(
                                "The parent directory " + dir.Name + " does not exist.\n" +
                                "Are you sure you want to create it?"
                                );
                            if (foo)
                            {
                                try
                                {
                                    dir.Create(); userinfo.showMessage(dir.Name + " has been succesfully created.\nPlease check its access permission.");
                                }
                                catch
                                {
                                    userinfo.showMessage(dir.Name + " has not been created.");
                                    foo = false;
                                }
                            }
                        }
                        if (goo == null) foo = false;
                    }
                }
                if (foo)
                {
                    try
                    {
                        sync(bar);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("sync known_hosts: " + e);
                    }
                }
            }
        }

        public override HostKey[] getHostKey()
        {
            return getHostKey(null, null);
        }
        public override HostKey[] getHostKey(string host, string type)
        {
            lock (m_pool)
            {
                int count = 0;
                for (int i = 0; i < m_pool.Count; i++)
                {
                    HostKey hk = m_pool[i];
                    if (hk.m_type == HostKey.HostKeyTypes.UNKNOWN)
                        continue;
                    if (host == null ||
                        (isIncluded(hk.m_host, host) &&
                        (type == null || hk.getType().Equals(type)))
                        )
                    {
                        count++;
                    }
                }
                if (count == 0) return null;
                HostKey[] foo = new HostKey[count];
                int j = 0;
                for (int i = 0; i < m_pool.Count; i++)
                {
                    HostKey hk = m_pool[i];
                    if (hk.m_type == HostKey.HostKeyTypes.UNKNOWN) continue;
                    if (host == null ||
                        (isIncluded(hk.m_host, host) &&
                        (type == null || hk.getType().Equals(type))))
                    {
                        foo[j++] = hk;
                    }
                }
                return foo;
            }
        }
        public override void remove(string host, string type)
        {
            remove(host, type, null);
        }
        public override void remove(string host, string type, byte[] key)
        {
            bool _sync = false;
            for (int i = 0; i < m_pool.Count; i++)
            {
                HostKey hk = (HostKey)(m_pool[i]);
                if (host == null || (hk.Host.Equals(host) && (type == null || (hk.getType().Equals(type) && (key == null || Util.array_equals(key, hk.m_key))))))
                {
                    m_pool.Remove(hk);
                    _sync = true;
                }
            }
            if (_sync)
            {
                try { sync(); }
                catch { };
            }
        }

        private void sync()
        {
            if (m_known_hosts != null)
                sync(m_known_hosts);
        }
        private void sync(string foo)
        {
            if (foo == null) return;
            FileStream fos = File.OpenWrite(foo);
            dump(fos);
            fos.Close();
        }

        private static byte[] space = new byte[] { (byte)0x20 };
        private static byte[] cr = System.Text.Encoding.Default.GetBytes("\n");
        void dump(FileStream outs)
        {
            //StreamWriter outs = new StreamWriter(fs);
            try
            {
                HostKey hk;
                for (int i = 0; i < m_pool.Count; i++)
                {
                    hk = m_pool[i];
                    //hk.dump(out);
                    string host = hk.Host;
                    string type = hk.getType();
                    if (type.Equals("UNKNOWN"))
                    {
                        Write(outs, Util.getBytes(host));
                        Write(outs, cr);
                        continue;
                    }
                    Write(outs, Util.getBytes(host));
                    Write(outs, space);
                    Write(outs, Util.getBytes(type));
                    Write(outs, space);
                    Write(outs, Util.getBytes(hk.getKey()));
                    Write(outs, cr);
                }
                outs.Flush();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void Write(Stream s, byte[] buff)
        {
            s.Write(buff, 0, buff.Length);
        }
        private HostKey.HostKeyTypes getType(byte[] key)
        {
            if (key[8] == 'd') return HostKey.HostKeyTypes.SSHDSS;
            if (key[8] == 'r') return HostKey.HostKeyTypes.SSHRSA;
            return HostKey.HostKeyTypes.UNKNOWN;
        }
        private string deleteSubString(string hosts, string host)
        {
            int i = 0;
            int hostlen = host.Length;
            int hostslen = hosts.Length;
            int j;
            while (i < hostslen)
            {
                j = hosts.IndexOf(',', i);
                if (j == -1) break;
                if (!host.Equals(hosts.Substring(i, j)))
                {
                    i = j + 1;
                    continue;
                }
                return hosts.Substring(0, i) + hosts.Substring(j + 1);
            }
            if (hosts.EndsWith(host) && hostslen - i == hostlen)
                return hosts.Substring(0, (hostlen == hostslen) ? 0 : hostslen - hostlen - 1);
            return hosts;
        }

        private bool isIncluded(string hosts, string host)
        {
            int i = 0;
            int hostlen = host.Length;
            int hostslen = hosts.Length;
            int j;
            while (i < hostslen)
            {
                j = hosts.IndexOf(',', i);
                if (j == -1)
                {
                    if (hostlen != hostslen - i) return false;
                    return Util.RegionMatches(hosts, true, i, host, 0, hostlen);
                }

                if (hostlen == (j - i))
                    if (Util.RegionMatches(hosts, true, i, host, 0, hostlen)) return true;
                i = j + 1;
            }
            return false;
        }
    }
}
