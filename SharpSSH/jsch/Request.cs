using System;

namespace SharpSsh.jsch
{
	internal interface Request
	{
		bool waitForReply();
		void request(Session session, Channel channel);
	}
}