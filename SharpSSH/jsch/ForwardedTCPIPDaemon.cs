using System;
using SharpSsh.java.lang;

namespace SharpSsh.jsch
{
	public interface ForwardedTCPIPDaemon : IRunnable
	{
		void setChannel(ChannelForwardedTCPIP channel);
		void setArg(Object[] arg);
	}
}
