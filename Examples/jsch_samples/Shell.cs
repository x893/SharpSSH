using System;
using SharpSsh.jsch;

/* Shell.cs
 * ====================================================================
 * The following example was posted with the original JSch java library,
 * and is translated to C# to show the usage of SharpSSH JSch API
 * ====================================================================
 * */
namespace SharpSshTest.jsch_samples
{
    /// <summary>
    /// This program enables you to connect to sshd server and get the shell prompt.
    /// You will be asked username, hostname and passwd. 
    /// If everything works fine, you will get the shell prompt. Output will
    /// be ugly because of lacks of terminal-emulation, but you can issue commands.
    /// </summary>
    public class Shell
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

                // username and password will be given via UserInfo interface.
                UserInfo ui = new UserInfoShell();
                session.setUserInfo(ui);

                // Connect to remote SSH server
                session.Connect();

                // Open a new Shell channel on the SSH session
                Channel channel = session.openChannel("shell");

                //Redirect standard I/O to the SSH channel
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

        #region UserInfoShell

        /// <summary>
        /// A user info for getting user data
        /// </summary>
        public class UserInfoShell : UserInfo, UIKeyboardInteractive
        {
            /// <summary>
            /// Prompt the user for a Yes/No input
            /// </summary>
            public override bool promptYesNo(string str)
            {
                return InputForm.PromptYesNo(str);
            }

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

            #region UIKeyboardInteractive Members

            public string[] promptKeyboardInteractive(string destination, string name, string instruction, string[] prompt, bool[] echo)
            {
                string prmpt = prompt != null && prompt.Length > 0 ? prompt[0] : "";
                m_passwd = InputForm.GetUserInput(prmpt, true);
                return new string[] { m_passwd };
            }

            #endregion
        }
        #endregion
    }
}

