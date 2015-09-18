using System;
using SharpSsh.jsch;

/* KeyGen.cs
 * ====================================================================
 * The following example was posted with the original JSch java library,
 * and is translated to C# to show the usage of SharpSSH JSch API
 * ====================================================================
 * */
namespace SharpSshTest.jsch_samples
{
	/// <summary>
	/// This progam will demonstrate the RSA/DSA keypair generation. 
	/// You will be asked a passphrase for output_keyfile.
	/// If everything works fine, you will get the DSA or RSA keypair, 
	/// output_keyfile and output_keyfile+".pub".
	/// The private key and public key are in the OpenSSH format.
	/// </summary>
	public class KeyGen
	{
		public static void RunExample(params string[] arg)
		{
			if (arg.Length < 3)
			{
				Console.Error.WriteLine(
					"usage: java KeyGen rsa output_keyfile comment\n" +
					"       java KeyGen dsa  output_keyfile comment");
				return;
			}

			try
			{
				// Get sig type ('rsa' or 'dsa')
				string _type = arg[0];
				int type = 0;
				if (_type.Equals("rsa"))
					type = KeyPair.RSA;
				else if (_type.Equals("dsa"))
					type = KeyPair.DSA;
				else
				{
					Console.Error.WriteLine(
						"usage: java KeyGen rsa output_keyfile comment\n" +
						"       java KeyGen dsa  output_keyfile comment");
					return;
				}
				// Output file name
				string filename = arg[1];
				// Signature comment
				string comment = arg[2];

				// Create a new JSch instance
				JSch jsch = new JSch();

				//Prompt the user for a passphrase for the private key file
				string passphrase = InputForm.GetUserInput("Enter passphrase (empty for no passphrase)", true);


				// Generate the new key pair
				KeyPair key_pair = KeyPair.genKeyPair(jsch, type);
				// Set a passphrase
				key_pair.setPassphrase(passphrase);
				// Write the private key to "filename"
				key_pair.writePrivateKey(filename);
				// Write the public key to "filename.pub"
				key_pair.writePublicKey(filename + ".pub", comment);
				// Print the key fingerprint
				Console.WriteLine("Finger print: " + key_pair.getFingerPrint());
				// Free resources
				key_pair.dispose();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
			return;
		}
	}
}
