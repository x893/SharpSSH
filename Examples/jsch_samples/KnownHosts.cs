using System;
using SharpSsh.jsch;

/* KnownHosts.cs
 * ====================================================================
 * The following example was posted with the original JSch java library,
 * and is translated to C# to show the usage of SharpSSH JSch API
 * ====================================================================
 * */
namespace SharpSshTest.jsch_samples
{
	/// <summary>
	/// This program will demonstrate the 'known_hosts' file handling.
	/// You will be asked username, hostname, a path for 'known_hosts' and passwd. 
	/// If everything works fine, you will get the shell prompt.
	/// In current implementation, jsch only reads 'known_hosts' for checking
	/// and does not modify it.
	/// </summary>
	public class KnownHosts
	{
		public static void RunExample(string[] arg)
		{
			try
			{
				// Get the "known hosts" filename from the user
				Console.WriteLine("Please select your 'known_hosts' from the poup window...");
				string file = InputForm.GetFileFromUser("Choose your known_hosts(ex. ~/.ssh/known_hosts)");
				Console.WriteLine("You chose " + file + ".");
				// Create a new JSch instance
				JSch jsch = new JSch();
				//Set the known hosts file
				jsch.setKnownHosts(file);

				// Get the KnownHosts repository from JSchs
				HostKeyRepository hkr = jsch.getHostKeyRepository();

				// Print all known hosts and keys
				HostKey[] hks = hkr.getHostKey();
				HostKey hk;
				if (hks != null)
				{
					Console.WriteLine();
					Console.WriteLine("Host keys in " + hkr.getKnownHostsRepositoryID() + ":");
					for (int i = 0; i < hks.Length; i++)
					{
						hk = hks[i];
						Console.WriteLine(hk.Host + " " +
							hk.getType() + " " +
							hk.getFingerPrint(jsch)
							);
					}
					Console.WriteLine("");
				}

				// Now connect to the remote server...

				// Prompt for username and server host
				Console.WriteLine("Please enter the user and host info at the popup window...");
				string host = InputForm.GetUserInput("Enter username@hostname", Environment.UserName + "@localhost");
				string user = host.Substring(0, host.IndexOf('@'));
				host = host.Substring(host.IndexOf('@') + 1);

				// Create a new SSH session
				Session session = jsch.getSession(user, host, SharpSsh.SshBase.SSH_TCP_PORT);

				// username and password will be given via UserInfo interface.
				UserInfo ui = new UserInfoKnownHosts();
				session.setUserInfo(ui);

				// Connect to remote SSH server
				session.Connect();

				// Print the host key info
				// of the connected server:
				hk = session.HostKey;
				Console.WriteLine("HostKey: " +
					hk.Host + " " +
					hk.getType() + " " +
					hk.getFingerPrint(jsch)
					);

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
				Console.WriteLine(e.Message);
			}
		}

		#region UserInfoKnownHosts
		/// <summary>
		/// A user info for getting user data
		/// </summary>
		public class UserInfoKnownHosts : UserInfo
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
			/// </summary>
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