using System;

namespace SharpSsh.jsch
{
	public class RequestWindowChange : Request
	{
		internal int m_width_columns = 80;
		internal int m_height_rows = 24;
		internal int m_width_pixels = 640;
		internal int m_height_pixels = 480;

		public void setSize(int row, int col, int wp, int hp)
		{
			m_width_columns = row;
			m_height_rows = col;
			m_width_pixels = wp;
			m_height_pixels = hp;
		}

		public void request(Session session, Channel channel)
		{
			Buffer buf = new Buffer();
			Packet packet = new Packet(buf);

			//byte      SSH_MSG_CHANNEL_REQUEST
			//uint32    recipient_channel
			//string    "window-change"
			//boolean   FALSE
			//uint32    terminal width, columns
			//uint32    terminal height, rows
			//uint32    terminal width, pixels
			//uint32    terminal height, pixels
			packet.reset();
			buf.putByte((byte)Session.SSH_MSG_CHANNEL_REQUEST);
			buf.putInt(channel.Recipient);
			buf.putString(Util.getBytes("window-change"));
			buf.putByte((byte)(waitForReply() ? 1 : 0));
			buf.putInt(m_width_columns);
			buf.putInt(m_height_rows);
			buf.putInt(m_width_pixels);
			buf.putInt(m_height_pixels);
			session.write(packet);
		}
		public bool waitForReply() { return false; }
	}
}
