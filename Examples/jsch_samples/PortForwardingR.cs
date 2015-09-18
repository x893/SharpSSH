using System;
using SharpSsh.jsch;

/* PortForwardingR.cs
 * ====================================================================
 * The following example was posted with the original JSch java library,
 * and is translated to C# to show the usage of SharpSSH JSch API
 * ====================================================================
 * */
namespace SharpSshTest.jsch_samples
{
	/// <summary>
	/// This program will demonstrate the port forwarding like option -R of
	/// ssh command; the given port on the remote host will be forwarded to
	/// the given host and port  on the local side.
	/// You will be asked username, hostname, port:host:hostport and passwd. 
	/// If everything works fine, you will get the shell prompt.
	/// Try the port on remote host.
	/// </summary>
	public class PortForwardingR
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

				// Get from user the remote port, local host and local host port
				string foo = InputForm.GetUserInput("Enter -R port:host:hostport", "port:host:hostport");
				int rport = int.Parse(foo.Substring(0, foo.IndexOf(':')));
				foo = foo.Substring(foo.IndexOf(':') + 1);
				string lhost = foo.Substring(0, foo.IndexOf(':'));
				int lport = int.Parse(foo.Substring(foo.IndexOf(':') + 1));

				// username and password will be given via UserInfo interface.
				UserInfo ui = new UserInfoPortForwardingR();
				session.setUserInfo(ui);
				session.Connect();

				Console.WriteLine(host + ":" + rport + " -> " + lhost + ":" + lport);

				// Set port forwarding on the opened session
				session.setPortForwardingR(rport, lhost, lport);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		#region UserInfoPortForwardingR
		/// <summary>
		/// A user info for getting user data
		/// </summary>
		public class UserInfoPortForwardingR : UserInfo, UIKeyboardInteractive
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

			public string[] promptKeyboardInteractive(string destination, string name, string instruction, string[] prompt,
													  bool[] echo)
			{
				string prmpt = prompt != null && prompt.Length > 0 ? prompt[0] : "";
				m_passwd = InputForm.GetUserInput(prmpt, true);
				return new string[] { m_passwd };
			}
		}
		#endregion
	}
}