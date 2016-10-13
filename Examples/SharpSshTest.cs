using System;
using SharpSsh;
using SharpSsh.jsch;
using System.Threading;
using System.Collections;
using System.IO;

namespace SharpSshTest
{
	/// <summary>
	/// Summary description for sharpSshTest.
	/// </summary>
	public class sharpSshTest
	{
		enum COMMANDS
		{
			JSCH_SHELL,
			JSCH_AES,
			JSCH_PUBKEY,
			JSCH_SFTP,
			JSCH_KEYEGEN,
			JSCH_KNOWN_HOSTS,
			JSCH_CHANGE_PASS,
			JSCH_PORT_FWD_L,
			JSCH_PORT_FWD_R,
			JSCH_STREAM_FWD,
			JSCH_STREAM_SUBSYSTEM,
			JSCH_VIA_HTTP,
			SHARP_SHELL,
			SHARP_EXPECT,
			SHARP_EXEC,
			SHARP_TRANSFER,
			SHARP_DOWNLOAD,
			BAD,
			EXIT,
			UNKNOWN
		};

		public static void Main()
		{
			COMMANDS cmd = COMMANDS.UNKNOWN;
			while (cmd != COMMANDS.EXIT)
			{
				if (cmd != COMMANDS.BAD)
				{
					PrintVersoin();

					Console.WriteLine();
					Console.WriteLine("JSch Smaples:");
					Console.WriteLine("=============");
					Console.WriteLine("{0})\tShell.cs", COMMANDS.JSCH_SHELL);
					Console.WriteLine("{0})\tAES.cs", COMMANDS.JSCH_AES);
					Console.WriteLine("{0})\tUserAuthPublicKey.cs", COMMANDS.JSCH_PUBKEY);
					Console.WriteLine("{0})\tSftp.cs", COMMANDS.JSCH_SFTP);
					Console.WriteLine("{0})\tKeyGen.cs", COMMANDS.JSCH_KEYEGEN);
					Console.WriteLine("{0})\tKnownHosts.cs", COMMANDS.JSCH_KNOWN_HOSTS);
					Console.WriteLine("{0})\tChangePassphrase.cs", COMMANDS.JSCH_CHANGE_PASS);
					Console.WriteLine("{0})\tPortForwardingL.cs", COMMANDS.JSCH_PORT_FWD_L);
					Console.WriteLine("{0})\tPortForwardingR.cs", COMMANDS.JSCH_PORT_FWD_R);
					Console.WriteLine("{0})\tStreamForwarding.cs", COMMANDS.JSCH_STREAM_FWD);
					Console.WriteLine("{0})\tSubsystem.cs", COMMANDS.JSCH_STREAM_SUBSYSTEM);
					Console.WriteLine("{0})\tViaHTTP.cs", COMMANDS.JSCH_VIA_HTTP);
					Console.WriteLine();
					Console.WriteLine("SharpSSH Smaples:");
					Console.WriteLine("=================");
					Console.WriteLine("{0})\tSSH Shell sample", COMMANDS.SHARP_SHELL);
					Console.WriteLine("{0})\tSSH Expect Sample", COMMANDS.SHARP_EXPECT);
					Console.WriteLine("{0})\tSSH Exec Sample", COMMANDS.SHARP_EXEC);
					Console.WriteLine("{0})\tSSH File Transfer", COMMANDS.SHARP_TRANSFER);
					Console.WriteLine("{0})\tSSH File Download", COMMANDS.SHARP_DOWNLOAD);
					Console.WriteLine("{0})\tExit", COMMANDS.EXIT);
					Console.WriteLine();
				}

				cmd = COMMANDS.UNKNOWN;
				Console.Write("Please enter your choice: ");
				try
				{
					string input = Console.ReadLine();
					if (input == "")
						break;
					cmd = (COMMANDS)Enum.Parse(typeof(COMMANDS), input);
					Console.WriteLine();
				}
				catch
				{
					cmd = COMMANDS.UNKNOWN;
				}

				switch (cmd)
				{
					// JSch samples:
					case COMMANDS.JSCH_SHELL:
						jsch_samples.Shell.RunExample(null);
						break;
					case COMMANDS.JSCH_AES:
						jsch_samples.AES.RunExample(null); ;
						break;
					case COMMANDS.JSCH_PUBKEY:
						jsch_samples.UserAuthPubKey.RunExample(null);
						break;
					case COMMANDS.JSCH_SFTP:
						jsch_samples.Sftp.RunExample(null);
						break;
					case COMMANDS.JSCH_KEYEGEN:
						jsch_samples.KeyGen.RunExample(
							GetArgs(new string[] {
								"Sig Type [rsa|dsa]",
								"output_keyfile",
								"comment"
							}));
						break;
					case COMMANDS.JSCH_KNOWN_HOSTS:
						jsch_samples.KnownHosts.RunExample(null);
						break;
					case COMMANDS.JSCH_CHANGE_PASS:
						jsch_samples.ChangePassphrase.RunExample(null);
						break;
					case COMMANDS.JSCH_PORT_FWD_L:
						jsch_samples.PortForwardingL.RunExample(null);
						break;
					case COMMANDS.JSCH_PORT_FWD_R:
						jsch_samples.PortForwardingR.RunExample(null);
						break;
					case COMMANDS.JSCH_STREAM_FWD:
						jsch_samples.StreamForwarding.RunExample(null);
						break;
					case COMMANDS.JSCH_STREAM_SUBSYSTEM:
						jsch_samples.Subsystem.RunExample(null);
						break;
					case COMMANDS.JSCH_VIA_HTTP:
						jsch_samples.ViaHTTP.RunExample(null);
						break;

					// SharpSSH samples:
					case COMMANDS.SHARP_SHELL:
						sharpssh_samples.SshShellTest.RunExample();
						break;
					case COMMANDS.SHARP_EXPECT:
						sharpssh_samples.SshExpectTest.RunExample();
						break;
					case COMMANDS.SHARP_EXEC:
						sharpssh_samples.SshExeTest.RunExample();
						break;
					case COMMANDS.SHARP_TRANSFER:
						sharpssh_samples.SshFileTransferTest.RunExample();
						break;
					case COMMANDS.SHARP_DOWNLOAD:
						sharpssh_samples.SshFileTransferTest.RunExampleDownload();
						break;
					case COMMANDS.EXIT:
						break;
					default:
						Console.Write("Bad input, ");
						cmd = COMMANDS.BAD;
						break;
				}
			}
		}

		public static string[] GetArgs(string[] args)
		{
			for (int i = 0; i < args.Length; i++)
			{
				Console.Write("Enter {0}: ", args[i]);
				args[i] = Console.ReadLine();
			}
			return args;
		}

		private static void PrintVersoin()
		{
			try
			{
				System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(typeof(SharpSsh.SshBase));
				Console.WriteLine("SharpSSH-" + asm.GetName().Version);
			}
			catch
			{
				Console.WriteLine("SharpSsh v1.0");
			}
		}
	}
}
