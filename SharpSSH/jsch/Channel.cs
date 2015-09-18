using System;
using System.Net;
using System.IO;
using SharpSsh.Streams;
using System.Runtime.CompilerServices;
using SharpSsh.java.lang;
using Str = SharpSsh.java.StringEx;
using System.Collections;
using System.Collections.Generic;

namespace SharpSsh.jsch
{
	public abstract class Channel : SharpSsh.java.lang.IRunnable
	{
		internal static int m_index = 0;
		private static List<Channel> m_pool = new List<Channel>();

		protected int m_id;

		protected int m_recipient = -1;
		protected byte[] m_type = Str.getBytes("foo");
		protected int m_lwsize_max = 0x100000;
		protected int m_lwsize = 0x100000;   // local initial window size
		protected int m_lmpsize = 0x4000;    // local maximum packet size
		protected int m_rwsize = 0;          // remote initial window size
		protected int m_rmpsize = 0;         // remote maximum packet size
		protected IO m_io = null;

		protected Thread m_thread = null;
		protected bool m_eof_local = false;
		protected bool m_eof_remote = false;
		protected bool m_close = false;
		protected bool m_connected = false;
		protected int m_exitstatus = -1;
		protected int m_reply = 0;
		protected Session m_session;

		#region Properties
		public bool IsEofRemote
		{
			get { return m_eof_remote; }
			set { m_eof_remote = value; }
		}
		public virtual bool IsClosed
		{
			get { return m_close; }
			set { m_close = value; }
		}
		internal virtual int LocalWindowSizeMax
		{
			get { return m_lwsize_max; }
			set { m_lwsize_max = value; }
		}

		internal virtual int LocalPacketSize
		{
			get { return m_lmpsize; }
			set { m_lmpsize = value; }
		}
		internal virtual int LocalWindowSize
		{
			get { return m_lwsize; }
			set { m_lwsize = value; }
		}

		internal virtual int RemoteWindowSize
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			get
			{ return m_rwsize; }
			[MethodImpl(MethodImplOptions.Synchronized)]
			set
			{ m_rwsize = value; }
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		internal virtual void addRemoteWindowSize(int foo) { m_rwsize += foo; }

		internal virtual int RemotePacketSize
		{
			get { return m_rmpsize; }
			set { m_rmpsize = value; }
		}

		internal virtual Session Session
		{
			get { return m_session; }
			set { m_session = value; }
		}
		public int Replay
		{
			get { return m_reply; }
			set { m_reply = value; }
		}
		public virtual int ExitStatus
		{
			get { return m_exitstatus; }
			set { m_exitstatus = value; }
		}
		public int Id { get { return m_id; } }
		public IO IO { get { return m_io; } }
		public bool IsEOF { get { return m_eof_remote; } }
		internal virtual int Recipient
		{
			set { m_recipient = value; }
			get { return m_recipient; }
		}
		#endregion

		internal static Channel getChannel(string type)
		{
			if (type.Equals("session"))
				return new ChannelSession();
			if (type.Equals("shell"))
				return new ChannelShell();
			if (type.Equals("exec"))
				return new ChannelExec();
			if (type.Equals("x11"))
				return new ChannelX11();
			if (type.Equals("direct-tcpip"))
				return new ChannelDirectTCPIP();
			if (type.Equals("forwarded-tcpip"))
				return new ChannelForwardedTCPIP();
			if (type.Equals("subsystem"))
				return new ChannelSubsystem();
			if (type.Equals("sftp"))
				return new ChannelSftp();
			return null;
		}

		internal static Channel FindChannel(int id, Session session)
		{
			lock (m_pool)
			{
				for (int i = 0; i < m_pool.Count; i++)
				{
					Channel channel = m_pool[i];
					if (channel.m_id == id && channel.m_session == session)
						return channel;
				}
			}
			return null;
		}

		internal static void Remove(Channel channel)
		{
			lock (m_pool)
			{
				m_pool.Remove(channel);
			}
		}

		internal Channel()
		{
			lock (m_pool)
			{
				m_id = m_index++;
				m_pool.Add(this);
			}
		}

		public virtual void Init()
		{
		}

		public virtual void connect()
		{
			if (!m_session.IsConnected())
				throw new JSchException("session is down");
			try
			{
				Buffer buf = new Buffer(100);
				Packet packet = new Packet(buf);

				packet.reset();
				buf.putByte((byte)90);
				buf.putString(m_type);
				buf.putInt(m_id);
				buf.putInt(m_lwsize);
				buf.putInt(m_lmpsize);
				m_session.write(packet);

				int retry = 1000;
				while (Recipient == -1
					&& m_session.IsConnected()
					&& retry > 0
					)
				{
					try
					{
						Thread.sleep(50);
					}
					catch (Exception) { }
					retry--;
				}

				if (!m_session.IsConnected())
					throw new JSchException("session is down");
				if (retry == 0)
					throw new JSchException("channel is not opened.");
				m_connected = true;
				start();
			}
			catch (Exception e)
			{
				m_connected = false;
				if (e is JSchException)
					throw (JSchException)e;
			}
		}

		public virtual void setXForwarding(bool foo)
		{
		}

		public virtual void start() { }

		internal virtual void getData(Buffer buf)
		{
			Recipient = buf.getInt();
			RemoteWindowSize = buf.getInt();
			RemotePacketSize = buf.getInt();
		}

		public virtual void setInputStream(Stream In)
		{
			m_io.setInputStream(In, false);
		}
		public virtual void setInputStream(Stream In, bool dontclose)
		{
			m_io.setInputStream(In, dontclose);
		}
		public virtual void setOutputStream(Stream Out)
		{
			m_io.setOutputStream(Out, false);
		}
		public virtual void setOutputStream(Stream Out, bool dontclose)
		{
			m_io.setOutputStream(Out, dontclose);
		}
		public virtual void setExtOutputStream(Stream Out)
		{
			m_io.setExtOutputStream(Out, false);
		}
		public virtual void setExtOutputStream(Stream Out, bool dontclose)
		{
			m_io.setExtOutputStream(Out, dontclose);
		}

		public virtual java.io.InputStream getInputStream()
		{
			PipedInputStream stream =
				new ChannelPipedInputStream(
				32 * 1024  // this value should be customizable.
				);
			m_io.setOutputStream(new PassiveOutputStream(stream), false);
			return stream;
		}

		public virtual java.io.InputStream getExtInputStream()
		{
			PipedInputStream stream =
				new ChannelPipedInputStream(
				32 * 1024  // this value should be customizable.
				);
			m_io.setExtOutputStream(new PassiveOutputStream(stream), false);
			return stream;
		}
		public virtual Stream getOutputStream()
		{
			PipedOutputStream stream = new PipedOutputStream();
			m_io.setInputStream(new PassiveInputStream(stream
				, 32 * 1024
				), false);
			return stream;
		}

		internal class ChannelPipedInputStream : PipedInputStream
		{
			internal ChannelPipedInputStream() : base() { ; }
			internal ChannelPipedInputStream(int size)
				: base()
			{
				m_buffer = new byte[size];
			}

			internal ChannelPipedInputStream(PipedOutputStream Out) : base(Out) { }
			internal ChannelPipedInputStream(PipedOutputStream Out, int size)
				: base(Out)
			{
				m_buffer = new byte[size];
			}
		}

		public virtual void Run()
		{
		}

		internal virtual void write(byte[] foo)
		{
			write(foo, 0, foo.Length);
		}
		internal virtual void write(byte[] foo, int s, int l)
		{
			try
			{
				//    if(io.outs!=null)
				m_io.put(foo, s, l);
			}
			catch (NullReferenceException)
			{ }
		}
		internal virtual void write_ext(byte[] foo, int s, int l)
		{
			try
			{
				//    if(io.out_ext!=null)
				m_io.put_ext(foo, s, l);
			}
			catch (NullReferenceException)
			{ }
		}

		internal virtual void eof_remote()
		{
			m_eof_remote = true;
			try
			{
				if (m_io.m_outs != null)
				{
					m_io.m_outs.Close();
					m_io.m_outs = null;
				}
			}
			catch (NullReferenceException)
			{ }
			catch (IOException)
			{ }
		}

		internal virtual void eof()
		{
			// Thread.dumpStack();
			if (m_close || m_eof_local)
				return;
			m_eof_local = true;

			try
			{
				Buffer buf = new Buffer(100);
				Packet packet = new Packet(buf);
				packet.reset();
				buf.putByte((byte)Session.SSH_MSG_CHANNEL_EOF);
				buf.putInt(Recipient);
				m_session.write(packet);
			}
			catch (Exception)
			{ }
		}

		/*
		http://www1.ietf.org/internet-drafts/draft-ietf-secsh-connect-24.txt

	  5.3  Closing a Channel
		When a party will no longer send more data to a channel, it SHOULD
		 send SSH_MSG_CHANNEL_EOF.

				  byte      SSH_MSG_CHANNEL_EOF
				  uint32    recipient_channel

		No explicit response is sent to this message.  However, the
		 application may send EOF to whatever is at the other end of the
		channel.  Note that the channel remains open after this message, and
		 more data may still be sent In the other direction.  This message
		 does not consume window space and can be sent even if no window space
		 is available.

		   When either party wishes to terminate the channel, it sends
		   SSH_MSG_CHANNEL_CLOSE.  Upon receiving this message, a party MUST
		 send back a SSH_MSG_CHANNEL_CLOSE unless it has already sent this
		 message for the channel.  The channel is considered closed for a
		   party when it has both sent and received SSH_MSG_CHANNEL_CLOSE, and
		 the party may then reuse the channel number.  A party MAY send
		 SSH_MSG_CHANNEL_CLOSE without having sent or received
		 SSH_MSG_CHANNEL_EOF.

				  byte      SSH_MSG_CHANNEL_CLOSE
				  uint32    recipient_channel

		 This message does not consume window space and can be sent even if no
		 window space is available.

		 It is recommended that any data sent before this message is delivered
		   to the actual destination, if possible.
		*/

		internal virtual void close()
		{
			if (m_close) return;
			m_close = true;
			try
			{
				Buffer buf = new Buffer(100);
				Packet packet = new Packet(buf);
				packet.reset();
				buf.putByte((byte)Session.SSH_MSG_CHANNEL_CLOSE);
				buf.putInt(Recipient);
				m_session.write(packet);
			}
			catch (Exception)
			{ }
		}

		internal static void disconnect(Session session)
		{
			Channel[] channels = null;
			int count = 0;
			lock (m_pool)
			{
				channels = new Channel[m_pool.Count];
				for (int i = 0; i < m_pool.Count; i++)
				{
					try
					{
						Channel c = ((Channel)(m_pool[i]));
						if (c.m_session == session)
							channels[count++] = c;
					}
					catch (Exception) { }
				}
			}
			for (int i = 0; i < count; i++)
				channels[i].disconnect();
		}

		public virtual void disconnect()
		{
			if (!m_connected)
				return;

			m_connected = false;
			close();
			m_eof_remote = m_eof_local = true;
			m_thread = null;

			try
			{
				if (m_io != null)
					m_io.close();
			}
			catch (Exception) { }

			m_io = null;
			Channel.Remove(this);
		}

		public virtual bool isConnected()
		{
			if (m_session != null)
				return m_session.IsConnected() && m_connected;
			return false;
		}

		public virtual void sendSignal(string foo)
		{
			RequestSignal request = new RequestSignal();
			request.setSignal(foo);
			request.request(m_session, this);
		}

		internal class PassiveInputStream : ChannelPipedInputStream
		{
			internal PipedOutputStream m_Out;
			internal PassiveInputStream(PipedOutputStream Out, int size)
				: base(Out, size)
			{
				m_Out = Out;
			}
			internal PassiveInputStream(PipedOutputStream Out)
				: base(Out)
			{
				m_Out = Out;
			}
			public override void close()
			{
				if (m_Out != null)
					m_Out.close();
				m_Out = null;
			}
		}

		internal class PassiveOutputStream : PipedOutputStream
		{
			internal PassiveOutputStream(PipedInputStream In)
				: base(In)
			{
			}
		}
	}
}
