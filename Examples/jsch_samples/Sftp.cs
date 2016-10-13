using System;
using System.IO;
using System.Collections;
using System.Windows.Forms;
using SharpSsh.jsch;
using SharpSsh.java.util;
using System.Collections.Generic;

/* Sftp.cs
 * ====================================================================
 * The following example was posted with the original JSch java library,
 * and is translated to C# to show the usage of SharpSSH JSch API
 * ====================================================================
 * */
namespace SharpSshTest.jsch_samples
{
	/// <summary>
	/// This program will demonstrate the sftp protocol support.
	/// You will be asked username, host and passwd. 
	/// If everything works fine, you will get a prompt 'sftp>'. 
	/// 'help' command will show available command.
	/// In current implementation, the destination path for 'get' and 'put'
	/// commands must be a file, not a directory.
	/// </summary>
	public class Sftp
	{
		private const string help = @"
      Available commands:
      * means unimplemented command.
cd path                       Change remote directory to 'path'
lcd path                      Change local directory to 'path'
chgrp grp path                Change group of file 'path' to 'grp'
chmod mode path               Change permissions of file 'path' to 'mode'
chown own path                Change owner of file 'path' to 'own'
help                          Display this help text
get remote-path [local-path]  Download file
get-resume remote-path [local-path]  Resume to download file.
get-append remote-path [local-path]  Append remote file to local file
*lls [ls-options [path]]      Display local directory listing
ln oldpath newpath            Symlink remote file
*lmkdir path                  Create local directory
lpwd                          Print local working directory
ls [path]                     Display remote directory listing
*lumask umask                 Set local umask to 'umask'
mkdir path                    Create remote directory
put local-path [remote-path]  Upload file
put-resume local-path [remote-path]  Resume to upload file
put-append local-path [remote-path]  Append local file to remote file.
pwd                           Display remote working directory
stat path                     Display info about path
exit                          Quit sftp
quit                          Quit sftp
rename oldpath newpath        Rename remote file
rmdir path                    Remove remote directory
rm path                       Delete remote file
symlink oldpath newpath       Symlink remote file
rekey                         Key re-exchanging
compression level             Packet compression will be enabled
version                       Show SFTP version
?                             Synonym for help";

		public static void RunExample(string[] arg)
		{
			try
			{
				JSch jsch = new JSch();

				InputForm inForm = new InputForm();
				inForm.Text = "Enter username@hostname";
				string host = null, user;
				while(true)
				{
					inForm.SetText("");

					if (!inForm.PromptForInput())
					{
						Console.WriteLine("Cancelled");
						return;
					}
					user = inForm.GetText();
					if (!string.IsNullOrEmpty(user) && user.IndexOf('@') >= 0 && user.IndexOf('@') < user.Length - 1)
					{
						host = user.Substring(user.IndexOf('@') + 1);
						break;
					}
				}

				Session session = jsch.getSession(user, host, SharpSsh.SshBase.SSH_TCP_PORT);

				// username and password will be given via UserInfo interface.
				UserInfo ui = new UserInfoSftp();
				session.setUserInfo(ui);

				session.Connect();

				Channel channel = session.openChannel("sftp");
				channel.connect();
				ChannelSftp c = (ChannelSftp)channel;

				Stream ins = Console.OpenStandardInput();
				TextWriter outs = Console.Out;

				List<string> cmds = new List<string>();
				byte[] buf = new byte[1024];
				int i;
				string str;
				int level = 0;

				while (true)
				{
					outs.Write("sftp> ");
					cmds.Clear();
					i = ins.Read(buf, 0, 1024);
					if (i <= 0)
						break;

					i--;
					if (i > 0 && buf[i - 1] == 0x0d)
						i--;
					int s = 0;
					for (int ii = 0; ii < i; ii++)
					{
						if (buf[ii] == ' ')
						{
							if (ii - s > 0)
								cmds.Add(Util.getString(buf, s, ii - s));
							while (ii < i)
							{
								if (buf[ii] != ' ')
									break;
								ii++;
							}
							s = ii;
						}
					}

					if (s < i)
						cmds.Add(Util.getString(buf, s, i - s));

					if (cmds.Count == 0)
						continue;

					string cmd = cmds[0];
					if (cmd.Equals("quit"))
					{
						c.quit();
						break;
					}

					if (cmd.Equals("exit"))
					{
						c.exit();
						break;
					}

					if (cmd.Equals("rekey"))
					{
						session.rekey();
						continue;
					}

					if (cmd.Equals("compression"))
					{
						if (cmds.Count < 2)
						{
							outs.WriteLine("compression level: " + level);
							continue;
						}
						try
						{
							level = int.Parse((String)cmds[1]);
							StringDictionary config = new StringDictionary(2);
							if (level == 0)
							{
								config.Add("compression.s2c", "none");
								config.Add("compression.c2s", "none");
							}
							else
							{
								config.Add("compression.s2c", "zlib,none");
								config.Add("compression.c2s", "zlib,none");
							}
							session.setConfig(config);
						}
						catch { }
						continue;
					}
					if (cmd.Equals("cd") || cmd.Equals("lcd"))
					{
						if (cmds.Count < 2) continue;
						string path = (String)cmds[1];
						try
						{
							if (cmd.Equals("cd")) c.cd(path);
							else c.lcd(path);
						}
						catch (SftpException e)
						{
							Console.WriteLine(e.ToString());
						}
						continue;
					}
					if (cmd.Equals("rm") || cmd.Equals("rmdir") || cmd.Equals("mkdir"))
					{
						if (cmds.Count < 2) continue;
						string path = (String)cmds[1];
						try
						{
							if (cmd.Equals("rm")) c.rm(path);
							else if (cmd.Equals("rmdir")) c.rmdir(path);
							else c.mkdir(path);
						}
						catch (SftpException e)
						{
							Console.WriteLine(e.ToString());
						}
						continue;
					}
					if (cmd.Equals("chgrp") || cmd.Equals("chown") || cmd.Equals("chmod"))
					{
						if (cmds.Count != 3) continue;
						string path = (String)cmds[2];
						int foo = 0;
						if (cmd.Equals("chmod"))
						{
							byte[] bar = Util.getBytes((String)cmds[1]);
							int k;
							for (int j = 0; j < bar.Length; j++)
							{
								k = bar[j];
								if (k < '0' || k > '7') { foo = -1; break; }
								foo <<= 3;
								foo |= (k - '0');
							}
							if (foo == -1) continue;
						}
						else
						{
							try { foo = int.Parse((String)cmds[1]); }
							catch { }//(Exception e){continue;}
						}
						try
						{
							if (cmd.Equals("chgrp")) { c.chgrp(foo, path); }
							else if (cmd.Equals("chown")) { c.chown(foo, path); }
							else if (cmd.Equals("chmod")) { c.chmod(foo, path); }
						}
						catch (SftpException e)
						{
							Console.WriteLine(e.ToString());
						}
						continue;
					}
					if (cmd.Equals("pwd") || cmd.Equals("lpwd"))
					{
						str = (cmd.Equals("pwd") ? "Remote" : "Local");
						str += " working directory: ";
						if (cmd.Equals("pwd"))
							str += c.Pwd;
						else
							str += c.LPwd;
						outs.WriteLine(str);
						continue;
					}
					if (cmd.Equals("ls") || cmd.Equals("dir"))
					{
						string path = ".";
						if (cmds.Count == 2) path = (String)cmds[1];
						try
						{
							ArrayList vv = c.ls(path);
							if (vv != null)
							{
								for (int ii = 0; ii < vv.Count; ii++)
								{
									object obj = vv[ii];
									if (obj is ChannelSftp.LsEntry)
										outs.WriteLine(vv[ii]);
								}
							}
						}
						catch (SftpException e)
						{
							Console.WriteLine(e.ToString());
						}
						continue;
					}

					if (cmd.Equals("lls") || cmd.Equals("ldir"))
					{
						string path = ".";
						if (cmds.Count == 2) path = (String)cmds[1];
						try
						{
							if (!File.Exists(path))
							{
								outs.WriteLine(path + ": No such file or directory");
								continue;
							}
							if (Directory.Exists(path))
							{
								string[] list = Directory.GetDirectories(path);
								for (int ii = 0; ii < list.Length; ii++)
								{
									outs.WriteLine(list[ii]);
								}
								continue;
							}
							outs.WriteLine(path);
						}
						catch (Exception e)
						{
							Console.WriteLine(e);
						}
						continue;
					}

					if (cmd.Equals("get")
					|| cmd.Equals("get-resume")
					|| cmd.Equals("get-append")
					|| cmd.Equals("put")
					|| cmd.Equals("put-resume")
					|| cmd.Equals("put-append")
						)
					{
						if (cmds.Count != 2 && cmds.Count != 3)
							continue;
						string p1 = (string)cmds[1];
						string p2 = ".";
						if (cmds.Count == 3) p2 = (String)cmds[2];
						try
						{
							SftpProgressMonitor monitor = new MyProgressMonitor();
							if (cmd.StartsWith("get"))
							{
								ChannelSftp.ChannelSftpModes mode = ChannelSftp.ChannelSftpModes.OVERWRITE;
								if (cmd.Equals("get-resume"))
									mode = ChannelSftp.ChannelSftpModes.RESUME;
								else if (cmd.Equals("get-append"))
									mode = ChannelSftp.ChannelSftpModes.APPEND;
								c.get(p1, p2, monitor, mode);
							}
							else
							{
								ChannelSftp.ChannelSftpModes mode = ChannelSftp.ChannelSftpModes.OVERWRITE;
								if (cmd.Equals("put-resume"))
									mode = ChannelSftp.ChannelSftpModes.RESUME;
								else if (cmd.Equals("put-append"))
									mode = ChannelSftp.ChannelSftpModes.APPEND;
								c.put(p1, p2, monitor, mode);
							}
						}
						catch (SftpException e)
						{
							Console.WriteLine(e.ToString());
						}
						continue;
					}
					if (cmd.Equals("ln")
					|| cmd.Equals("symlink")
					|| cmd.Equals("rename")
						)
					{
						if (cmds.Count != 3)
							continue;
						string p1 = (string)cmds[1];
						string p2 = (string)cmds[2];
						try
						{
							if (cmd.Equals("rename")) c.rename(p1, p2);
							else c.symlink(p1, p2);
						}
						catch (SftpException e)
						{
							Console.WriteLine(e.ToString());
						}
						continue;
					}
					if (cmd.Equals("stat") || cmd.Equals("lstat"))
					{
						if (cmds.Count != 2) continue;
						string p1 = (String)cmds[1];
						SftpATTRS attrs = null;
						try
						{
							if (cmd.Equals("stat")) attrs = c.stat(p1);
							else attrs = c.lstat(p1);
						}
						catch (SftpException e)
						{
							Console.WriteLine(e.ToString());
						}
						if (attrs != null)
							outs.WriteLine(attrs);
						continue;
					}
					if (cmd.Equals("version"))
					{
						outs.WriteLine("SFTP protocol version " + c.Version);
						continue;
					}
					if (cmd.Equals("help") || cmd.Equals("?"))
					{
						outs.WriteLine(help);
						continue;
					}
					outs.WriteLine("unimplemented command: " + cmd);
				}
				session.disconnect();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		#region UserInfoSftp
		public class UserInfoSftp : UserInfo
		{
			private InputForm passwordField = new InputForm();

			public override bool promptYesNo(string str)
			{
				DialogResult returnVal = MessageBox.Show(str, "SharpSSH", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
				return (returnVal == DialogResult.Yes);
			}

			public override bool promptPassphrase(string message) { return true; }
			public override bool promptPassword(string message)
			{
				InputForm inForm = new InputForm();
				inForm.Text = message;
				inForm.PasswordField = true;

				if (!inForm.PromptForInput())
					return false;

				m_passwd = inForm.GetText();
				return true;
			}
			public override void showMessage(string message)
			{
				MessageBox.Show(message, "SharpSSH", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
		}
		#endregion

		public class MyProgressMonitor : SftpProgressMonitor
		{
			private ConsoleProgressBar bar;
			private long m_value = 0;
			private long m_max = 0;
			private long m_percent = -1;
			private int m_elapsed = -1;
			private System.Timers.Timer m_timer;

			public override void Init(SfrpOperation op, string src, string dest, long max)
			{
				bar = new ConsoleProgressBar();
				m_max = max;
				m_elapsed = 0;
				m_timer = new System.Timers.Timer(1000);
				m_timer.Start();
				m_timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
			}

			public override bool Count(long c)
			{
				m_value += c;
				if (m_percent >= m_value * 100 / m_max)
					return true;
				m_percent = m_value * 100 / m_max;

				bar.Update(m_value, m_max, "Transfering... [Elapsed time: " + m_elapsed + "]");
				return true;
			}

			public override void End()
			{
				m_timer.Stop();
				m_timer.Dispose();
				bar.Update(m_value, m_max, "Done in " + m_elapsed + " seconds!");
				bar = null;
			}

			private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
			{
				m_elapsed++;
			}
		}
	}
}
