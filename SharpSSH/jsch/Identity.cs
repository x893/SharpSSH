using System;

namespace SharpSsh.jsch
{
	internal interface Identity
	{
		bool setPassphrase(string passphrase);
		byte[] PublicKeyBlob { get; }
		byte[] getSignature(Session session, byte[] data);
		bool decrypt();

		string AlgName { get; }
		string Name { get; }
		bool isEncrypted { get; }
	}
}
