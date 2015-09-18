using System;
using SharpSsh;

namespace SharpSshTest.sharpssh_samples
{
	/// <summary>
	/// Sample showing the use of the SSH file trasfer features of 
	/// SharpSSH such as SFTP and SCP
	/// </summary>
	public class SshFileTransferTest
	{
		private static ConsoleProgressBar progressBar;

		public static void RunExampleDownload()
		{
			try
			{
				SshTransferProtocolBase sshCp = new Sftp("host", "login", "password");
				Console.Write("Connecting...");
				sshCp.Connect();
				Console.WriteLine("OK");

				sshCp.Get("remote_file", "local_file");

				Console.Write("Disconnecting...");
				sshCp.Close();
				Console.WriteLine("OK");
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		public static void RunExample()
		{
			try
			{
				SshConnectionInfo input = Util.GetInput();
				string proto = GetProtocol();
				SshTransferProtocolBase sshCp;

				if (proto.Equals("scp"))
					sshCp = new Scp(input.Host, input.User);
				else
					sshCp = new Sftp(input.Host, input.User);

				if (input.Pass != null)
					sshCp.Password = input.Pass;
				if (input.IdentityFile != null)
					sshCp.AddIdentityFile(input.IdentityFile);

				Console.Write("Connecting...");
				sshCp.Connect();
				Console.WriteLine("OK");

				while (true)
				{
					string direction = GetTransferDirection();
					if (direction.Equals("to"))
					{
						string lfile = GetArg("Enter local file ['Enter to cancel']");
						if (lfile == "")
							break;
						string rfile = GetArg("Enter remote file ['Enter to cancel']");
						if (rfile == "")
							break;
						sshCp.Put(lfile, rfile);
					}
					else
					{
						string rfile = GetArg("Enter remote file ['Enter to cancel']");
						if (rfile == "")
							break;
						string lpath = GetArg("Enter local path ['Enter to cancel']");
						if (lpath == "")
							break;
						sshCp.Get(rfile, lpath);
					}
				}

				Console.Write("Disconnecting...");
				sshCp.Close();
				Console.WriteLine("OK");
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		public static string GetProtocol()
		{
			string proto = "";
			while (true)
			{
				Console.Write("Enter SSH transfer protocol [SCP|SFTP]: ");
				proto = Console.ReadLine();
				if (proto.ToLower().Equals(""))
					break;
				if (proto.ToLower().Equals("scp") || proto.ToLower().Equals("sftp"))
					break;
				Console.Write("Bad input, ");
			}
			return proto;
		}

		public static string GetTransferDirection()
		{
			string dir = "";
			while (true)
			{
				Console.Write("Enter transfer direction [To|From]: ");
				dir = Console.ReadLine();
				if (dir.ToLower().Equals(""))
					break;
				if (dir.ToLower().Equals("to") || dir.ToLower().Equals("from"))
					break;
				Console.Write("Bad input, ");
			}
			return dir;
		}

		public static string GetArg(string msg)
		{
			Console.Write(msg + ": ");
			return Console.ReadLine();
		}

		private static void sshCp_OnTransferStart(string src, string dst, int transferredBytes, int totalBytes, string message)
		{
			Console.WriteLine();
			progressBar = new ConsoleProgressBar();
			progressBar.Update(transferredBytes, totalBytes, message);
		}

		private static void sshCp_OnTransferProgress(string src, string dst, int transferredBytes, int totalBytes, string message)
		{
			if (progressBar != null)
				progressBar.Update(transferredBytes, totalBytes, message);
		}

		private static void sshCp_OnTransferEnd(string src, string dst, int transferredBytes, int totalBytes, string message)
		{
			if (progressBar != null)
			{
				progressBar.Update(transferredBytes, totalBytes, message);
				progressBar = null;
			}
		}
	}
}
