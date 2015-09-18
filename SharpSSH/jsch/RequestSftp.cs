using System;

namespace SharpSsh.jsch
{
	public class RequestSftp : Request
	{
		public void request(Session session, Channel channel)
		{
			Buffer buf = new Buffer();
			Packet packet = new Packet(buf);

			bool reply = waitForReply();
			if (reply)
			{
				channel.Replay = -1;
			}

			packet.reset();
			buf.putByte((byte)Session.SSH_MSG_CHANNEL_REQUEST);
			buf.putInt(channel.Recipient);
			buf.putString(Util.getBytes("subsystem"));
			buf.putByte((byte)(waitForReply() ? 1 : 0));
			buf.putString(Util.getBytes("sftp"));
			session.write(packet);

			if (reply)
			{
				while (channel.Replay == -1)
				{
					try { System.Threading.Thread.Sleep(10); }
					catch//(Exception ee)
					{
					}
				}
				if (channel.Replay == 0)
				{
					throw new JSchException("failed to send sftp request");
				}
			}
		}
		public bool waitForReply() { return true; }
	}
}
