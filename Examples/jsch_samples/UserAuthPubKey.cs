using System;
using SharpSsh.jsch;

/* UserAuthPubKey.cs
 * ====================================================================
 * The following example was posted with the original JSch java library,
 * and is translated to C# to show the usage of SharpSSH JSch API
 * ====================================================================
 * */
namespace SharpSshTest.jsch_samples
{
    /// <summary>
    /// This program will demonstrate the user authentification by public key.
    /// You will be asked username, hostname, privatekey(id_dsa) and passphrase. 
    /// If everything works fine, you will get the shell prompt
    /// </summary>
    public class UserAuthPubKey
    {
        public static void RunExample(string[] arg)
        {
            try
            {
                JSch jsch = new JSch();

                //Get the "known hosts" filename from the user
                Console.WriteLine("Please choose your private key file...");
                string file = InputForm.GetFileFromUser("Choose your privatekey(ex. ~/.ssh/id_dsa)");
                Console.WriteLine("You chose " + file + ".");

                //Add the identity file to JSch
                jsch.addIdentity(file);

                //Prompt for username and server host
                Console.WriteLine("Please enter the user and host info at the popup window...");
                string host = InputForm.GetUserInput("Enter username@hostname", Environment.UserName + "@localhost");
                string user = host.Substring(0, host.IndexOf('@'));
                host = host.Substring(host.IndexOf('@') + 1);

                //Create a new SSH session
                Session session = jsch.getSession(user, host, SharpSsh.SshBase.SSH_TCP_PORT);

                // username and password will be given via UserInfo interface.
                UserInfo ui = new UserInfoPubKey();
                session.setUserInfo(ui);

                //Connect to remote SSH server
                session.Connect();

                //Open a new Shell channel on the SSH session
                Channel channel = session.openChannel("shell");

                //Redirect standard I/O to the SSH channel
                channel.setInputStream(Console.OpenStandardInput());
                channel.setOutputStream(Console.OpenStandardOutput());

                //Connect the channel
                channel.connect();

                //Wait till channel is closed
                while (!channel.IsClosed)
                    System.Threading.Thread.Sleep(500);

                //Disconnect from remote server
                channel.disconnect();
                session.disconnect();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #region UserInfoPubKey
        /// <summary>
        /// A user info for getting user data
        /// </summary>
        public class UserInfoPubKey : UserInfo
        {
            /// <summary>
            /// Holds the key file passphrase
            /// </summary>
            private string m_passphrase;

            /// <summary>
            /// Returns the user password
            /// </summary>
            public override string Password { get { return null; } }

            /// <summary>
            /// Prompt the user for a Yes/No input
            /// </summary>
            public override bool promptYesNo(string message)
            {
                return InputForm.PromptYesNo(message);
            }

            /// <summary>
            /// Returns the user passphrase (passwd for the private key file)
            /// </summary>
            public override string Passphrase { get { return m_passphrase; } }

            /// <summary>
            /// Prompt the user for a passphrase (passwd for the private key file)
            /// </summary>
            public override bool promptPassphrase(string message)
            {
                m_passphrase = InputForm.GetUserInput(message, true);
                return true;
            }

            /// <summary>
            /// Prompt the user for a password
            /// </summary>
            public override bool promptPassword(string message) { return true; }
            public override void showMessage(string message)
            {
                InputForm.ShowMessage(message);
            }
        }
        #endregion
    }
}
