using System;
using SharpSsh.jsch;
using System.IO;
using System.Windows.Forms;
using System.Text;

/* ScpTo.cs
 * ====================================================================
 * The following example was posted with the original JSch java library,
 * and is translated to C# to show the usage of SharpSSH JSch API
 * ====================================================================
 * */
namespace SharpSshTest.jsch_samples
{
	/// <summary>
	/// This program will demonstrate the file transfer from local to remote.
	/// You will be asked passwd. 
	/// If everything works fine, a local file 'file1' will copied to
	/// 'file2' on 'remotehost'.
	/// </summary>	
	public class ScpTo
	{
		public static void RunExample(string[] arg)
		{
			if (arg.Length != 2)
			{
				Console.WriteLine("usage: java ScpTo file1 user@remotehost:file2");
				Environment.Exit(-1);
			}

			try
			{
				string lfile = arg[0];
				string user = arg[1].Substring(0, arg[1].IndexOf('@'));
				arg[1] = arg[1].Substring(arg[1].IndexOf('@') + 1);

				string host = arg[1].Substring(0, arg[1].IndexOf(':'));
				string rfile = arg[1].Substring(arg[1].IndexOf(':') + 1);

				JSch jsch = new JSch();
				Session session = jsch.getSession(user, host, SharpSsh.SshBase.SSH_TCP_PORT);

				// username and password will be given via UserInfo interface.
				UserInfo ui = new UseScpTorInfo();
				session.setUserInfo(ui);
				session.Connect();

				// exec 'scp -t rfile' remotely
				string command = "scp -p -t " + rfile;
				Channel channel = session.openChannel("exec");
				((ChannelExec)channel).setCommand(command);

				// get I/O streams for remote scp
				Stream outs = channel.getOutputStream();
				Stream ins = channel.getInputStream();

				channel.connect();

				byte[] tmp = new byte[1];
				if (checkAck(ins) != 0)
					Environment.Exit(0);

				// send "C0644 filesize filename", where filename should not include '/'

				int filesize = (int)(new FileInfo(lfile)).Length;
				command = "C0644 " + filesize + " ";
				if (lfile.LastIndexOf('/') > 0)
					command += lfile.Substring(lfile.LastIndexOf('/') + 1);
				else
					command += lfile;

				command += "\n";
				byte[] buff = Util.getBytes(command);
				outs.Write(buff, 0, buff.Length);
				outs.Flush();

				if (checkAck(ins) != 0)
					Environment.Exit(0);

				// send a content of lfile
				FileStream fis = File.OpenRead(lfile);
				byte[] buf = new byte[1024];
				while (true)
				{
					int len = fis.Read(buf, 0, buf.Length);
					if (len <= 0) break;
					outs.Write(buf, 0, len); outs.Flush();
					Console.Write("#");
				}

				// send '\0'
				buf[0] = 0; outs.Write(buf, 0, 1); outs.Flush();
				Console.Write(".");

				if (checkAck(ins) != 0)
					Environment.Exit(0);

				Console.WriteLine("OK");
				Environment.Exit(0);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		private static int checkAck(Stream ins)
		{
			Console.Write(".");
			int b = ins.ReadByte();
			Console.Write(".");
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

				if (b == 1)
				{	// error
					Console.Write(sb.ToString());
				}
				if (b == 2)
				{	// fatal error
					Console.Write(sb.ToString());
				}
			}
			return b;
		}

		#region UseScpTorInfo
		public class UseScpTorInfo : UserInfo
		{
			public override bool promptYesNo(string message)
			{
				DialogResult returnVal = MessageBox.Show(message, "SharpSSH", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
				return (returnVal == DialogResult.Yes);
			}

			public override bool promptPassphrase(string message) { return true; }
			public override bool promptPassword(string message)
			{
				InputForm passwordField = new InputForm();
				passwordField.Text = message;
				passwordField.PasswordField = true;

				if (!passwordField.PromptForInput())
					return false;

				m_passwd = passwordField.GetText();
				return true;
			}
			public override void showMessage(string message)
			{
				MessageBox.Show(message, "SharpSSH", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
		}
		#endregion
	}
}
