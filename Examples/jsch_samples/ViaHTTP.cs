using System;
using SharpSsh.jsch;

/* ViaHTTP.cs
 * ====================================================================
 * The following example was posted with the original JSch java library,
 * and is translated to C# to show the usage of SharpSSH JSch API
 * ====================================================================
 * */
namespace SharpSshTest.jsch_samples
{
    /// <summary>
    /// This program will demonstrate the ssh session via HTTP proxy.
    /// You will be asked username, hostname and passwd. 
    /// If everything works fine, you will get the shell prompt. Output will
    /// be ugly because of lacks of terminal-emulation, but you can issue commands.
    /// </summary>
    public class ViaHTTP
    {
        public static void RunExample(string[] arg)
        {
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

                string proxy = InputForm.GetUserInput("Enter proxy server", "hostname:port");

                string proxy_host = proxy.Substring(0, proxy.IndexOf(':'));
                int proxy_port = int.Parse(proxy.Substring(proxy.IndexOf(':') + 1));

                session.setProxy(new ProxyHTTP(proxy_host, proxy_port));

                // username and password will be given via UserInfo interface.
                UserInfo ui = new UserInfoViaHTTP();
                session.setUserInfo(ui);

                // Connect to remote SSH server
                session.Connect();

                // Open a new Shell channel on the SSH session
                Channel channel = session.openChannel("shell");

                // Redirect standard I/O to the SSH channel
                channel.setInputStream(Console.OpenStandardInput());
                channel.setOutputStream(Console.OpenStandardOutput());

                // Connect the channel
                channel.connect();

                Console.WriteLine("-- Shell channel is connected using the {0} cipher", session.Cipher);

                // Wait till channel is closed
                while (!channel.IsClosed)
                    System.Threading.Thread.Sleep(500);

                // Disconnect from remote server
                channel.disconnect();
                session.disconnect();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #region UserInfoViaHTTP
        /// <summary>
        /// A user info for getting user data
        /// </summary>
        public class UserInfoViaHTTP : UserInfo, UIKeyboardInteractive
        {
            /// <summary>
            /// Prompt the user for a Yes/No input
            /// </summary>
            public override bool promptYesNo(string message)
            {
                return InputForm.PromptYesNo(message);
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

            public string[] promptKeyboardInteractive(string destination, string name, string instruction, string[] prompt, bool[] echo)
            {
                string prmpt = prompt != null && prompt.Length > 0 ? prompt[0] : "";
                m_passwd = InputForm.GetUserInput(prmpt, true);
                return new string[] { m_passwd };
            }
        }
        #endregion
    }
}
