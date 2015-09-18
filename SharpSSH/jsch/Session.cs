using System.IO;
using System.Runtime.CompilerServices;
using SharpSsh.java;
using SharpSsh.java.util;
using SharpSsh.java.net;
using SharpSsh.java.lang;
using Exception = System.Exception;
using NullReferenceException = System.NullReferenceException;
using ThreadInterruptedException = System.Threading.ThreadInterruptedException;
using System;

namespace SharpSsh.jsch
{
    public class Session : IRunnable
    {
        int[] m_uncompress_len = new int[1];
        private int m_cipher_size = 8;

        private static string m_version = "SharpSSH-" + SharpSsh.SshBase.Version.ToString() + "-JSCH-0.1.28";

        // http://ietf.org/internet-drafts/draft-ietf-secsh-assignednumbers-01.txt
        internal const int SSH_MSG_DISCONNECT = 1;
        internal const int SSH_MSG_IGNORE = 2;
        internal const int SSH_MSG_UNIMPLEMENTED = 3;
        internal const int SSH_MSG_DEBUG = 4;
        internal const int SSH_MSG_SERVICE_REQUEST = 5;
        internal const int SSH_MSG_SERVICE_ACCEPT = 6;
        internal const int SSH_MSG_KEXINIT = 20;
        internal const int SSH_MSG_NEWKEYS = 21;
        internal const int SSH_MSG_KEXDH_INIT = 30;
        internal const int SSH_MSG_KEXDH_REPLY = 31;
        internal const int SSH_MSG_KEX_DH_GEX_GROUP = 31;
        internal const int SSH_MSG_KEX_DH_GEX_INIT = 32;
        internal const int SSH_MSG_KEX_DH_GEX_REPLY = 33;
        internal const int SSH_MSG_KEX_DH_GEX_REQUEST = 34;
        internal const int SSH_MSG_USERAUTH_REQUEST = 50;
        internal const int SSH_MSG_USERAUTH_FAILURE = 51;
        internal const int SSH_MSG_USERAUTH_SUCCESS = 52;
        internal const int SSH_MSG_USERAUTH_BANNER = 53;
        internal const int SSH_MSG_USERAUTH_INFO_REQUEST = 60;
        internal const int SSH_MSG_USERAUTH_INFO_RESPONSE = 61;
        internal const int SSH_MSG_USERAUTH_PK_OK = 60;
        internal const int SSH_MSG_GLOBAL_REQUEST = 80;
        internal const int SSH_MSG_REQUEST_SUCCESS = 81;
        internal const int SSH_MSG_REQUEST_FAILURE = 82;
        internal const int SSH_MSG_CHANNEL_OPEN = 90;
        internal const int SSH_MSG_CHANNEL_OPEN_CONFIRMATION = 91;
        internal const int SSH_MSG_CHANNEL_OPEN_FAILURE = 92;
        internal const int SSH_MSG_CHANNEL_WINDOW_ADJUST = 93;
        internal const int SSH_MSG_CHANNEL_DATA = 94;
        internal const int SSH_MSG_CHANNEL_EXTENDED_DATA = 95;
        internal const int SSH_MSG_CHANNEL_EOF = 96;
        internal const int SSH_MSG_CHANNEL_CLOSE = 97;
        internal const int SSH_MSG_CHANNEL_REQUEST = 98;
        internal const int SSH_MSG_CHANNEL_SUCCESS = 99;
        internal const int SSH_MSG_CHANNEL_FAILURE = 100;

        private byte[] m_server_version;                            // server version
        private byte[] m_client_version = Util.getBytesUTF8("SSH-2.0-" + m_version);  // client version

        private byte[] m_I_C; // the payload of the client's SSH_MSG_KEXINIT
        private byte[] m_I_S; // the payload of the server's SSH_MSG_KEXINIT

        private byte[] m_session_id;

        private byte[] m_IVc2s;
        private byte[] m_IVs2c;
        private byte[] m_Ec2s;
        private byte[] m_Es2c;
        private byte[] m_MACc2s;
        private byte[] m_MACs2c;

        private int m_seq_i = 0;
        private int m_seq_o = 0;

        private Cipher m_s2ccipher;
        private Cipher m_c2scipher;
        private MAC m_s2cmac;
        private MAC m_c2smac;
        private byte[] m_mac_buf;

        private Compression m_deflater;
        private Compression m_inflater;
        private IO m_io;
        private Socket m_socket;
        private int m_timeout = 0;
        private bool m_isConnected = false;
        private bool m_isAuthed = false;
        private Thread m_connectThread = null;
        private StringDictionary m_config = null;
        private IProxy m_proxy = null;
        private UserInfo m_userinfo;

        internal bool m_x11_forwarding = false;
        internal Stream m_In = null;
        internal Stream m_Out = null;
        internal static Random m_random;
        internal Buffer m_buf;
        internal Packet m_packet;
        internal SocketFactory m_socket_factory = null;

        internal string m_host = "127.0.0.1";
        internal int m_port = SharpSsh.SshBase.SSH_TCP_PORT;
        internal string m_username = null;
        internal string m_password = null;
        internal JSch m_jsch;

        IRunnable m_thread;
        private GlobalRequestReply m_grr = new GlobalRequestReply();
        private bool m_in_kex = false;
        private static byte[] m_keepalivemsg = Util.getBytesUTF8("keepalive@jcraft.com");
        private HostKey m_hostkey = null;

        internal Session(JSch jsch)
        {
            m_jsch = jsch;
            m_buf = new Buffer();
            m_packet = new Packet(m_buf);
        }

        public void Connect()
        {
            Connect(m_timeout);
        }

        public void Connect(int connectTimeout)
        {
            if (m_isConnected)
                throw new JSchException("session is already connected");

            m_io = new IO();
            if (m_random == null)
                try
                {
                    Class c = Class.ForName(getConfig("random"));
                    m_random = (Random)(c.Instance());
                }
                catch (Exception e)
                {
                    throw e;
                }

            Packet.setRandom(m_random);
            try
            {
                int i, j;
                if (m_proxy == null)
                {
                    m_proxy = m_jsch.getProxy(m_host);
                    if (m_proxy != null)
                        lock (m_proxy)
                        {
                            m_proxy.close();
                        }
                }

                if (m_proxy == null)
                {
                    Stream In;
                    Stream Out;
                    if (m_socket_factory == null)
                    {
                        m_socket = Util.createSocket(m_host, m_port, connectTimeout);
                        In = m_socket.getInputStream();
                        Out = m_socket.getOutputStream();
                    }
                    else
                    {
                        m_socket = m_socket_factory.createSocket(m_host, m_port);
                        In = m_socket_factory.getInputStream(m_socket);
                        Out = m_socket_factory.getOutputStream(m_socket);
                    }
                    //if(timeout>0){ socket.setSoTimeout(timeout); }
                    m_socket.setTcpNoDelay(true);
                    m_io.setInputStream(In);
                    m_io.setOutputStream(Out);
                }
                else
                    lock (m_proxy)
                    {
                        m_proxy.connect(m_socket_factory, m_host, m_port, connectTimeout);
                        m_io.setInputStream(m_proxy.InputStream);
                        m_io.setOutputStream(m_proxy.OutputStream);
                        m_socket = m_proxy.Socket;
                    }

                if (connectTimeout > 0 && m_socket != null)
                    m_socket.setSoTimeout(connectTimeout);

                m_isConnected = true;

                while (true)
                {

                    i = 0;
                    j = 0;
                    while (i < m_buf.m_buffer.Length)
                    {
                        j = m_io.getByte();
                        if (j < 0)
                            break;
                        m_buf.m_buffer[i] = (byte)j; i++;
                        if (j == 10)
                            break;
                    }
                    if (j < 0)
                        throw new JSchException("connection is closed by foreign host");

                    if (m_buf.m_buffer[i - 1] == '\n')
                    {
                        i--;
                        if (m_buf.m_buffer[i - 1] == '\r')
                            i--;
                    }

                    if (i > 4
                    && (i != m_buf.m_buffer.Length)
                    && (m_buf.m_buffer[0] != 'S' || m_buf.m_buffer[1] != 'S' || m_buf.m_buffer[2] != 'H' || m_buf.m_buffer[3] != '-')
                        )
                        continue;

                    if (i == m_buf.m_buffer.Length
                    || i < 7    // SSH-1.99 or SSH-2.0
                    || (m_buf.m_buffer[4] == '1' && m_buf.m_buffer[6] != '9')  // SSH-1.5
                        )
                        throw new JSchException("invalid server's version String");
                    break;
                }

                m_server_version = new byte[i];
                Array.Copy(m_buf.m_buffer, 0, m_server_version, 0, i);
                {
                    // Some Cisco devices will miss to read '\n' if it is sent separately.
                    byte[] foo = new byte[m_client_version.Length + 1];
                    Array.Copy(m_client_version, 0, foo, 0, m_client_version.Length);
                    foo[foo.Length - 1] = (byte)'\n';
                    m_io.put(foo, 0, foo.Length);
                }

                m_buf = read(m_buf);
                if (m_buf.m_buffer[5] != SSH_MSG_KEXINIT)
                    throw new JSchException("invalid protocol: " + m_buf.m_buffer[5]);

                KeyExchange kex = receive_kexinit(m_buf);

                while (true)
                {
                    m_buf = read(m_buf);
                    if (kex.getState() == m_buf.m_buffer[5])
                    {
                        bool result = kex.next(m_buf);
                        if (!result)
                        {
                            m_in_kex = false;
                            throw new JSchException("verify: " + result);
                        }
                    }
                    else
                    {
                        m_in_kex = false;
                        throw new JSchException("invalid protocol(kex): " + m_buf.m_buffer[5]);
                    }
                    if (kex.getState() == KeyExchange.STATE_END)
                        break;
                }

                try
                {
                    checkHost(m_host, kex);
                }
                catch (JSchException ee)
                {
                    m_in_kex = false;
                    throw ee;
                }

                send_newkeys();

                // receive SSH_MSG_NEWKEYS(21)
                m_buf = read(m_buf);
                if (m_buf.m_buffer[5] == SSH_MSG_NEWKEYS)
                    receive_newkeys(m_buf, kex);
                else
                {
                    m_in_kex = false;
                    throw new JSchException("invalid protocol(newkyes): " + m_buf.m_buffer[5]);
                }

                bool auth = false;
                bool auth_cancel = false;

                UserAuthNone usn = new UserAuthNone(m_userinfo);
                auth = usn.start(this);

                string methods = null;
                if (!auth)
                {
                    methods = usn.getMethods();
                    if (methods != null)
                        methods = methods.ToLowerInvariant();
                    else
                        methods = "publickey,password,keyboard-interactive";
                }

                while (true)
                {
                    while (!auth && methods != null && methods.Length > 0)
                    {
                        UserAuth us = null;
                        if (methods.StartsWith("publickey"))
                        {
                            lock (m_jsch.Identities)
                            {
                                if (m_jsch.Identities.Count > 0)
                                    us = new UserAuthPublicKey(m_userinfo);
                            }
                        }
                        else if (methods.StartsWith("keyboard-interactive"))
                        {
                            if (m_userinfo is UIKeyboardInteractive)
                                us = new UserAuthKeyboardInteractive(m_userinfo);
                        }
                        else if (methods.StartsWith("password"))
                            us = new UserAuthPassword(m_userinfo);

                        if (us != null)
                        {
                            try
                            {
                                auth = us.start(this);
                                auth_cancel = false;
                            }
                            catch (JSchAuthCancelException)
                            {
                                auth_cancel = true;
                            }
                            catch (JSchPartialAuthException ex)
                            {
                                methods = ex.getMethods();
                                auth_cancel = false;
                                continue;
                            }
                            catch (RuntimeException ee)
                            {
                                throw ee;
                            }
                            catch (Exception ee)
                            {
                                Console.WriteLine("ee: " + ee); // SSH_MSG_DISCONNECT: 2 Too many authentication failures
                            }
                        }

                        if (!auth)
                        {
                            int comma = methods.IndexOf(",");
                            if (comma == -1)
                                break;
                            methods = methods.Substring(comma + 1);
                        }
                    }
                    break;
                }

                if (connectTimeout > 0 || m_timeout > 0)
                    m_socket.setSoTimeout(m_timeout);

                if (auth)
                {
                    m_isAuthed = true;
                    m_connectThread = new Thread(this);
                    m_connectThread.Name = "Connect thread " + m_host + " session";
                    m_connectThread.Start();
                    return;
                }
                if (auth_cancel)
                    throw new JSchException("Auth cancel");
                throw new JSchException("Auth fail");
            }
            catch (Exception e)
            {
                m_in_kex = false;
                if (m_isConnected)
                {
                    try
                    {
                        m_packet.reset();
                        m_buf.putByte((byte)SSH_MSG_DISCONNECT);
                        m_buf.putInt(3);
                        m_buf.putString(e.ToString());
                        m_buf.putString("en");
                        write(m_packet);
                        disconnect();
                    }
                    catch (Exception)
                    { }
                }
                m_isConnected = false;

                if (e is RuntimeException)
                    throw (RuntimeException)e;
                if (e is JSchException)
                    throw (JSchException)e;
                throw new JSchException("Session.connect: " + e);
            }
        }

        private KeyExchange receive_kexinit(Buffer buf)
        {
            int j = buf.getInt();
            if (j != buf.Length)
            {    // packet was compressed and
                buf.getByte();           // j is the size of deflated packet.
                m_I_S = new byte[buf.m_index - 5];
            }
            else
                m_I_S = new byte[j - 1 - buf.getByte()];

            Array.Copy(buf.m_buffer, buf.m_s, m_I_S, 0, m_I_S.Length);

            send_kexinit();
            string[] guess = KeyExchange.guess(m_I_S, m_I_C);
            if (guess == null)
                throw new JSchException("Algorithm negotiation fail");

            if (!m_isAuthed &&
                (guess[KeyExchange.PROPOSAL_ENC_ALGS_CTOS].Equals("none")
                || (guess[KeyExchange.PROPOSAL_ENC_ALGS_STOC].Equals("none")))
                )
                throw new JSchException("NONE Cipher should not be chosen before authentification is successed.");

            KeyExchange kex = null;
            try
            {
                Class c = Class.ForName(getConfig(guess[KeyExchange.PROPOSAL_KEX_ALGS]));
                kex = (KeyExchange)(c.Instance());
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("kex: " + e);
            }
            kex.m_guess = guess;
            kex.init(this, m_server_version, m_client_version, m_I_S, m_I_C);
            return kex;
        }

        public void rekey()
        {
            send_kexinit();
        }
        private void send_kexinit()
        {
            if (m_in_kex)
                return;
            m_in_kex = true;

            m_packet.reset();
            m_buf.putByte((byte)SSH_MSG_KEXINIT);
            lock (m_random)
            {
                m_random.fill(m_buf.m_buffer, m_buf.m_index, 16);
                m_buf.skip(16);
            }

            m_buf.putString(getConfig("kex"));
            m_buf.putString(getConfig("server_host_key"));
            m_buf.putString(getConfig("cipher.c2s"));
            m_buf.putString(getConfig("cipher.s2c"));
            m_buf.putString(getConfig("mac.c2s"));
            m_buf.putString(getConfig("mac.s2c"));
            m_buf.putString(getConfig("compression.c2s"));
            m_buf.putString(getConfig("compression.s2c"));
            m_buf.putString(getConfig("lang.c2s"));
            m_buf.putString(getConfig("lang.s2c"));
            m_buf.putByte((byte)0);
            m_buf.putInt(0);

            m_buf.OffSet = 5;
            m_I_C = new byte[m_buf.Length];
            m_buf.getByte(m_I_C);

            write(m_packet);
        }

        private void send_newkeys()
        {
            // send SSH_MSG_NEWKEYS(21)
            m_packet.reset();
            m_buf.putByte((byte)SSH_MSG_NEWKEYS);
            write(m_packet);
        }

        private void checkHost(string host, KeyExchange kex)
        {
            string shkc = getConfig("StrictHostKeyChecking");

            byte[] K_S = kex.getHostKey();
            string key_type = kex.getKeyType();
            string key_fprint = kex.getFingerPrint();

            m_hostkey = new HostKey(host, K_S);

            HostKeyRepository hkr = m_jsch.getHostKeyRepository();
            int i = 0;
            lock (hkr)
            {
                i = hkr.check(host, K_S);
            }

            bool insert = false;

            if ((shkc.Equals("ask") || shkc.Equals("yes")) && i == HostKeyRepository.CHANGED)
            {
                string file = null;
                lock (hkr)
                {
                    file = hkr.getKnownHostsRepositoryID();
                }
                if (file == null)
                    file = "known_hosts";
                string message =
                    "WARNING: REMOTE HOST IDENTIFICATION HAS CHANGED!\n" +
                    "IT IS POSSIBLE THAT SOMEONE IS DOING SOMETHING NASTY!\n" +
                    "Someone could be eavesdropping on you right now (man-in-the-middle attack)!\n" +
                    "It is also possible that the " + key_type + " host key has just been changed.\n" +
                    "The fingerprint for the " + key_type + " key sent by the remote host is\n" +
                    key_fprint + ".\n" +
                    "Please contact your system administrator.\n" +
                    "Add correct host key in " + file + " to get rid of this message.";

                bool b = false;
                if (m_userinfo != null)
                    b = m_userinfo.promptYesNo(message + "\nDo you want to delete the old key and insert the new key?");

                //throw new JSchException("HostKey has been changed: "+host);
                if (!b)
                    throw new JSchException("HostKey has been changed: " + host);
                else
                    lock (hkr)
                    {
                        hkr.remove(host,
                                  (key_type.Equals("DSA") ? "ssh-dss" : "ssh-rsa"),
                                   null);
                        insert = true;
                    }
            }

            if ((shkc.Equals("ask") || shkc.Equals("yes")) && (i != HostKeyRepository.OK) && !insert)
            {
                if (shkc.Equals("yes"))
                    throw new JSchException("reject HostKey: " + host);

                if (m_userinfo != null)
                {
                    bool foo = m_userinfo.promptYesNo(
                        "The authenticity of host '" + host + "' can't be established.\n" +
                        key_type + " key fingerprint is " + key_fprint + ".\n" +
                        "Are you sure you want to continue connecting?"
                        );
                    if (!foo)
                        throw new JSchException("reject HostKey: " + host);
                    insert = true;
                }
                else
                {
                    if (i == HostKeyRepository.NOT_INCLUDED)
                        throw new JSchException("UnknownHostKey: " + host + ". " + key_type + " key fingerprint is " + key_fprint);
                    else throw new JSchException("HostKey has been changed: " + host);
                }
            }

            if (shkc.Equals("no") && HostKeyRepository.NOT_INCLUDED == i)
                insert = true;

            if (insert)
                lock (hkr)
                {
                    hkr.add(host, K_S, m_userinfo);
                }
        }

        public Channel openChannel(string type)
        {
            if (!m_isConnected)
                throw new JSchException("session is down");

            try
            {
                Channel channel = Channel.getChannel(type);
                addChannel(channel);
                channel.Init();
                return channel;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
        }

        // encode will bin invoked in write with synchronization.
        public void encode(Packet packet)
        {
            if (m_deflater != null)
                packet.m_buffer.m_index = m_deflater.compress(packet.m_buffer.m_buffer, 5, packet.m_buffer.m_index);

            if (m_c2scipher != null)
            {
                packet.padding(m_c2scipher.IVSize);
                int pad = packet.m_buffer.m_buffer[4];
                lock (m_random)
                {
                    m_random.fill(packet.m_buffer.m_buffer, packet.m_buffer.m_index - pad, pad);
                }
            }
            else
                packet.padding(8);

            byte[] mac = null;
            if (m_c2smac != null)
            {
                m_c2smac.update(m_seq_o);
                m_c2smac.update(packet.m_buffer.m_buffer, 0, packet.m_buffer.m_index);
                mac = m_c2smac.doFinal();
            }
            if (m_c2scipher != null)
            {
                byte[] buf = packet.m_buffer.m_buffer;
                m_c2scipher.update(buf, 0, packet.m_buffer.m_index, buf, 0);
            }
            if (mac != null)
                packet.m_buffer.putByte(mac);
        }

        public Buffer read(Buffer buf)
        {
            int j = 0;
            while (true)
            {
                buf.reset();
                m_io.getByte(buf.m_buffer, buf.m_index, m_cipher_size);
                buf.m_index += m_cipher_size;
                if (m_s2ccipher != null)
                    m_s2ccipher.update(buf.m_buffer, 0, m_cipher_size, buf.m_buffer, 0);

                j = Util.ToInt32(buf.m_buffer, 0);
                j = j - 4 - m_cipher_size + 8;
                if (j < 0 || (buf.m_index + j) > buf.m_buffer.Length)
                    throw new IOException("invalid data");

                if (j > 0)
                {
                    m_io.getByte(buf.m_buffer, buf.m_index, j);
                    buf.m_index += (j);
                    if (m_s2ccipher != null)
                        m_s2ccipher.update(buf.m_buffer, m_cipher_size, j, buf.m_buffer, m_cipher_size);
                }

                if (m_s2cmac != null)
                {
                    m_s2cmac.update(m_seq_i);
                    m_s2cmac.update(buf.m_buffer, 0, buf.m_index);
                    byte[] result = m_s2cmac.doFinal();
                    m_io.getByte(m_mac_buf, 0, m_mac_buf.Length);
                    if (!Arrays.Equals(result, m_mac_buf))
                        throw new IOException("MAC Error");
                }
                m_seq_i++;

                if (m_inflater != null)
                {
                    int pad = buf.m_buffer[4];
                    m_uncompress_len[0] = buf.m_index - 5 - pad;
                    byte[] foo = m_inflater.uncompress(buf.m_buffer, 5, m_uncompress_len);
                    if (foo != null)
                    {
                        buf.m_buffer = foo;
                        buf.m_index = 5 + m_uncompress_len[0];
                    }
                    else
                    {
                        Console.Error.WriteLine("fail in inflater");
                        break;
                    }
                }

                int type = buf.m_buffer[5] & 0xff;
                if (type == SSH_MSG_DISCONNECT)
                {
                    buf.rewind();
                    buf.getInt(); buf.getShort();
                    int reason_code = buf.getInt();
                    byte[] description = buf.getString();
                    byte[] language_tag = buf.getString();
                    throw new JSchException("SSH_MSG_DISCONNECT:" +
                        " " + reason_code +
                        " " + Util.getStringUTF8(description) +
						" " + Util.getStringUTF8(language_tag));
                }
                else if (type == SSH_MSG_IGNORE)
                {
                }
                else if (type == SSH_MSG_DEBUG)
                {
                    buf.rewind();
                    buf.getInt(); buf.getShort();
                }
                else if (type == SSH_MSG_CHANNEL_WINDOW_ADJUST)
                {
                    buf.rewind();
                    buf.getInt(); buf.getShort();
                    Channel c = Channel.FindChannel(buf.getInt(), this);
                    if (c != null)
                        c.addRemoteWindowSize(buf.getInt());
                }
                else
                    break;
            }
            buf.rewind();
            return buf;
        }

        internal byte[] getSessionId()
        {
            return m_session_id;
        }

        private void receive_newkeys(Buffer buf, KeyExchange kex)
        {
            updateKeys(kex);
            m_in_kex = false;
        }
        private void updateKeys(KeyExchange kex)
        {
            byte[] K = kex.getK();
            byte[] H = kex.getH();
            HASH hash = kex.getHash();

            string[] guess = kex.m_guess;

            if (m_session_id == null)
            {
                m_session_id = new byte[H.Length];
                Array.Copy(H, 0, m_session_id, 0, H.Length);
            }

            /*
			  Initial IV client to server:     HASH (K || H || "A" || session_id)
			  Initial IV server to client:     HASH (K || H || "B" || session_id)
			  Encryption key client to server: HASH (K || H || "C" || session_id)
			  Encryption key server to client: HASH (K || H || "D" || session_id)
			  Integrity key client to server:  HASH (K || H || "E" || session_id)
			  Integrity key server to client:  HASH (K || H || "F" || session_id)
			*/

            m_buf.reset();
            m_buf.putMPInt(K);
            m_buf.putByte(H);
            m_buf.putByte((byte)0x41);
            m_buf.putByte(m_session_id);
            hash.update(m_buf.m_buffer, 0, m_buf.m_index);
            m_IVc2s = hash.digest();

            int j = m_buf.m_index - m_session_id.Length - 1;

            m_buf.m_buffer[j]++;
            hash.update(m_buf.m_buffer, 0, m_buf.m_index);
            m_IVs2c = hash.digest();

            m_buf.m_buffer[j]++;
            hash.update(m_buf.m_buffer, 0, m_buf.m_index);
            m_Ec2s = hash.digest();

            m_buf.m_buffer[j]++;
            hash.update(m_buf.m_buffer, 0, m_buf.m_index);
            m_Es2c = hash.digest();

            m_buf.m_buffer[j]++;
            hash.update(m_buf.m_buffer, 0, m_buf.m_index);
            m_MACc2s = hash.digest();

            m_buf.m_buffer[j]++;
            hash.update(m_buf.m_buffer, 0, m_buf.m_index);
            m_MACs2c = hash.digest();

            try
            {
                Class c;

                c = Class.ForName(getConfig(guess[KeyExchange.PROPOSAL_ENC_ALGS_STOC]));
                m_s2ccipher = (Cipher)(c.Instance());
                while (m_s2ccipher.BlockSize > m_Es2c.Length)
                {
                    m_buf.reset();
                    m_buf.putMPInt(K);
                    m_buf.putByte(H);
                    m_buf.putByte(m_Es2c);
                    hash.update(m_buf.m_buffer, 0, m_buf.m_index);
                    byte[] foo = hash.digest();
                    byte[] bar = new byte[m_Es2c.Length + foo.Length];
                    Array.Copy(m_Es2c, 0, bar, 0, m_Es2c.Length);
                    Array.Copy(foo, 0, bar, m_Es2c.Length, foo.Length);
                    m_Es2c = bar;
                }
                m_s2ccipher.init(jsch.Cipher.DECRYPT_MODE, m_Es2c, m_IVs2c);
                m_cipher_size = m_s2ccipher.IVSize;
                c = Class.ForName(getConfig(guess[KeyExchange.PROPOSAL_MAC_ALGS_STOC]));
                m_s2cmac = (MAC)(c.Instance());
                m_s2cmac.init(m_MACs2c);
                m_mac_buf = new byte[m_s2cmac.BlockSize];

                c = Class.ForName(getConfig(guess[KeyExchange.PROPOSAL_ENC_ALGS_CTOS]));
                m_c2scipher = (Cipher)(c.Instance());
                while (m_c2scipher.BlockSize > m_Ec2s.Length)
                {
                    m_buf.reset();
                    m_buf.putMPInt(K);
                    m_buf.putByte(H);
                    m_buf.putByte(m_Ec2s);
                    hash.update(m_buf.m_buffer, 0, m_buf.m_index);
                    byte[] foo = hash.digest();
                    byte[] bar = new byte[m_Ec2s.Length + foo.Length];
                    Array.Copy(m_Ec2s, 0, bar, 0, m_Ec2s.Length);
                    Array.Copy(foo, 0, bar, m_Ec2s.Length, foo.Length);
                    m_Ec2s = bar;
                }
                m_c2scipher.init(jsch.Cipher.ENCRYPT_MODE, m_Ec2s, m_IVc2s);

                c = Class.ForName(getConfig(guess[KeyExchange.PROPOSAL_MAC_ALGS_CTOS]));
                m_c2smac = (MAC)(c.Instance());
                m_c2smac.init(m_MACc2s);

                if (!guess[KeyExchange.PROPOSAL_COMP_ALGS_CTOS].Equals("none"))
                {
                    string foo = getConfig(guess[KeyExchange.PROPOSAL_COMP_ALGS_CTOS]);
                    if (foo != null)
                    {
                        try
                        {
                            c = Class.ForName(foo);
                            m_deflater = (Compression)(c.Instance());
                            int level = 6;
                            try
                            {
                                level = int.Parse(getConfig("compression_level"));
                            }
                            catch (Exception)
                            { }
                            m_deflater.init(Compression.DEFLATER, level);
                        }
                        catch (Exception)
                        {
                            Console.Error.WriteLine(foo + " isn't accessible.");
                        }
                    }
                }
                else
                {
                    if (m_deflater != null)
                        m_deflater = null;
                }
                if (!guess[KeyExchange.PROPOSAL_COMP_ALGS_STOC].Equals("none"))
                {
                    string foo = getConfig(guess[KeyExchange.PROPOSAL_COMP_ALGS_STOC]);
                    if (foo != null)
                    {
                        try
                        {
                            c = Class.ForName(foo);
                            m_inflater = (Compression)(c.Instance());
                            m_inflater.init(Compression.INFLATER, 0);
                        }
                        catch (Exception)
                        {
                            Console.Error.WriteLine(foo + " isn't accessible.");
                        }
                    }
                }
                else
                {
                    if (m_inflater != null)
                        m_inflater = null;
                }
            }
            catch (Exception e) { Console.Error.WriteLine("updatekeys: " + e); }
        }

        public void write(Packet packet, Channel c, int length)
        {
            while (true)
            {
                if (m_in_kex)
                {
                    try
                    {
                        Thread.Sleep(10);
                    }
                    catch (ThreadInterruptedException)
                    { }
                    continue;
                }
                lock (c)
                {
                    if (c.RemoteWindowSize >= length)
                    {
                        c.RemoteWindowSize -= length;
                        break;
                    }
                }

                if (c.IsClosed || !c.isConnected())
                    throw new IOException("channel is broken");

                bool sendit = false;
                int s = 0;
                byte command = 0;
                int recipient = -1;
                lock (c)
                {
                    if (c.RemoteWindowSize > 0)
                    {
                        int len = c.RemoteWindowSize;
                        if (len > length)
                            len = length;

                        if (len != length)
                            s = packet.shift(len, (m_c2smac != null ? m_c2smac.BlockSize : 0));

                        command = packet.m_buffer.m_buffer[5];
                        recipient = c.Recipient;
                        length -= len;
                        c.RemoteWindowSize -= len;
                        sendit = true;
                    }
                }
                if (sendit)
                {
                    _write(packet);
                    if (length == 0)
                        return;

                    packet.unshift(command, recipient, s, length);
                    lock (c)
                    {
                        if (c.RemoteWindowSize >= length)
                        {
                            c.RemoteWindowSize -= length;
                            break;
                        }
                    }
                }

                try
                {
                    Thread.Sleep(100);
                }
                catch (ThreadInterruptedException)
                { }
            }
            _write(packet);
        }

        public void write(Packet packet)
        {
            while (m_in_kex)
            {
                byte command = packet.m_buffer.m_buffer[5];
                if (false
                || command == SSH_MSG_KEXINIT
                || command == SSH_MSG_NEWKEYS
                || command == SSH_MSG_KEXDH_INIT
                || command == SSH_MSG_KEXDH_REPLY
                || command == SSH_MSG_DISCONNECT
                || command == SSH_MSG_KEX_DH_GEX_GROUP
                || command == SSH_MSG_KEX_DH_GEX_INIT
                || command == SSH_MSG_KEX_DH_GEX_REPLY
                || command == SSH_MSG_KEX_DH_GEX_REQUEST
                    )
                    break;

                try
                {
                    Thread.Sleep(10);
                }
                catch (ThreadInterruptedException)
                { }
            }
            _write(packet);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void _write(Packet packet)
        {
            encode(packet);
            if (m_io != null)
            {
                m_io.put(packet);
                m_seq_o++;
            }
        }

        public void Run()
        {
            m_thread = this;

            byte[] foo;
            Buffer buf = new Buffer();
            Packet packet = new Packet(buf);
            int i = 0;
            Channel channel;
            int[] start = new int[1];
            int[] length = new int[1];
            KeyExchange kex = null;

            try
            {
                while (m_isConnected &&
                    m_thread != null)
                {
                    buf = read(buf);
                    int msgType = buf.m_buffer[5] & 0xff;

                    if (kex != null && kex.getState() == msgType)
                    {
                        bool result = kex.next(buf);
                        if (!result)
                            throw new JSchException("verify: " + result);
                        continue;
                    }

                    switch (msgType)
                    {
                        case SSH_MSG_KEXINIT:
                            kex = receive_kexinit(buf);
                            break;

                        case SSH_MSG_NEWKEYS:
                            send_newkeys();
                            receive_newkeys(buf, kex);
                            kex = null;
                            break;

                        case SSH_MSG_CHANNEL_DATA:
                            buf.getInt();
                            buf.getByte();
                            buf.getByte();
                            i = buf.getInt();
                            channel = Channel.FindChannel(i, this);
                            foo = buf.getString(start, length);
                            if (channel == null)
                                break;

                            try
                            {
                                channel.write(foo, start[0], length[0]);
                            }
                            catch (Exception)
                            {
                                try
                                {
                                    channel.disconnect();
                                }
                                catch (Exception)
                                { }
                                break;
                            }
                            int len = length[0];
                            channel.LocalWindowSize = channel.LocalWindowSize - len;
                            if (channel.LocalWindowSize < channel.LocalWindowSizeMax / 2)
                            {
                                packet.reset();
                                buf.putByte((byte)SSH_MSG_CHANNEL_WINDOW_ADJUST);
                                buf.putInt(channel.Recipient);
                                buf.putInt(channel.LocalWindowSizeMax - channel.LocalWindowSize);
                                write(packet);
                                channel.LocalWindowSize = channel.LocalWindowSizeMax;
                            }
                            break;

                        case SSH_MSG_CHANNEL_EXTENDED_DATA:
                            buf.getInt();
                            buf.getShort();
                            i = buf.getInt();
                            channel = Channel.FindChannel(i, this);
                            buf.getInt();   // data_type_code == 1
                            foo = buf.getString(start, length);

                            if (channel == null)
                                break;

                            channel.write_ext(foo, start[0], length[0]);

                            len = length[0];
                            channel.LocalWindowSize = channel.LocalWindowSize - len;
                            if (channel.LocalWindowSize < channel.LocalWindowSizeMax / 2)
                            {
                                packet.reset();
                                buf.putByte((byte)SSH_MSG_CHANNEL_WINDOW_ADJUST);
                                buf.putInt(channel.Recipient);
                                buf.putInt(channel.LocalWindowSizeMax - channel.LocalWindowSize);
                                write(packet);
                                channel.LocalWindowSize = channel.LocalWindowSizeMax;
                            }
                            break;

                        case SSH_MSG_CHANNEL_WINDOW_ADJUST:
                            buf.getInt();
                            buf.getShort();
                            i = buf.getInt();
                            channel = Channel.FindChannel(i, this);
                            if (channel == null)
                                break;

                            channel.addRemoteWindowSize(buf.getInt());
                            break;

                        case SSH_MSG_CHANNEL_EOF:
                            buf.getInt();
                            buf.getShort();
                            i = buf.getInt();
                            channel = Channel.FindChannel(i, this);
                            if (channel != null)
                                channel.eof_remote();
                            break;
                        case SSH_MSG_CHANNEL_CLOSE:
                            buf.getInt();
                            buf.getShort();
                            i = buf.getInt();
                            channel = Channel.FindChannel(i, this);
                            if (channel != null)
                                channel.disconnect();
                            break;
                        case SSH_MSG_CHANNEL_OPEN_CONFIRMATION:
                            buf.getInt();
                            buf.getShort();
                            i = buf.getInt();
                            channel = Channel.FindChannel(i, this);

                            channel.Recipient = buf.getInt();
                            channel.RemoteWindowSize = buf.getInt();
                            channel.RemotePacketSize = buf.getInt();
                            break;
                        case SSH_MSG_CHANNEL_OPEN_FAILURE:
                            buf.getInt();
                            buf.getShort();
                            i = buf.getInt();
                            channel = Channel.FindChannel(i, this);

                            int reason_code = buf.getInt();
                            channel.ExitStatus = reason_code;
                            channel.IsClosed = true;
                            channel.IsEofRemote = true;
                            channel.Recipient = 0;
                            break;
                        case SSH_MSG_CHANNEL_REQUEST:
                            buf.getInt();
                            buf.getShort();
                            i = buf.getInt();
                            foo = buf.getString();
                            bool reply = (buf.getByte() != 0);
                            channel = Channel.FindChannel(i, this);
                            if (channel != null)
                            {
                                byte reply_type = (byte)SSH_MSG_CHANNEL_FAILURE;
								if (Util.getStringUTF8(foo) == "exit-status")
								{
									i = buf.getInt();             // exit-status
									channel.ExitStatus = i;
									reply_type = (byte)SSH_MSG_CHANNEL_SUCCESS;
								}
                                if (reply)
                                {
                                    packet.reset();
                                    buf.putByte(reply_type);
                                    buf.putInt(channel.Recipient);
                                    write(packet);
                                }
                            }
                            break;
                        case SSH_MSG_CHANNEL_OPEN:
                            buf.getInt();
                            buf.getShort();
                            string ctyp = Util.getStringUTF8(buf.getString());

							if ("forwarded-tcpip" != ctyp && !("x11" == ctyp && m_x11_forwarding))
							{
								Console.WriteLine("Session.run: CHANNEL OPEN " + ctyp);
								throw new IOException("Session.run: CHANNEL OPEN " + ctyp);
							}
							else
							{
								channel = Channel.getChannel(ctyp);
								addChannel(channel);
								channel.getData(buf);
								channel.Init();

								packet.reset();
								buf.putByte((byte)SSH_MSG_CHANNEL_OPEN_CONFIRMATION);
								buf.putInt(channel.Recipient);
								buf.putInt(channel.Id);
								buf.putInt(channel.LocalWindowSize);
								buf.putInt(channel.LocalPacketSize);
								write(packet);
								Thread tmp = new Thread(channel);
								tmp.Name = "Channel " + ctyp + " " + m_host;
								tmp.Start();
								break;
							}
                        case SSH_MSG_CHANNEL_SUCCESS:
                            buf.getInt();
                            buf.getShort();
                            i = buf.getInt();
                            channel = Channel.FindChannel(i, this);
                            if (channel == null)
                                break;

                            channel.Replay = 1;
                            break;
                        case SSH_MSG_CHANNEL_FAILURE:
                            buf.getInt();
                            buf.getShort();
                            i = buf.getInt();
                            channel = Channel.FindChannel(i, this);
                            if (channel == null)
                                break;

                            channel.Replay = 0;
                            break;
                        case SSH_MSG_GLOBAL_REQUEST:
                            buf.getInt();
                            buf.getShort();
                            foo = buf.getString();       // request name
                            reply = (buf.getByte() != 0);
                            if (reply)
                            {
                                packet.reset();
                                buf.putByte((byte)SSH_MSG_REQUEST_FAILURE);
                                write(packet);
                            }
                            break;
                        case SSH_MSG_REQUEST_FAILURE:
                        case SSH_MSG_REQUEST_SUCCESS:
                            Thread t = m_grr.getThread();
                            if (t != null)
                            {
                                m_grr.setReply(msgType == SSH_MSG_REQUEST_SUCCESS ? 1 : 0);
                                t.Interrupt();
                            }
                            break;
                        default:
                            throw new IOException("Unknown SSH message type " + msgType);
                    }
                }
            }
            catch (Exception)
            { }

            try
            {
                disconnect();
            }
            catch (NullReferenceException)
            { }
            catch (Exception)
            { }

            m_isConnected = false;
        }

        public void disconnect()
        {
            if (!m_isConnected)
                return;

            Channel.disconnect(this);
            m_isConnected = false;
            PortWatcher.delPort(this);
            ChannelForwardedTCPIP.delPort(this);

            lock (m_connectThread)
            {
                m_connectThread.yield();
                m_connectThread.Interrupt();
                m_connectThread = null;
            }
            m_thread = null;
            try
            {
                if (m_io != null)
                {
                    if (m_io.m_ins != null)
                        m_io.m_ins.Close();
                    if (m_io.m_outs != null)
                        m_io.m_outs.Close();
                    if (m_io.m_outs_ext != null)
                        m_io.m_outs_ext.Close();
                }
                if (m_proxy == null)
                {
                    if (m_socket != null)
                        m_socket.Close();
                }
                else
                {
                    lock (m_proxy)
                    {
                        m_proxy.close();
                    }
                    m_proxy = null;
                }
            }
            catch { }

            m_io = null;
            m_socket = null;
            m_jsch.removeSession(this);
        }

        public void setPortForwardingL(int lport, string host, int rport)
        {
            setPortForwardingL("127.0.0.1", lport, host, rport);
        }
		public void setPortForwardingL(string boundaddress, int lport, string host, int rport)
        {
            setPortForwardingL(boundaddress, lport, host, rport, null);
        }
        public void setPortForwardingL(string boundaddress, int lport, string host, int rport, ServerSocketFactory ssf)
        {
            PortWatcher pw = PortWatcher.addPort(this, boundaddress, lport, host, rport, ssf);
            Thread tmp = new Thread(pw);
            tmp.Name = "PortWatcher Thread for " + host;
            tmp.Start();
        }
        public void delPortForwardingL(int lport)
        {
            delPortForwardingL("127.0.0.1", lport);
        }
        public void delPortForwardingL(string boundaddress, int lport)
        {
            PortWatcher.delPort(this, boundaddress, lport);
        }
        public string[] getPortForwardingL()
        {
            return PortWatcher.getPortForwarding(this);
        }

        public void setPortForwardingR(int rport, string host, int lport)
        {
            setPortForwardingR(rport, host, lport, (SocketFactory)null);
        }
        public void setPortForwardingR(int rport, string host, int lport, SocketFactory sf)
        {
            ChannelForwardedTCPIP.addPort(this, rport, host, lport, sf);
            setPortForwarding(rport);
        }

        public void setPortForwardingR(int rport, string daemon)
        {
            setPortForwardingR(rport, daemon, null);
        }

        public void setPortForwardingR(int rport, string daemon, System.Object[] arg)
        {
            ChannelForwardedTCPIP.addPort(this, rport, daemon, arg);
            setPortForwarding(rport);
        }

        private class GlobalRequestReply
        {
            private Thread m_thread = null;
            private int m_reply = -1;
            internal void setThread(Thread thread)
            {
                m_thread = thread;
                m_reply = -1;
            }
            internal Thread getThread() { return m_thread; }
            internal void setReply(int reply) { this.m_reply = reply; }
            internal int getReply() { return this.m_reply; }
        }

        private void setPortForwarding(int rport)
        {
            lock (m_grr)
            {
                Buffer buf = new Buffer(100); // ??
                Packet packet = new Packet(buf);

                try
                {
                    packet.reset();
                    buf.putByte((byte)SSH_MSG_GLOBAL_REQUEST);
                    buf.putString("tcpip-forward");
                    buf.putByte((byte)1);
                    buf.putString("0.0.0.0");
                    buf.putInt(rport);
                    write(packet);
                }
                catch (Exception e)
                {
                    throw new JSchException(e.ToString());
                }

                m_grr.setThread(Thread.currentThread());
                try
                {
                    Thread.Sleep(10000);
                }
                catch { }

                int reply = m_grr.getReply();
                m_grr.setThread(null);
                if (reply == 0)
                    throw new JSchException("remote port forwarding failed for listen port " + rport);
            }
        }
        public void delPortForwardingR(int rport)
        {
            ChannelForwardedTCPIP.delPort(this, rport);
        }

        internal void addChannel(Channel channel)
        {
            channel.Session = this;
        }

        public string getConfig(string name)
        {
            string value = null;
            if (m_config != null && m_config.ContainKey(name))
                value = m_config[name];
            if (string.IsNullOrEmpty(value))
                value = m_jsch.getConfig(name);
            return value;
        }

        public void setProxy(IProxy proxy) { this.m_proxy = proxy; }
        public void setHost(string host) { this.m_host = host; }
        public void setPort(int port) { this.m_port = port; }
        internal void setUserName(string foo) { this.m_username = foo; }
        public void setPassword(string foo) { this.m_password = foo; }
        public void setUserInfo(UserInfo userinfo) { this.m_userinfo = userinfo; }
        public void setInputStream(Stream In) { this.m_In = In; }
        public void setOutputStream(Stream Out) { this.m_Out = Out; }
        // public void setX11Host(string host) { ChannelX11.setHost(host); }
        // public void setX11Port(int port) { ChannelX11.setPort(port); }
        // public void setX11Cookie(string cookie) { ChannelX11.setCookie(cookie); }

        public void setConfig(StringDictionary foo)
        {
            if (m_config == null)
                m_config = new StringDictionary();
            foreach (string key in foo.Keys)
                m_config.Add(key, foo[key]);
        }

        public void setSocketFactory(SocketFactory foo) { m_socket_factory = foo; }
        public bool IsConnected() { return m_isConnected; }
        public int getTimeout() { return m_timeout; }

        public void setTimeout(int foo)
        {
            if (m_socket == null)
            {
                if (foo < 0)
                    throw new JSchException("invalid timeout value");
                m_timeout = foo;
                return;
            }
            try
            {
                m_socket.setSoTimeout(foo);
                m_timeout = foo;
            }
            catch (Exception e)
            {
                throw new JSchException(e.ToString());
            }
        }
        public string getServerVersion()
        {
            return Util.getStringUTF8(m_server_version);
        }
        public string getClientVersion()
        {
            return Util.getStringUTF8(m_client_version);
        }
        public void setClientVersion(string cv)
        {
            m_client_version = Util.getBytesUTF8(cv);
        }

        public void sendIgnore()
        {
            Buffer buf = new Buffer();
            Packet packet = new Packet(buf);
            packet.reset();
            buf.putByte((byte)SSH_MSG_IGNORE);
            write(packet);
        }

        public void sendKeepAliveMsg()
        {
            Buffer buf = new Buffer();
            Packet packet = new Packet(buf);
            packet.reset();
            buf.putByte((byte)SSH_MSG_GLOBAL_REQUEST);
            buf.putString(m_keepalivemsg);
            buf.putByte((byte)1);
            write(packet);
        }

        public HostKey HostKey { get { return m_hostkey; } }
        public string Host { get { return m_host; } }
        public string UserName { get { return m_username; } }
        public int Port { get { return m_port; } }

        public string Mac
        {
            get
            {
                string mac = string.Empty;
                if (m_s2cmac != null)
                    mac = m_s2cmac.Name;
                return mac;
            }
        }

        public string Cipher
        {
            get
            {
                string cipher = string.Empty;
                if (m_s2ccipher != null)
                    cipher = m_s2ccipher.ToString();
                return cipher;
            }
        }

        public int BufferLength
        {
            get { return m_buf.m_buffer.Length; }
        }
    }
}
