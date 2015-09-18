using System;
using SharpSsh.jsch;

/* StreamForwarding.cs
 * ====================================================================
 * The following example was posted with the original JSch java library,
 * and is translated to C# to show the usage of SharpSSH JSch API
 * ====================================================================
 * */
namespace SharpSshTest.jsch_samples
{
    /// <summary>
    /// This program will demonstrate the stream forwarding. The given Java
    /// I/O streams will be forwared to the given remote host and port on
    /// the remote side.  It is simmilar to the -L option of ssh command,
    /// but you don't have to assign and open a local tcp port.
    /// You will be asked username, hostname, host:hostport and passwd. 
    /// If everything works fine, System.in and System.out streams will be
    /// forwared to remote port and you can send messages from command line.
    /// </summary>
    public class StreamForwarding
    {
        public static void RunExample(string[] arg)
        {
            int port;

            try
            {
                // Create a new JSch instance
                JSch jsch = new JSch();

                // Prompt for username and server host
                Console.WriteLine("Please enter the user and host info at the popup window...");
                string host = InputForm.GetUserInput("Enter username@hostname", Environment.UserName + "@localhost");
                string user = host.Substring(0, host.IndexOf('@'));
                host = host.Substring(host.IndexOf('@') + 1);

                // Create a new SSH session
                Session session = jsch.getSession(user, host, SharpSsh.SshBase.SSH_TCP_PORT);

                // username and password will be given via UserInfo interface.
                UserInfo ui = new UserInfoStreamForwarding();
                session.setUserInfo(ui);
                session.Connect();

                // Get from user the remote host and remote host port
                string foo = InputForm.GetUserInput("Enter host and port", "host:port");
                host = foo.Substring(0, foo.IndexOf(':'));
                port = int.Parse(foo.Substring(foo.IndexOf(':') + 1));

                Console.WriteLine("System.{in,out} will be forwarded to " + host + ":" + port + ".");
                Channel channel = session.openChannel("direct-tcpip");
                ((ChannelDirectTCPIP)channel).setInputStream(Console.OpenStandardInput());
                ((ChannelDirectTCPIP)channel).setOutputStream(Console.OpenStandardOutput());
                ((ChannelDirectTCPIP)channel).Host = host;
                ((ChannelDirectTCPIP)channel).Port = port;
                channel.connect();

                while (!channel.IsClosed)
                    System.Threading.Thread.Sleep(500);

                channel.disconnect();
                session.disconnect();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #region UserInfoStreamForwarding
        /// <summary>
        /// A user info for getting user data
        /// </summary>
        public class UserInfoStreamForwarding : UserInfo
        {
            /// <summary>
            /// Prompt the user for a Yes/No input
            /// </summary>
            public override bool promptYesNo(string str)
            {
                return InputForm.PromptYesNo(str);
            }

            /// <summary>
            /// Prompt the user for a passphrase (passwd for the private key file)
            /// </summary>
            public override bool promptPassphrase(string message) { return true; }

            /// <summary>
            /// Prompt the user for a password
            /// </summary>\
            public override bool promptPassword(string message)
            {
                m_passwd = InputForm.GetUserInput(message, true);
                return true;
            }

            /// <summary>
            /// Shows a message to the user
            /// </summary>
            public override void showMessage(string message)
            {
                InputForm.ShowMessage(message);
            }
        }
        #endregion
    }
}
