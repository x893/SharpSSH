using System;
using SharpSsh.jsch;
using SharpSsh.java.util;

/* AES.cs 
 * ====================================================================
 * The following example was posted with the original JSch java library,
 * and is translated to C# to show the usage of SharpSSH JSch API
 * ====================================================================
 * */

namespace SharpSshTest.jsch_samples
{
    /// <summary>
    /// This program will demonstrate how to use "aes128-cbc" encryption.
    /// </summary>
    public class AES
    {
        public static void RunExample(string[] arg)
        {
            try
            {
                //Create a new JSch instance
                JSch jsch = new JSch();

                //Prompt for username and server host
                Console.WriteLine("Please enter the user and host info at the popup window...");
                string host = InputForm.GetUserInput("Enter username@hostname", Environment.UserName + "@localhost");
                string user = host.Substring(0, host.IndexOf('@'));
                host = host.Substring(host.IndexOf('@') + 1);

                //Create a new SSH session
                Session session = jsch.getSession(user, host, SharpSsh.SshBase.SSH_TCP_PORT);

                // username and password will be given via UserInfo interface.
                UserInfo ui = new UserInfoAES();
                session.setUserInfo(ui);

                //Add AES128 as default cipher in the session config store
                StringDictionary config = new StringDictionary(2);
                config.Add("cipher.s2c", "aes128-cbc,3des-cbc");
                config.Add("cipher.c2s", "aes128-cbc,3des-cbc");
                session.setConfig(config);

                //Connect to remote SSH server
                session.Connect();

                //Open a new Shell channel on the SSH session
                Channel channel = session.openChannel("shell");

                //Redirect standard I/O to the SSH channel
                channel.setInputStream(Console.OpenStandardInput());
                channel.setOutputStream(Console.OpenStandardOutput());

                //Connect the channel
                channel.connect();

                Console.WriteLine("-- Shell channel is connected using the {0} cipher", session.Cipher);

                //Wait till channel is closed
                while (!channel.IsClosed)
                    System.Threading.Thread.Sleep(500);

                //Disconnect from remote server
                channel.disconnect();
                session.disconnect();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        #region UserInfoAES
        /// <summary>
        /// A user info for getting user data
        /// </summary>
        public class UserInfoAES : UserInfo
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
        }
        #endregion
    }
}
