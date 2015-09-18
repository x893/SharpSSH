using System;
using SharpSsh.jsch;

/* PortForwardingL.cs
 * ====================================================================
 * The following example was posted with the original JSch java library,
 * and is translated to C# to show the usage of SharpSSH JSch API
 * ====================================================================
 * */
namespace SharpSshTest.jsch_samples
{
    /// <summary>
    /// This program will demonstrate the port forwarding like option -L of
    /// ssh command; the given port on the local host will be forwarded to
    /// the given remote host and port on the remote side.
    /// You will be asked username, hostname, port:host:hostport and passwd. 
    /// If everything works fine, you will get the shell prompt.
    /// Try the port on localhost.
    /// </summary>
    public class PortForwardingL
    {
        public static void RunExample(string[] arg)
        {
            //int port;

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

                // Get from user the local port, remote host and remote host port
                string foo = InputForm.GetUserInput("Enter -L port:host:hostport", "port:host:hostport");
                int lport = int.Parse(foo.Substring(0, foo.IndexOf(':')));
                foo = foo.Substring(foo.IndexOf(':') + 1);
                string rhost = foo.Substring(0, foo.IndexOf(':'));
                int rport = int.Parse(foo.Substring(foo.IndexOf(':') + 1));

                // username and password will be given via UserInfo interface.
                UserInfo ui = new UserInfoPortForwardingL();
                session.setUserInfo(ui);
                session.Connect();

                Console.WriteLine("localhost:" + lport + " -> " + rhost + ":" + rport);

                // Set port forwarding on the opened session
                session.setPortForwardingL(lport, rhost, rport);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

		#region UserInfoPortForwardingL
		/// <summary>
		/// A user info for getting user data
		/// </summary>
		public class UserInfoPortForwardingL : UserInfo
        {
            /// <summary>
            /// Prompt the user for a Yes/No input
            /// </summary>
            public override bool promptYesNo(string message)
            {
                return InputForm.PromptYesNo(message);
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
