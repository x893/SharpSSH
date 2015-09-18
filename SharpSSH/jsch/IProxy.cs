using SharpSsh.java;
using System.IO;
using SharpSsh.java.net;

namespace SharpSsh.jsch
{
	public interface IProxy
	{
		void connect(SocketFactory socket_factory, string host, int port, int timeout);
		Stream InputStream { get; }
		Stream OutputStream { get; }
		Socket Socket { get; }
		void close();
	}
}
