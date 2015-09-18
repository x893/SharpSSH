using System;
using SharpSsh.jsch;

/* ChangePassphrase.cs
 * ====================================================================
 * The following example was posted with the original JSch java library,
 * and is translated to C# to show the usage of SharpSSH JSch API
 * ====================================================================
 * */
namespace SharpSshTest.jsch_samples
{
	/// <summary>
	/// This program will demonstrate changing the passphrase for a
	/// private key file instead of creating a new private key.
	/// A passphrase will be prompted if the given private-key has been
	/// encrypted.  After successfully loading the content of the
	/// private-key, the new passphrase will be prompted and the given
	/// private-key will be re-encrypted with that new passphrase.
	/// </summary>
	public class ChangePassphrase
	{
		public static void RunExample(string[] arg)
		{
			// Get the private key filename from the user
			Console.WriteLine("Please choose your private key file...");
			string pkey = InputForm.GetFileFromUser("Choose your privatekey(ex. ~/.ssh/id_dsa)");
			Console.WriteLine("You chose " + pkey + ".");

			// Create a new JSch instance
			JSch jsch = new JSch();

			try
			{
				// Load the key pair
				KeyPair key_pair = KeyPair.load(jsch, pkey);

				// Print the key file encryption status
				Console.WriteLine(pkey + " has " + (key_pair.isEncrypted() ? "been " : "not been ") + "encrypted");

				string passphrase = "";

				while (key_pair.isEncrypted())
				{
					passphrase = InputForm.GetUserInput("Enter passphrase for " + pkey, true);
					if (!key_pair.decrypt(passphrase))
						Console.WriteLine("failed to decrypt " + pkey);
					else
						Console.WriteLine(pkey + " is decrypted.");
				}

				passphrase = "";
				passphrase = InputForm.GetUserInput("Enter new passphrase for " + pkey + " (empty for no passphrase)", true);

				// Set the new passphrase
				key_pair.setPassphrase(passphrase);
				// write the key to file
				key_pair.writePrivateKey(pkey);
				// free the resource
				key_pair.dispose();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}
	}
}