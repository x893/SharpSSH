using System;
using System.Collections;
using SharpSsh.jsch;
using SharpSsh.java.util;

namespace SharpSsh
{
    /// <summary>
    /// A wrapper class for JSch's SSH channel
    /// </summary>
    public abstract class SshBase
    {
        /// <summary>
        /// Default TCP port of SSH protocol
        /// </summary>
        public const int SSH_TCP_PORT = 22;

        protected string m_host;
        protected string m_user;
        protected string m_pass;
        protected JSch m_jsch;
        protected Session m_session;
        protected Channel m_channel;

        /// <summary>
        /// Constructs a new SSH instance
        /// </summary>
        /// <param name="host">The remote SSH host</param>
        /// <param name="user">The login username</param>
        /// <param name="password">The login password</param>
        public SshBase(string host, string user, string password)
        {
            Host = host;
            Username = user;
            Password = password;
            m_jsch = new JSch();
        }

        /// <summary>
        /// Constructs a new SSH instance
        /// </summary>
        /// <param name="host">The remote SSH host</param>
        /// <param name="user">The login username</param>
        public SshBase(string host, string user)
            : this(host, user, null)
        {
        }

        /// <summary>
        /// Adds identity file for publickey user authentication
        /// </summary>
        /// <param name="privateKeyFile">The path to the private key file</param>
        public virtual void AddIdentityFile(string privateKeyFile)
        {
            m_jsch.addIdentity(privateKeyFile);
        }

        /// <summary>
        /// Adds identity file for publickey user authentication
        /// </summary>
        /// <param name="privateKeyFile">The path to the private key file</param>
        /// <param name="passphrase">A passphrase for decrypting the private key file</param>
        public virtual void AddIdentityFile(string privateKeyFile, string passphrase)
        {
            m_jsch.addIdentity(privateKeyFile, passphrase);
        }

        protected abstract string ChannelType { get; }

        /// <summary>
        /// Connect to remote SSH server
        /// </summary>
        public virtual void Connect()
        {
            Connect(SSH_TCP_PORT);
        }

        /// <summary>
        /// Connect to remote SSH server
        /// </summary>
        /// <param name="tcpPort">The destination TCP port for this connection</param>
        public virtual void Connect(int tcpPort)
        {
            ConnectSession(tcpPort);
            ConnectChannel();
        }

        protected virtual void ConnectSession(int port)
        {
            m_session = m_jsch.getSession(m_user, m_host, port);
            if (Password != null)
                m_session.setUserInfo(new KeyboardInteractiveUserInfo(Password));
            StringDictionary config = new StringDictionary();
            config.Add("StrictHostKeyChecking", "no");
            m_session.setConfig(config);
            m_session.Connect();
        }

        protected virtual void ConnectChannel()
        {
            m_channel = m_session.openChannel(ChannelType);
            OnChannelReceived();
            m_channel.connect();
            OnConnected();
        }

        protected virtual void OnConnected()
        {
        }

        protected virtual void OnChannelReceived()
        {
        }

        /// <summary>
        /// Closes the SSH subsystem
        /// </summary>
        public virtual void Close()
        {
            if (m_channel != null)
            {
                m_channel.disconnect();
                m_channel = null;
            }
            if (m_session != null)
            {
                m_session.disconnect();
                m_session = null;
            }
        }

        /// <summary>
        /// Return true if the SSH subsystem is connected
        /// </summary>
        public virtual bool Connected
        {
            get
            {
                if (m_session != null)
                    return m_session.IsConnected();
                return false;
            }
        }

        /// <summary>
        /// Gets the Cipher algorithm name used in this SSH connection.
        /// </summary>
        public string Cipher
        {
            get
            {
                CheckConnected();
                return m_session.Cipher;
            }
        }

        /// <summary>
        /// Gets the MAC algorithm name used in this SSH connection.
        /// </summary>
        public string Mac
        {
            get
            {
                CheckConnected();
                return m_session.Mac;
            }
        }

        /// <summary>
        /// Gets the server SSH version string.
        /// </summary>
        public string ServerVersion
        {
            get
            {
                CheckConnected();
                return m_session.getServerVersion();
            }
        }

        /// <summary>
        /// Gets the client SSH version string.
        /// </summary>
        public string ClientVersion
        {
            get
            {
                CheckConnected();
                return m_session.getClientVersion();
            }
        }

        public string Host
        {
            get
            {
                CheckConnected();
                return m_session.Host;
            }
            set
            {
                m_host = value;
            }
        }

        public HostKey HostKey
        {
            get
            {
                CheckConnected();
                return m_session.HostKey;
            }
        }

        public int Port
        {
            get
            {
                CheckConnected();
                return m_session.Port;
            }
        }

        /// <summary>
        /// The password string of the SSH subsystem
        /// </summary>
        public string Password
        {
            get { return m_pass; }
            set { m_pass = value; }
        }
        public string Username
        {
            get { return m_user; }
            set { m_user = value; }
        }

        public static Version Version
        {
            get
            {
                System.Reflection.Assembly asm
                    = System.Reflection.Assembly.GetAssembly(typeof(SharpSsh.SshBase));
                return asm.GetName().Version;
            }
        }

        private void CheckConnected()
        {
            if (!Connected)
                throw new Exception("SSH session is not connected.");
        }

        /// <summary>
        /// For password and KI auth modes
        /// </summary>
        protected class KeyboardInteractiveUserInfo : UserInfo, UIKeyboardInteractive
        {
            public KeyboardInteractiveUserInfo(string password)
            {
                m_passwd = password;
            }

            #region UIKeyboardInteractive Members

            public string[] promptKeyboardInteractive(string destination, string name, string instruction, string[] prompt, bool[] echo)
            {
                return new string[] { m_passwd };
            }

            #endregion

            #region UserInfo Members

            public override bool promptYesNo(string message) { return true; }
            public override bool promptPassword(string message) { return true; }

            #endregion
        }
    }
}
