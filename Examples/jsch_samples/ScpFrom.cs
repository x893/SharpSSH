using System;
using SharpSsh.jsch;
using System.IO;
using System.Text;

/* ScpFrom.cs
 * ====================================================================
 * The following example was posted with the original JSch java library,
 * and is translated to C# to show the usage of SharpSSH JSch API
 * ====================================================================
 * */
namespace SharpSshTest.jsch_samples
{
	/// <summary>
	/// This program will demonstrate the file transfer from remote to local
	/// You will be asked passwd. 
	/// If everything works fine, a file 'file1' on 'remotehost' will copied to
	/// local 'file1'.
	/// </summary>
	public class ScpFrom
	{
		public static void RunExample(string[] arg)
		{
			if (arg.Length != 2)
			{
				Console.WriteLine("usage: java ScpFrom user@remotehost:file1 file2");
				return;
			}

			try
			{
				string user = arg[0].Substring(0, arg[0].IndexOf('@'));
				arg[0] = arg[0].Substring(arg[0].IndexOf('@') + 1);
				string host = arg[0].Substring(0, arg[0].IndexOf(':'));
				string rfile = arg[0].Substring(arg[0].IndexOf(':') + 1);
				string lfile = arg[1];

				string prefix = null;
				if (Directory.Exists(lfile))
					prefix = lfile + Path.DirectorySeparatorChar;

				JSch jsch = new JSch();
				Session session = jsch.getSession(user, host, SharpSsh.SshBase.SSH_TCP_PORT);

				// username and password will be given via UserInfo interface.
				UserInfo ui = new UserInfoScpFrom();
				session.setUserInfo(ui);
				session.Connect();

				// exec 'scp -f rfile' remotely
				string command = "scp -f " + rfile;
				Channel channel = session.openChannel("exec");
				((ChannelExec)channel).setCommand(command);

				// get I/O streams for remote scp
				Stream outs = channel.getOutputStream();
				Stream ins = channel.getInputStream();

				channel.connect();

				byte[] buf = new byte[1024];

				// send '\0'
				buf[0] = 0; outs.Write(buf, 0, 1); outs.Flush();

				while (true)
				{
					int c = checkAck(ins);
					if (c != 'C')
						break;

					// read '0644 '
					ins.Read(buf, 0, 5);

					int filesize = 0;
					while (true)
					{
						ins.Read(buf, 0, 1);
						if (buf[0] == ' ')
							break;
						filesize = filesize * 10 + (buf[0] - '0');
					}

					string file = null;
					for (int i = 0; ; i++)
					{
						ins.Read(buf, i, 1);
						if (buf[i] == (byte)0x0a)
						{
							file = Util.getString(buf, 0, i);
							break;
						}
					}

					//Console.WriteLine("filesize="+filesize+", file="+file);

					// send '\0'
					buf[0] = 0; outs.Write(buf, 0, 1); outs.Flush();

					// read a content of lfile
					FileStream fos = File.OpenWrite(prefix == null ? lfile : prefix + file);
					int foo;
					while (true)
					{
						if (buf.Length < filesize)
							foo = buf.Length;
						else
							foo = filesize;
						ins.Read(buf, 0, foo);
						fos.Write(buf, 0, foo);
						filesize -= foo;
						if (filesize == 0)
							break;
					}
					fos.Close();

					byte[] tmp = new byte[1];

					if (checkAck(ins) != 0)
						Environment.Exit(0);

					// send '\0'
					buf[0] = 0; outs.Write(buf, 0, 1); outs.Flush();
				}
				Environment.Exit(0);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		static int checkAck(Stream ins)
		{
			int b = ins.ReadByte();
			// b may be 0 for success,
			//          1 for error,
			//          2 for fatal error,
			//          -1
			if (b == 0) return b;
			if (b == -1) return b;

			if (b == 1 || b == 2)
			{
				StringBuilder sb = new StringBuilder();
				int c;
				do
				{
					c = ins.ReadByte();
					sb.Append((char)c);
				} while (c != '\n');

				if (b == 1) // error
					Console.Write(sb.ToString());
				if (b == 2) // fatal error
					Console.Write(sb.ToString());
			}
			return b;
		}

		#region UserInfoScpFrom
		/// <summary>
		/// A user info for getting user data
		/// </summary>
		public class UserInfoScpFrom : UserInfo
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
		}
		#endregion
	}
}
