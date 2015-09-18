using System;
using System.Runtime.CompilerServices;
using SharpSsh.Streams;
using SharpSsh.java.io;
using SharpSsh.java.lang;
using SharpSsh.java;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SharpSsh.jsch
{
	/// <summary>
	/// Based on JSch-0.1.30
	/// </summary>
	public class ChannelSftp : ChannelSession
	{
		public enum ChannelSftpModes
		{
			OVERWRITE = 0,
			RESUME = 1,
			APPEND = 2
		};

		public enum ChannelSftpResult
		{
			SSH_FX_OK = 0,
			SSH_FX_EOF = 1,
			SSH_FX_NO_SUCH_FILE = 2,
			SSH_FX_PERMISSION_DENIED = 3,
			SSH_FX_FAILURE = 4,
			SSH_FX_BAD_MESSAGE = 5,
			SSH_FX_NO_CONNECTION = 6,
			SSH_FX_CONNECTION_LOST = 7,
			SSH_FX_OP_UNSUPPORTED = 8
		};

		#region Privates

		private static byte SSH_FXP_INIT = 1;
		//!!! private static byte SSH_FXP_VERSION = 2;
		private static byte SSH_FXP_OPEN = 3;
		private static byte SSH_FXP_CLOSE = 4;
		private static byte SSH_FXP_READ = 5;
		private static byte SSH_FXP_WRITE = 6;
		private static byte SSH_FXP_LSTAT = 7;
		private static byte SSH_FXP_FSTAT = 8;
		private static byte SSH_FXP_SETSTAT = 9;
		// private static byte SSH_FXP_FSETSTAT = 10;
		private static byte SSH_FXP_OPENDIR = 11;
		private static byte SSH_FXP_READDIR = 12;
		private static byte SSH_FXP_REMOVE = 13;
		private static byte SSH_FXP_MKDIR = 14;
		private static byte SSH_FXP_RMDIR = 15;
		private static byte SSH_FXP_REALPATH = 16;
		private static byte SSH_FXP_STAT = 17;
		private static byte SSH_FXP_RENAME = 18;
		private static byte SSH_FXP_READLINK = 19;
		private static byte SSH_FXP_SYMLINK = 20;
		private static byte SSH_FXP_STATUS = 101;
		private static byte SSH_FXP_HANDLE = 102;
		private static byte SSH_FXP_DATA = 103;
		private static byte SSH_FXP_NAME = 104;
		private static byte SSH_FXP_ATTRS = 105;
		// private static byte SSH_FXP_EXTENDED = (byte)200;
		// private static byte SSH_FXP_EXTENDED_REPLY = (byte)201;

		// pflags
		private static int SSH_FXF_READ = 0x00000001;
		private static int SSH_FXF_WRITE = 0x00000002;
		// private static int SSH_FXF_APPEND = 0x00000004;
		private static int SSH_FXF_CREAT = 0x00000008;
		private static int SSH_FXF_TRUNC = 0x00000010;
		// private static int SSH_FXF_EXCL = 0x00000020;

		// private static int SSH_FILEXFER_ATTR_SIZE = 0x00000001;
		// private static int SSH_FILEXFER_ATTR_UIDGID = 0x00000002;
		// private static int SSH_FILEXFER_ATTR_PERMISSIONS = 0x00000004;
		// private static int SSH_FILEXFER_ATTR_ACMODTIME = 0x00000008;
		// private static uint SSH_FILEXFER_ATTR_EXTENDED = 0x80000000;

		private static int MAX_MSG_LENGTH = 256 * 1024;
		private static string m_file_separator = java.io.File.Separator;
		private static char m_file_separator_char = java.io.File.SeparatorChar;

		private int m_seq = 1;
		private int[] m_ackid = new int[1];
		private Buffer m_buffer;
		private Packet m_packet;

		private string m_version = "3";
		private int m_server_version = 3;

		private string m_cwd;
		private string m_home;
		private string m_lcwd;

		private List<Thread> m_threadList = null;

		#endregion

		/*
		SSH_FX_OK
			Indicates successful completion of the operation.
		SSH_FX_EOF
			indicates end-of-file condition; for SSH_FX_READ it means that no
			more data is available in the file, and for SSH_FX_READDIR it
			indicates that no more files are contained in the directory.
		SSH_FX_NO_SUCH_FILE
			is returned when a reference is made to a file which should exist
			but doesn't.
		SSH_FX_PERMISSION_DENIED
			is returned when the authenticated user does not have sufficient
			permissions to perform the operation.
		SSH_FX_FAILURE
			is a generic catch-all error message; it should be returned if an
			error occurs for which there is no more specific error code
			defined.
		SSH_FX_BAD_MESSAGE
			may be returned if a badly formatted packet or protocol
			incompatibility is detected.
		SSH_FX_NO_CONNECTION
			is a pseudo-error which indicates that the client has no
			connection to the server (it can only be generated locally by the
			client, and MUST NOT be returned by servers).
		SSH_FX_CONNECTION_LOST
			is a pseudo-error which indicates that the connection to the
			server has been lost (it can only be generated locally by the
			client, and MUST NOT be returned by servers).
		SSH_FX_OP_UNSUPPORTED
			indicates that an attempt was made to perform an operation which
			is not supported for the server (it may be generated locally by
			the client if e.g.  the version number exchange indicates that a
			required feature is not supported by the server, or it may be
			returned by the server if the server does not implement an
			operation).
		*/
		/*
		10. Changes from previous protocol versions
		The SSH File Transfer Protocol has changed over time, before it's
		standardization.  The following is a description of the incompatible
		changes between different versions.
		10.1 Changes between versions 3 and 2
		o  The SSH_FXP_READLINK and SSH_FXP_SYMLINK messages were added.
		o  The SSH_FXP_EXTENDED and SSH_FXP_EXTENDED_REPLY messages were added.
		o  The SSH_FXP_STATUS message was changed to include fields `error
		message' and `language tag'.
		10.2 Changes between versions 2 and 1
		o  The SSH_FXP_RENAME message was added.
		10.3 Changes between versions 1 and 0
		o  Implementation changes, no actual protocol changes.
		*/

		internal ChannelSftp()
		{
			m_packet = new Packet(m_buffer);
		}

		public string Pwd { get { return m_cwd; } }
		public string LPwd { get { return m_lcwd; } }
		public string Version { get { return m_version; } }
		public string Home { get { return m_home; } }


		public int Seq
		{
			get { return m_seq; }
		}

		public override void Init()
		{
		}

		public override void start()
		{
			try
			{

				PipedOutputStream pos = new PipedOutputStream();
				m_io.setOutputStream(pos);
				PipedInputStream pis = new ChannelPipedInputStream(pos, 32 * 1024);
				m_io.setInputStream(pis);

				Request request = new RequestSftp();
				request.request(m_session, this);

				m_buffer = new Buffer(m_rmpsize);
				m_packet = new Packet(m_buffer);
				int i = 0;
				int length;
				int type;
				byte[] str;

				// send SSH_FXP_INIT
				sendINIT();

				// receive SSH_FXP_VERSION
				Header _header = new Header();
				_header = fillHeader(m_buffer, _header);
				length = _header.length;
				if (length > MAX_MSG_LENGTH)
				{
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "Received message is too long: " + length);
				}
				type = _header.type;             // 2 -> SSH_FXP_VERSION
				m_server_version = _header.rid;
				skip(length);

				// send SSH_FXP_REALPATH
				sendREALPATH(Util.getBytesUTF8("."));

				// receive SSH_FXP_NAME
				_header = fillHeader(m_buffer, _header);
				length = _header.length;
				type = _header.type;            // 104 -> SSH_FXP_NAME
				m_buffer.rewind();
				fill(m_buffer.m_buffer, 0, length);
				i = m_buffer.getInt();              // count
				str = m_buffer.getString();         // filename
				m_home = m_cwd = Util.getStringUTF8(str);
				str = m_buffer.getString();         // logname
				//    SftpATTRS.getATTR(buf);      // attrs
				m_lcwd = new File(".").CanonicalPath;
			}
			catch (Exception e)
			{
				if (e is JSchException) throw (JSchException)e;
				throw new JSchException(e.ToString());
			}
		}

		public void quit() { disconnect(); }
		public void exit() { disconnect(); }
		public void lcd(string path)
		{
			path = localAbsolutePath(path);
			if ((new File(path)).IsDirectory)
			{
				try
				{
					path = (new File(path)).CanonicalPath;
				}
				catch (Exception)
				{ }
				m_lcwd = path;
				return;
			}
			throw new SftpException(ChannelSftpResult.SSH_FX_NO_SUCH_FILE, "No such directory");
		}

		/*
		cd /tmp
		c->s REALPATH
		s->c NAME
		c->s STAT
		s->c ATTR
		*/
		public void cd(string path)
		{
			try
			{
				path = remoteAbsolutePath(path);

				List<string> v = glob_remote(path);
				if (v.Count != 1)
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, v.ToString());

				path = v[0];
				sendREALPATH(Util.getBytesUTF8(path));

				Header _header = new Header();
				_header = fillHeader(m_buffer, _header);
				int length = _header.length;
				int type = _header.type;
				m_buffer.rewind();
				fill(m_buffer.m_buffer, 0, length);

				if (type != 101 && type != 104)
				{
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
				}
				int i;
				if (type == 101)
				{
					i = m_buffer.getInt();
					throwStatusError(m_buffer, i);
				}
				i = m_buffer.getInt();
				byte[] str = m_buffer.getString();
				if (str != null && str[0] != '/')
					str = Util.getBytesUTF8(m_cwd + "/" + Util.getStringUTF8(str));

				str = m_buffer.getString();         // logname
				i = m_buffer.getInt();              // attrs

				string newpwd = Util.getStringUTF8(str);
				SftpATTRS attr = execStat(newpwd);
				if ((attr.getFlags() & SftpATTRS.SSH_FILEXFER_ATTR_PERMISSIONS) == 0)
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "Can't change directory: " + path);
				if (!attr.isDir())
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "Can't change directory: " + path);
				m_cwd = newpwd;
			}
			catch (Exception e)
			{
				if (e is SftpException)
					throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}

		/*
		put foo
		c->s OPEN
		s->c HANDLE
		c->s WRITE
		s->c STATUS
		c->s CLOSE
		s->c STATUS
		*/
		public void put(string src, string dst)
		{
			put(src, dst, null, ChannelSftpModes.OVERWRITE);
		}

		public void put(string src, string dst, ChannelSftpModes mode)
		{
			put(src, dst, null, mode);
		}

		public void put(string src, string dst, SftpProgressMonitor monitor)
		{
			put(src, dst, monitor, ChannelSftpModes.OVERWRITE);
		}

		public void put(string src, string dst, SftpProgressMonitor monitor, ChannelSftpModes mode)
		{
			src = localAbsolutePath(src);
			dst = remoteAbsolutePath(dst);

			try
			{
				List<string> v = glob_remote(dst);
				int vsize = v.Count;
				if (vsize != 1)
				{
					if (vsize == 0)
					{
						if (isPattern(dst))
							throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, dst);
						else
							dst = Util.Unquote(dst);
					}
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, v.ToString());
				}
				else
					dst = v[0];

				bool _isRemoteDir = isRemoteDir(dst);

				v = glob_local(src);
				vsize = v.Count;

				StringBuilder dstsb = null;
				if (_isRemoteDir)
				{
					if (!dst.EndsWith("/"))
						dst += "/";
					dstsb = new StringBuilder(dst);
				}
				else if (vsize > 1)
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "Copying multiple files, but destination is missing or a file.");

				for (int j = 0; j < vsize; j++)
				{
					string _src = v[j];
					string _dst = null;
					if (_isRemoteDir)
					{
						int i = _src.LastIndexOf(m_file_separator_char);
						if (i == -1)
							dstsb.Append(_src);
						else
							dstsb.Append(_src.Substring(i + 1));
						_dst = dstsb.ToString();
						dstsb.Remove(dst.Length, _dst.Length - dst.Length);
					}
					else
						_dst = dst;

					long size_of_dst = 0;
					if (mode == ChannelSftpModes.RESUME)
					{
						try
						{
							SftpATTRS attr = execStat(_dst);
							size_of_dst = attr.getSize();
						}
						catch (Exception)
						{ }
						long size_of_src = new File(_src).Length;
						if (size_of_src < size_of_dst)
							throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "failed to resume for " + _dst);
						if (size_of_src == size_of_dst)
							return;
					}

					if (monitor != null)
					{
						monitor.Init(SftpProgressMonitor.SfrpOperation.PUT, _src, _dst,
									 (new File(_src)).Length);
						if (mode == ChannelSftpModes.RESUME)
							monitor.Count(size_of_dst);
					}
					FileInputStream fis = null;
					try
					{
						fis = new FileInputStream(_src);
						_put(fis, _dst, monitor, mode);
					}
					finally
					{
						if (fis != null)
							fis.close();
					}
				}
			}
			catch (Exception e)
			{
				if (e is SftpException) throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, e.ToString());
			}
		}
		public void put(InputStream src, string dst)
		{
			put(src, dst, null, ChannelSftpModes.OVERWRITE);
		}
		public void put(InputStream src, string dst, ChannelSftpModes mode)
		{
			put(src, dst, null, mode);
		}
		public void put(InputStream src, string dst, SftpProgressMonitor monitor)
		{
			put(src, dst, monitor, ChannelSftpModes.OVERWRITE);
		}
		public void put(InputStream src, string dst, SftpProgressMonitor monitor, ChannelSftpModes mode)
		{
			try
			{
				dst = remoteAbsolutePath(dst);
				List<string> v = glob_remote(dst);
				int vsize = v.Count;
				if (vsize != 1)
				{
					if (vsize == 0)
					{
						if (isPattern(dst))
							throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, dst);
						else
							dst = Util.Unquote(dst);
					}
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, v.ToString());
				}
				else
				{
					dst = v[0];
				}
				if (isRemoteDir(dst))
				{
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, dst + " is a directory");
				}
				_put(src, dst, monitor, mode);
			}
			catch (Exception e)
			{
				if (e is SftpException) throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, e.ToString());
			}
		}

		private void _put(InputStream src, string dst, SftpProgressMonitor monitor, ChannelSftpModes mode)
		{
			try
			{
				long skip = 0;
				if (mode == ChannelSftpModes.RESUME || mode == ChannelSftpModes.APPEND)
				{
					try
					{
						SftpATTRS attr = execStat(dst);
						skip = attr.getSize();
					}
					catch (Exception)
					{ }
				}
				if (mode == ChannelSftpModes.RESUME && skip > 0)
				{
					long skipped = src.Skip(skip);
					if (skipped < skip)
						throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "failed to resume for " + dst);
				}
				if (mode == ChannelSftpModes.OVERWRITE)
					sendOPENW(Util.getBytesUTF8(dst));
				else
					sendOPENA(Util.getBytesUTF8(dst));

				Header _header = new Header();
				_header = fillHeader(m_buffer, _header);
				int length = _header.length;
				int type = _header.type;
				m_buffer.rewind();
				fill(m_buffer.m_buffer, 0, length);

				if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "invalid type=" + type);

				if (type == SSH_FXP_STATUS)
				{
					int i = m_buffer.getInt();
					throwStatusError(m_buffer, i);
				}
				byte[] handle = m_buffer.getString();         // filename
				byte[] data = null;

				bool dontcopy = true;

				if (!dontcopy)
				{
					data = new byte[m_buffer.m_buffer.Length
									- (5 + 13 + 21 + handle.Length
									+ 32 + 20 // padding and mac
									)
						];
				}

				long offset = 0;
				if (mode == ChannelSftpModes.RESUME || mode == ChannelSftpModes.APPEND)
					offset += skip;

				int startid = m_seq;
				int _ackid = m_seq;
				int ackcount = 0;
				while (true)
				{
					int nread = 0;
					int s = 0;
					int datalen = 0;
					int count = 0;

					if (!dontcopy)
						datalen = data.Length - s;
					else
					{
						data = m_buffer.m_buffer;
						s = 5 + 13 + 21 + handle.Length;
						datalen = m_buffer.m_buffer.Length - s
								- 32 - 20; // padding and mac
					}

					do
					{
						nread = src.Read(data, s, datalen);
						if (nread > 0)
						{
							s += nread;
							datalen -= nread;
							count += nread;
						}
					}
					while (datalen > 0 && nread > 0);
					if (count <= 0) break;

					int _i = count;
					while (_i > 0)
					{
						_i -= sendWRITE(handle, offset, data, 0, _i);
						if ((m_seq - 1) == startid ||
						   m_io.m_ins.Available() >= 1024)
						{
							while (m_io.m_ins.Available() > 0)
							{
								if (checkStatus(m_ackid, _header))
								{
									_ackid = m_ackid[0];
									if (startid > _ackid || _ackid > m_seq - 1)
									{
										if (_ackid == m_seq)
										{
											//!!! Console.Error.WriteLine("ack error: startid=" + startid + " seq=" + m_seq + " _ackid=" + _ackid);
										}
										else
											throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "ack error: startid=" + startid + " seq=" + m_seq + " _ackid=" + _ackid);
									}
									ackcount++;
								}
								else
								{
									break;
								}
							}
						}
					}
					offset += count;
					if (monitor != null && !monitor.Count(count))
					{
						break;
					}
				}
				int _ackcount = m_seq - startid;
				while (_ackcount > ackcount)
				{
					if (!checkStatus(null, _header))
						break;
					ackcount++;
				}
				if (monitor != null) monitor.End();
				_sendCLOSE(handle, _header);
			}
			catch (Exception e)
			{
				if (e is SftpException) throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, e.ToString());
			}
		}

		private SftpATTRS execStat(string path)
		{
			try
			{
				sendSTAT(Util.getBytesUTF8(path));

				Header _header = fillHeader(m_buffer, new Header());
				int length = _header.length;
				int type = _header.type;
				m_buffer.rewind();
				fill(m_buffer.m_buffer, 0, length);

				if (type != SSH_FXP_ATTRS)
				{
					if (type == SSH_FXP_STATUS)
					{
						int i = m_buffer.getInt();
						throwStatusError(m_buffer, i);
					}
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
				}
				SftpATTRS attr = SftpATTRS.getATTR(m_buffer);
				return attr;
			}
			catch (Exception e)
			{
				if (e is SftpException)
					throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}

		public OutputStream put(string dst)
		{
			return put(dst, (SftpProgressMonitor)null, ChannelSftpModes.OVERWRITE);
		}

		public OutputStream put(string dst, ChannelSftpModes mode)
		{
			return put(dst, (SftpProgressMonitor)null, mode);
		}
		public OutputStream put(string dst, SftpProgressMonitor monitor, ChannelSftpModes mode)
		{
			return put(dst, monitor, mode, 0);
		}
		public OutputStream put(string dst, SftpProgressMonitor monitor, ChannelSftpModes mode, long offset)
		{
			dst = remoteAbsolutePath(dst);
			try
			{
				List<string> v = glob_remote(dst);
				if (v.Count != 1)
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, v.ToString());
				dst = v[0];
				if (isRemoteDir(dst))
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, dst + " is a directory");

				long skip = 0;
				if (mode == ChannelSftpModes.RESUME || mode == ChannelSftpModes.APPEND)
				{
					try
					{
						SftpATTRS attr = stat(dst);
						skip = attr.getSize();
					}
					catch (Exception)
					{ }
				}

				if (mode == ChannelSftpModes.OVERWRITE)
					sendOPENW(Util.getBytesUTF8(dst));
				else
					sendOPENA(Util.getBytesUTF8(dst));

				Header _header = new Header();
				_header = fillHeader(m_buffer, _header);
				int length = _header.length;
				int type = _header.type;

				m_buffer.rewind();
				fill(m_buffer.m_buffer, 0, length);

				if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");

				if (type == SSH_FXP_STATUS)
				{
					int i = m_buffer.getInt();
					throwStatusError(m_buffer, i);
				}

				byte[] handle = m_buffer.getString();         // filename

				if (mode == ChannelSftpModes.RESUME || mode == ChannelSftpModes.APPEND)
					offset += skip;

				long[] _offset = new long[1];
				_offset[0] = offset;
				return new OutputStreamPut(this, handle, _offset, monitor);
			}
			catch (Exception e)
			{
				if (e is SftpException)
					throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}

		public void get(string src, string dst)
		{
			get(src, dst, (SftpProgressMonitor)null, ChannelSftpModes.OVERWRITE);
		}
		public void get(string src, string dst, SftpProgressMonitor monitor)
		{
			get(src, dst, monitor, ChannelSftpModes.OVERWRITE);
		}

		public void get(string src, string dst, SftpProgressMonitor monitor, ChannelSftpModes mode)
		{
			src = remoteAbsolutePath(src);
			dst = localAbsolutePath(dst);
			try
			{
				List<string> v = glob_remote(src);
				int vsize = v.Count;
				if (vsize == 0)
					throw new SftpException(ChannelSftpResult.SSH_FX_NO_SUCH_FILE, "No such file");

				File dstFile = new File(dst);
				bool isDstDir = dstFile.IsDirectory;
				StringBuilder dstsb = null;
				if (isDstDir)
				{
					if (!dst.EndsWith(m_file_separator))
						dst += m_file_separator;
					dstsb = new StringBuilder(dst);
				}
				else if (vsize > 1)
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "Copying multiple files, but destination is missing or a file.");

				for (int j = 0; j < vsize; j++)
				{
					string _src = v[j];

					SftpATTRS attr = execStat(_src);
					if (attr.isDir())
						throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "not supported to get directory " + _src);

					string _dst = null;
					if (isDstDir)
					{
						int i = _src.LastIndexOf('/');
						if (i == -1)
							dstsb.Append(_src);
						else
							dstsb.Append(_src.Substring(i + 1));
						_dst = dstsb.ToString();
						dstsb.Remove(dst.Length, _dst.Length - dst.Length);
					}
					else
						_dst = dst;

					if (mode == ChannelSftpModes.RESUME)
					{
						long size_of_src = attr.getSize();
						long size_of_dst = new File(_dst).Length;
						if (size_of_dst > size_of_src)
							throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "failed to resume for " + _dst);
						if (size_of_dst == size_of_src)
							return;
					}

					if (monitor != null)
					{
						monitor.Init(SftpProgressMonitor.SfrpOperation.GET, _src, _dst, attr.getSize());
						if (mode == ChannelSftpModes.RESUME)
							monitor.Count(new File(_dst).Length);
					}

					FileOutputStream fos = null;
					if (mode == ChannelSftpModes.OVERWRITE)
						fos = new FileOutputStream(_dst);
					else
						fos = new FileOutputStream(_dst, true); // append

					execGet(_src, fos, monitor, mode, new File(_dst).Length);
					fos.close();
				}
			}
			catch (Exception e)
			{
				if (e is SftpException) throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}
		public void get(string src, OutputStream dst)
		{
			get(src, dst, null, ChannelSftpModes.OVERWRITE, 0);
		}
		public void get(string src, OutputStream dst, SftpProgressMonitor monitor)
		{
			get(src, dst, monitor, ChannelSftpModes.OVERWRITE, 0);
		}

		public void get(string src, OutputStream dst, SftpProgressMonitor monitor, ChannelSftpModes mode, long skip)
		{
			try
			{
				src = remoteAbsolutePath(src);
				List<string> v = glob_remote(src);
				if (v.Count != 1)
				{
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, v.ToString());
				}
				src = v[0];

				if (monitor != null)
				{
					SftpATTRS attr = execStat(src);
					monitor.Init(SftpProgressMonitor.SfrpOperation.GET, src, "??", attr.getSize());
					if (mode == ChannelSftpModes.RESUME)
						monitor.Count(skip);
				}
				execGet(src, dst, monitor, mode, skip);
			}
			catch (Exception e)
			{
				if (e is SftpException)
					throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}

		private void execGet(string src, OutputStream dst, SftpProgressMonitor monitor, ChannelSftpModes mode, long skip)
		{
			try
			{
				sendOPENR(Util.getBytesUTF8(src));

				Header header = fillHeader(m_buffer, new Header());
				int length = header.length;
				int type = header.type;

				m_buffer.rewind();

				fill(m_buffer.m_buffer, 0, length);

				if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "Type is " + type);

				if (type == SSH_FXP_STATUS)
				{
					int i = m_buffer.getInt();
					throwStatusError(m_buffer, i);
				}

				byte[] handle = m_buffer.getString();         // filename

				long offset = 0;
				if (mode == ChannelSftpModes.RESUME)
					offset += skip;

				int request_len = 0;

				while (true)
				{

					request_len = m_buffer.m_buffer.Length - 13;
					if (m_server_version == 0) { request_len = 1024; }
					sendREAD(handle, offset, request_len);

					header = fillHeader(m_buffer, header);
					length = header.length;
					type = header.type;

					int i;
					if (type == SSH_FXP_STATUS)
					{
						m_buffer.rewind();
						fill(m_buffer.m_buffer, 0, length);
						i = m_buffer.getInt();
						if (i == (int)ChannelSftpResult.SSH_FX_EOF)
							goto BREAK;

						throwStatusError(m_buffer, i);
					}

					if (type != SSH_FXP_DATA)
						goto BREAK;

					m_buffer.rewind();
					fill(m_buffer.m_buffer, 0, 4); length -= 4;
					i = m_buffer.getInt();   // length of data 
					int foo = i;
					while (foo > 0)
					{
						int bar = foo;
						if (bar > m_buffer.m_buffer.Length)
						{
							bar = m_buffer.m_buffer.Length;
						}
						i = m_io.m_ins.Read(m_buffer.m_buffer, 0, bar);
						if (i < 0)
						{
							goto BREAK;
						}
						int data_len = i;
						dst.write(m_buffer.m_buffer, 0, data_len);

						offset += data_len;
						foo -= data_len;

						if (monitor != null)
						{
							if (!monitor.Count(data_len))
							{
								while (foo > 0)
								{
									i = m_io.m_ins.Read(
											m_buffer.m_buffer,
											0,
											(m_buffer.m_buffer.Length < foo ? m_buffer.m_buffer.Length : foo)
										);
									if (i <= 0)
										break;
									foo -= i;
								}
								goto BREAK;
							}
						}
					}
				}
			BREAK:
				dst.flush();

				if (monitor != null) monitor.End();
				_sendCLOSE(handle, header);
			}
			catch (Exception e)
			{
				if (e is SftpException)
					throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}

		public InputStream Get(string src)
		{
			return Get(src, (SftpProgressMonitor)null, ChannelSftpModes.OVERWRITE);
		}
		public InputStream Get(string src, SftpProgressMonitor monitor)
		{
			return Get(src, monitor, ChannelSftpModes.OVERWRITE);
		}
		public InputStream Get(string src, ChannelSftpModes mode)
		{
			return Get(src, (SftpProgressMonitor)null, mode);
		}
		public InputStream Get(string src, SftpProgressMonitor monitor, ChannelSftpModes mode)
		{
			if (mode == ChannelSftpModes.RESUME)
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "faile to resume from " + src);

			try
			{
				src = remoteAbsolutePath(src);
				List<string> v = glob_remote(src);
				if (v.Count != 1)
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, v.ToString());

				src = v[0];
				SftpATTRS attr = execStat(src);

				if (monitor != null)
					monitor.Init(SftpProgressMonitor.SfrpOperation.GET, src, "??", attr.getSize());

				sendOPENR(Util.getBytesUTF8(src));

				Header _header = new Header();
				_header = fillHeader(m_buffer, _header);
				int length = _header.length;
				int type = _header.type;
				m_buffer.rewind();
				fill(m_buffer.m_buffer, 0, length);

				if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
				{
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
				}
				if (type == SSH_FXP_STATUS)
				{
					int i = m_buffer.getInt();
					throwStatusError(m_buffer, i);
				}

				byte[] handle = m_buffer.getString();         // filename
				return new InputStreamGet(this, handle, monitor);
			}
			catch (Exception e)
			{
				if (e is SftpException) throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}

		public ArrayList ls(string path)
		{
			try
			{
				path = remoteAbsolutePath(path);

				string dir = path;
				byte[] pattern = null;
				SftpATTRS attr = stat(dir);
				if (isPattern(dir) || (attr != null && !attr.isDir()))
				{
					int foo = path.LastIndexOf('/');
					dir = path.Substring(0, ((foo == 0) ? 1 : foo));
					pattern = Util.getBytesUTF8(path.Substring(foo + 1));
				}

				sendOPENDIR(Util.getBytesUTF8(dir));

				Header _header = new Header();
				_header = fillHeader(m_buffer, _header);
				int length = _header.length;
				int type = _header.type;
				m_buffer.rewind();
				fill(m_buffer.m_buffer, 0, length);

				if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
				{
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
				}
				if (type == SSH_FXP_STATUS)
				{
					int i = m_buffer.getInt();
					throwStatusError(m_buffer, i);
				}

				byte[] handle = m_buffer.getString();         // filename

				ArrayList v = new ArrayList();
				while (true)
				{
					sendREADDIR(handle);

					_header = fillHeader(m_buffer, _header);
					length = _header.length;
					type = _header.type;
					if (type != SSH_FXP_STATUS && type != SSH_FXP_NAME)
						throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");

					if (type == SSH_FXP_STATUS)
					{
						m_buffer.rewind();
						fill(m_buffer.m_buffer, 0, length);
						int i = m_buffer.getInt();
						if (i == (int)ChannelSftpResult.SSH_FX_EOF)
							break;
						throwStatusError(m_buffer, i);
					}

					m_buffer.rewind();
					fill(m_buffer.m_buffer, 0, 4); length -= 4;
					int count = m_buffer.getInt();

					byte[] str;

					m_buffer.reset();
					while (count > 0)
					{
						if (length > 0)
						{
							m_buffer.shift();
							int j = (m_buffer.m_buffer.Length > (m_buffer.m_index + length)) ? length : (m_buffer.m_buffer.Length - m_buffer.m_index);
							int i = fill(m_buffer.m_buffer, m_buffer.m_index, j);
							m_buffer.m_index += i;
							length -= i;
						}
						byte[] filename = m_buffer.getString();
						str = m_buffer.getString();
						string longname = Util.getStringUTF8(str);

						SftpATTRS attrs = SftpATTRS.getATTR(m_buffer);
						if (pattern == null || Util.glob(pattern, filename))
						{
							v.Add(new LsEntry(Util.getStringUTF8(filename), longname, attrs));
						}

						count--;
					}
				}
				_sendCLOSE(handle, _header);
				return v;
			}
			catch (Exception e)
			{
				if (e is SftpException)
					throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}

		public string readlink(string path)
		{
			try
			{
				path = remoteAbsolutePath(path);
				List<string> v = glob_remote(path);
				if (v.Count != 1)
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, v.ToString());

				path = v[0];
				sendREADLINK(Util.getBytesUTF8(path));

				Header _header = new Header();
				_header = fillHeader(m_buffer, _header);
				int length = _header.length;
				int type = _header.type;
				m_buffer.rewind();
				fill(m_buffer.m_buffer, 0, length);

				if (type != SSH_FXP_STATUS && type != SSH_FXP_NAME)
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");

				int i;
				if (type == SSH_FXP_NAME)
				{
					int count = m_buffer.getInt();       // count
					byte[] filename = null;
					byte[] longname = null;
					for (i = 0; i < count; i++)
					{
						filename = m_buffer.getString();
						longname = m_buffer.getString();
						SftpATTRS.getATTR(m_buffer);
					}
					return Util.getStringUTF8(filename);
				}

				i = m_buffer.getInt();
				throwStatusError(m_buffer, i);
			}
			catch (Exception e)
			{
				if (e is SftpException) throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
			return null;
		}


		public void symlink(string oldpath, string newpath)
		{
			if (m_server_version < 3)
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "The remote sshd is too old to support symlink operation.");

			try
			{
				oldpath = remoteAbsolutePath(oldpath);
				newpath = remoteAbsolutePath(newpath);

				List<string> v = glob_remote(oldpath);
				int vsize = v.Count;
				if (vsize != 1)
				{
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, v.ToString());
				}
				oldpath = v[0];

				if (isPattern(newpath))
				{
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, v.ToString());
				}

				newpath = Util.Unquote(newpath);

				sendSYMLINK(Util.getBytesUTF8(oldpath), Util.getBytesUTF8(newpath));

				Header _header = new Header();
				_header = fillHeader(m_buffer, _header);
				int length = _header.length;
				int type = _header.type;
				m_buffer.rewind();
				fill(m_buffer.m_buffer, 0, length);

				if (type != SSH_FXP_STATUS)
				{
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
				}

				int i = m_buffer.getInt();
				if (i == (int)ChannelSftpResult.SSH_FX_OK)
					return;
				throwStatusError(m_buffer, i);
			}
			catch (Exception e)
			{
				if (e is SftpException) throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}
		public void rename(string oldpath, string newpath)
		{
			if (m_server_version < 2)
			{
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "The remote sshd is too old to support rename operation.");
			}
			try
			{
				oldpath = remoteAbsolutePath(oldpath);
				newpath = remoteAbsolutePath(newpath);

				List<string> v = glob_remote(oldpath);
				int vsize = v.Count;
				if (vsize != 1)
				{
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, v.ToString());
				}
				oldpath = v[0];

				v = glob_remote(newpath);
				vsize = v.Count;
				if (vsize >= 2)
				{
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, v.ToString());
				}
				if (vsize == 1)
				{
					newpath = v[0];
				}
				else
				{  // vsize==0
					if (isPattern(newpath))
						throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, newpath);
					newpath = Util.Unquote(newpath);
				}

				sendRENAME(Util.getBytesUTF8(oldpath), Util.getBytesUTF8(newpath));

				Header _header = new Header();
				_header = fillHeader(m_buffer, _header);
				int length = _header.length;
				int type = _header.type;
				m_buffer.rewind();
				fill(m_buffer.m_buffer, 0, length);

				if (type != SSH_FXP_STATUS)
				{
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
				}

				int i = m_buffer.getInt();
				if (i == (int)ChannelSftpResult.SSH_FX_OK) return;
				throwStatusError(m_buffer, i);
			}
			catch (Exception e)
			{
				if (e is SftpException) throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}
		public void rm(string path)
		{
			try
			{
				path = remoteAbsolutePath(path);
				List<string> v = glob_remote(path);
				int vsize = v.Count;
				Header _header = new Header();

				for (int j = 0; j < vsize; j++)
				{
					path = v[j];
					sendREMOVE(Util.getBytesUTF8(path));

					_header = fillHeader(m_buffer, _header);
					int length = _header.length;
					int type = _header.type;
					m_buffer.rewind();
					fill(m_buffer.m_buffer, 0, length);

					if (type != SSH_FXP_STATUS)
						throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");

					int i = m_buffer.getInt();
					if (i != (int)ChannelSftpResult.SSH_FX_OK)
						throwStatusError(m_buffer, i);
				}
			}
			catch (Exception e)
			{
				if (e is SftpException) throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}
		private bool isRemoteDir(string path)
		{
			try
			{
				sendSTAT(Util.getBytesUTF8(path));

				Header _header = new Header();
				_header = fillHeader(m_buffer, _header);
				int length = _header.length;
				int type = _header.type;
				m_buffer.rewind();
				fill(m_buffer.m_buffer, 0, length);

				if (type != SSH_FXP_ATTRS)
				{
					return false;
				}
				SftpATTRS attr = SftpATTRS.getATTR(m_buffer);
				return attr.isDir();
			}
			catch (Exception)
			{ }
			return false;
		}

		public void chgrp(int gid, string path)
		{
			try
			{
				path = remoteAbsolutePath(path);

				List<string> v = glob_remote(path);
				int vsize = v.Count;
				for (int j = 0; j < vsize; j++)
				{
					path = v[j];

					SftpATTRS attr = execStat(path);

					attr.setFLAGS(0);
					attr.setUIDGID(attr.m_uid, gid);
					_setStat(path, attr);
				}
			}
			catch (Exception e)
			{
				if (e is SftpException) throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}

		public void chown(int uid, string path)
		{
			try
			{
				path = remoteAbsolutePath(path);

				List<string> v = glob_remote(path);
				int vsize = v.Count;
				for (int j = 0; j < vsize; j++)
				{
					path = v[j];

					SftpATTRS attr = execStat(path);

					attr.setFLAGS(0);
					attr.setUIDGID(uid, attr.m_gid);
					_setStat(path, attr);
				}
			}
			catch (Exception e)
			{
				if (e is SftpException) throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}
		public void chmod(int permissions, string path)
		{
			try
			{
				path = remoteAbsolutePath(path);

				List<string> v = glob_remote(path);
				int vsize = v.Count;
				for (int j = 0; j < vsize; j++)
				{
					path = v[j];

					SftpATTRS attr = execStat(path);

					attr.setFLAGS(0);
					attr.setPERMISSIONS(permissions);
					_setStat(path, attr);
				}
			}
			catch (Exception e)
			{
				if (e is SftpException) throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}

		public void setMtime(string path, int mtime)
		{
			try
			{
				path = remoteAbsolutePath(path);
				List<string> v = glob_remote(path);
				int vsize = v.Count;
				for (int j = 0; j < vsize; j++)
				{
					path = (string)(v[j]);
					SftpATTRS attr = execStat(path);
					attr.setFLAGS(0);
					attr.setACMODTIME(attr.getATime(), mtime);
					_setStat(path, attr);
				}
			}
			catch (Exception e)
			{
				if (e is SftpException)
					throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}
		public void rmdir(string path)
		{
			try
			{
				path = remoteAbsolutePath(path);

				List<string> v = glob_remote(path);
				int vsize = v.Count;
				Header _header = new Header();

				for (int j = 0; j < vsize; j++)
				{
					path = v[j];
					sendRMDIR(Util.getBytesUTF8(path));

					_header = fillHeader(m_buffer, _header);
					int length = _header.length;
					int type = _header.type;
					m_buffer.rewind();
					fill(m_buffer.m_buffer, 0, length);

					if (type != SSH_FXP_STATUS)
					{
						throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
					}

					int i = m_buffer.getInt();
					if (i != (int)ChannelSftpResult.SSH_FX_OK)
					{
						throwStatusError(m_buffer, i);
					}
				}
			}
			catch (Exception e)
			{
				if (e is SftpException) throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}

		public void mkdir(string path)
		{
			try
			{
				path = remoteAbsolutePath(path);
				sendMKDIR(Util.getBytesUTF8(path), null);

				Header header = fillHeader(m_buffer, new Header());
				int length = header.length;
				int type = header.type;
				m_buffer.rewind();
				fill(m_buffer.m_buffer, 0, length);

				if (type != SSH_FXP_STATUS)
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");

				int i = m_buffer.getInt();
				if (i == (int)ChannelSftpResult.SSH_FX_OK) return;
				throwStatusError(m_buffer, i);
			}
			catch (Exception e)
			{
				if (e is SftpException)
					throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}

		public SftpATTRS stat(string path)
		{
			try
			{
				path = remoteAbsolutePath(path);

				List<string> v = glob_remote(path);
				if (v.Count != 1)
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, v.ToString());

				path = v[0];
				return execStat(path);
			}
			catch (Exception e)
			{
				if (e is SftpException)
					throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}
		public SftpATTRS lstat(string path)
		{
			try
			{
				path = remoteAbsolutePath(path);

				List<string> v = glob_remote(path);
				if (v.Count != 1)
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, v.ToString());

				path = v[0];
				return _lstat(path);
			}
			catch (Exception e)
			{
				if (e is SftpException)
					throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}

		private SftpATTRS _lstat(string path)
		{
			try
			{
				sendLSTAT(Util.getBytesUTF8(path));

				Header _header = new Header();
				_header = fillHeader(m_buffer, _header);
				int length = _header.length;
				int type = _header.type;
				m_buffer.rewind();
				fill(m_buffer.m_buffer, 0, length);

				if (type != SSH_FXP_ATTRS)
				{
					if (type == SSH_FXP_STATUS)
					{
						int i = m_buffer.getInt();
						throwStatusError(m_buffer, i);
					}
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
				}
				SftpATTRS attr = SftpATTRS.getATTR(m_buffer);
				return attr;
			}
			catch (Exception e)
			{
				if (e is SftpException) throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}


		public void setStat(string path, SftpATTRS attr)
		{
			try
			{
				path = remoteAbsolutePath(path);
				List<string> v = glob_remote(path);
				int vsize = v.Count;
				for (int j = 0; j < vsize; j++)
				{
					path = v[j];
					_setStat(path, attr);
				}
			}
			catch (Exception e)
			{
				if (e is SftpException)
					throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}
		private void _setStat(string path, SftpATTRS attr)
		{
			try
			{
				sendSETSTAT(Util.getBytesUTF8(path), attr);

				Header _header = new Header();
				_header = fillHeader(m_buffer, _header);
				int length = _header.length;
				int type = _header.type;
				m_buffer.rewind();
				fill(m_buffer.m_buffer, 0, length);

				if (type != SSH_FXP_STATUS)
				{
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
				}
				int i = m_buffer.getInt();
				if (i != (int)ChannelSftpResult.SSH_FX_OK)
				{
					throwStatusError(m_buffer, i);
				}
			}
			catch (Exception e)
			{
				if (e is SftpException) throw (SftpException)e;
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
			}
		}

		private void read(byte[] buf, int s, int l)
		{
			int i = 0;
			while (l > 0)
			{
				i = m_io.m_ins.Read(buf, s, l);
				if (i <= 0)
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");

				s += i;
				l -= i;
			}
		}
		internal bool checkStatus(int[] ackid, Header _header)
		{
			_header = fillHeader(m_buffer, _header);
			int length = _header.length;
			int type = _header.type;
			if (ackid != null)
				ackid[0] = _header.rid;
			m_buffer.rewind();
			fill(m_buffer.m_buffer, 0, length);

			if (type != SSH_FXP_STATUS)
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");

			int i = m_buffer.getInt();
			if (i != (int)ChannelSftpResult.SSH_FX_OK)
				throwStatusError(m_buffer, i);

			return true;
		}

		internal bool _sendCLOSE(byte[] handle, Header header)
		//throws Exception
		{
			sendCLOSE(handle);
			return checkStatus(null, header);
		}

		private void sendINIT()
		{
			m_packet.reset();
			putHEAD(SSH_FXP_INIT, 5);
			m_buffer.putInt(3);                // version 3
			m_session.write(m_packet, this, 5 + 4);
		}

		private void sendREALPATH(byte[] path)
		{
			sendPacketPath(SSH_FXP_REALPATH, path);
		}
		private void sendSTAT(byte[] path)
		{
			sendPacketPath(SSH_FXP_STAT, path);
		}
		private void sendLSTAT(byte[] path)
		{
			sendPacketPath(SSH_FXP_LSTAT, path);
		}
		private void sendFSTAT(byte[] handle)
		{
			sendPacketPath(SSH_FXP_FSTAT, handle);
		}
		private void sendSETSTAT(byte[] path, SftpATTRS attr)
		{
			m_packet.reset();
			putHEAD(SSH_FXP_SETSTAT, 9 + path.Length + attr.Length());
			m_buffer.putInt(m_seq++);
			m_buffer.putString(path);             // path
			attr.dump(m_buffer);
			m_session.write(m_packet, this, 9 + path.Length + attr.Length() + 4);
		}
		private void sendREMOVE(byte[] path)
		{
			sendPacketPath(SSH_FXP_REMOVE, path);
		}
		private void sendMKDIR(byte[] path, SftpATTRS attr)
		{
			m_packet.reset();
			putHEAD(SSH_FXP_MKDIR, 9 + path.Length + (attr != null ? attr.Length() : 4));
			m_buffer.putInt(m_seq++);
			m_buffer.putString(path);             // path
			if (attr != null) attr.dump(m_buffer);
			else m_buffer.putInt(0);
			m_session.write(m_packet, this, 9 + path.Length + (attr != null ? attr.Length() : 4) + 4);
		}
		private void sendRMDIR(byte[] path)
		{
			sendPacketPath(SSH_FXP_RMDIR, path);
		}
		private void sendSYMLINK(byte[] p1, byte[] p2)
		{
			sendPacketPath(SSH_FXP_SYMLINK, p1, p2);
		}
		private void sendREADLINK(byte[] path)
		{
			sendPacketPath(SSH_FXP_READLINK, path);
		}
		private void sendOPENDIR(byte[] path)
		{
			sendPacketPath(SSH_FXP_OPENDIR, path);
		}
		private void sendREADDIR(byte[] path)
		{
			sendPacketPath(SSH_FXP_READDIR, path);
		}
		private void sendRENAME(byte[] p1, byte[] p2)
		{
			sendPacketPath(SSH_FXP_RENAME, p1, p2);
		}
		private void sendCLOSE(byte[] path)
		{
			sendPacketPath(SSH_FXP_CLOSE, path);
		}
		private void sendOPENR(byte[] path)
		{
			sendOPEN(path, SSH_FXF_READ);
		}
		private void sendOPENW(byte[] path)
		{
			sendOPEN(path, SSH_FXF_WRITE | SSH_FXF_CREAT | SSH_FXF_TRUNC);
		}
		private void sendOPENA(byte[] path)
		{
			sendOPEN(path, SSH_FXF_WRITE |/*SSH_FXF_APPEND|*/SSH_FXF_CREAT);
		}
		private void sendOPEN(byte[] path, int mode)
		{
			m_packet.reset();
			putHEAD(SSH_FXP_OPEN, 17 + path.Length);
			m_buffer.putInt(m_seq++);
			m_buffer.putString(path);
			m_buffer.putInt(mode);
			m_buffer.putInt(0);           // attrs
			m_session.write(m_packet, this, 17 + path.Length + 4);
		}
		private void sendPacketPath(byte fxp, byte[] path)
		{
			m_packet.reset();
			putHEAD(fxp, 9 + path.Length);
			m_buffer.putInt(m_seq++);
			m_buffer.putString(path);             // path
			m_session.write(m_packet, this, 9 + path.Length + 4);
		}
		private void sendPacketPath(byte fxp, byte[] p1, byte[] p2)
		{
			m_packet.reset();
			putHEAD(fxp, 13 + p1.Length + p2.Length);
			m_buffer.putInt(m_seq++);
			m_buffer.putString(p1);
			m_buffer.putString(p2);
			m_session.write(m_packet, this, 13 + p1.Length + p2.Length + 4);
		}

		internal int sendWRITE(byte[] handle, long offset,
			byte[] data, int start, int length)
		{
			int _length = length;
			m_packet.reset();
			if (m_buffer.m_buffer.Length < m_buffer.m_index + 13 + 21 + handle.Length + length
				+ 32 + 20  // padding and mac
				)
			{
				_length = m_buffer.m_buffer.Length - (m_buffer.m_index + 13 + 21 + handle.Length
					+ 32 + 20  // padding and mac
					);
			}
			putHEAD(SSH_FXP_WRITE, 21 + handle.Length + _length);   // 14
			m_buffer.putInt(m_seq++);                                       //  4
			m_buffer.putString(handle);                                 //  4+handle.length
			m_buffer.putLong(offset);                                   //  8
			if (m_buffer.m_buffer != data)
			{
				m_buffer.putString(data, start, _length);   //  4+_length
			}
			else
			{
				m_buffer.putInt(_length);
				m_buffer.skip(_length);
			}
			m_session.write(m_packet, this, 21 + handle.Length + _length + 4);
			return _length;
		}

		private void sendREAD(byte[] handle, long offset, int length)
		{
			m_packet.reset();
			putHEAD(SSH_FXP_READ, 21 + handle.Length);
			m_buffer.putInt(m_seq++);
			m_buffer.putString(handle);
			m_buffer.putLong(offset);
			m_buffer.putInt(length);
			m_session.write(m_packet, this, 21 + handle.Length + 4);
		}

		private void putHEAD(byte type, int length)
		{
			m_buffer.putByte((byte)Session.SSH_MSG_CHANNEL_DATA);
			m_buffer.putInt(m_recipient);
			m_buffer.putInt(length + 4);
			m_buffer.putInt(length);
			m_buffer.putByte(type);
		}

		private List<string> glob_remote(string _path)
		{
			List<string> v = new List<string>();
			byte[] path = Util.getBytesUTF8(_path);

			if (!isPatternEx(_path))
			{
				v.Add(Util.Unquote(_path));
				return v;
			}

			int i = path.Length - 1;
			while (i >= 0)
			{
				if (path[i] == '/')
					break;
				i--;
			}
			if (i < 0)
			{
				v.Add(Util.Unquote(_path));
				return v;
			}

			byte[] dir;
			if (i == 0)
				dir = new byte[] { (byte)'/' };
			else
			{
				dir = new byte[i];
				Array.Copy(path, 0, dir, 0, i);
			}

			byte[] pattern = new byte[path.Length - i - 1];
			Array.Copy(path, i + 1, pattern, 0, pattern.Length);

			sendOPENDIR(dir);

			Header _header = new Header();
			_header = fillHeader(m_buffer, _header);
			int length = _header.length;
			int type = _header.type;
			m_buffer.rewind();
			fill(m_buffer.m_buffer, 0, length);

			if (type != SSH_FXP_STATUS && type != SSH_FXP_HANDLE)
				throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");

			if (type == SSH_FXP_STATUS)
			{
				i = m_buffer.getInt();
				throwStatusError(m_buffer, i);
			}

			byte[] handle = m_buffer.getString();
			while (true)
			{
				sendREADDIR(handle);
				_header = fillHeader(m_buffer, _header);
				length = _header.length;
				type = _header.type;

				if (type != SSH_FXP_STATUS && type != SSH_FXP_NAME)
					throw new SftpException(ChannelSftpResult.SSH_FX_FAILURE, "");
				if (type == SSH_FXP_STATUS)
				{
					m_buffer.rewind();
					fill(m_buffer.m_buffer, 0, length);
					break;
				}

				m_buffer.rewind();
				fill(m_buffer.m_buffer, 0, 4); length -= 4;
				int count = m_buffer.getInt();

				byte[] str;

				m_buffer.reset();
				while (count > 0)
				{
					if (length > 0)
					{
						m_buffer.shift();
						int j = (m_buffer.m_buffer.Length > (m_buffer.m_index + length)) ? length : (m_buffer.m_buffer.Length - m_buffer.m_index);
						i = m_io.m_ins.Read(m_buffer.m_buffer, m_buffer.m_index, j);
						if (i <= 0)
							break;
						m_buffer.m_index += i;
						length -= i;
					}

					byte[] filename = m_buffer.getString();
					str = m_buffer.getString();
					SftpATTRS attrs = SftpATTRS.getATTR(m_buffer);

					if (Util.glob(pattern, filename))
						v.Add(Util.getStringUTF8(dir) + "/" + Util.getStringUTF8(filename));
					count--;
				}
			}
			if (_sendCLOSE(handle, _header))
				return v;

			return null;
		}

		private List<string> glob_local(string _path)
		{
			List<string> v = new List<string>();
			byte[] path = Util.getBytesUTF8(_path);
			int i = path.Length - 1;
			while (i >= 0)
			{
				if (path[i] == '*' || path[i] == '?')
					break;
				i--;
			}
			if (i < 0)
			{
				v.Add(_path);
				return v;
			}
			while (i >= 0)
			{
				if (path[i] == m_file_separator_char)
					break;
				i--;
			}
			if (i < 0)
			{
				v.Add(_path);
				return v;
			}

			byte[] dir;
			if (i == 0)
			{
				dir = new byte[] { (byte)m_file_separator_char };
			}
			else
			{
				dir = new byte[i];
				Array.Copy(path, 0, dir, 0, i);
			}
			byte[] pattern = new byte[path.Length - i - 1];
			Array.Copy(path, i + 1, pattern, 0, pattern.Length);

			try
			{
				List<string> children = (new File(Util.getStringUTF8(dir))).List();
				foreach (string entry in children)
					if (Util.glob(pattern, Util.getBytesUTF8(entry)))
						v.Add(Util.getStringUTF8(dir) + m_file_separator + entry);
			}
			catch { }
			return v;
		}

		private void throwStatusError(Buffer buf, ChannelSftpResult i)
		{
			if (m_server_version >= 3)
				throw new SftpException(i, Util.getStringUTF8(buf.getString()));
			else
				throw new SftpException(i, "Failure");
		}

		private void throwStatusError(Buffer buf, int i)
		{
			if (m_server_version >= 3)
				throw new SftpException(i, Util.getStringUTF8(buf.getString()));
			else
				throw new SftpException(i, "Failure");
		}

		private static bool isLocalAbsolutePath(string path)
		{
			return (new File(path)).IsAbsolute;
		}

		public override void disconnect()
		{
			clearRunningThreads();
			base.disconnect();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		protected void addRunningThread(Thread thread)
		{
			if (m_threadList == null)
				m_threadList = new List<Thread>();
			m_threadList.Add(thread);
		}
		[MethodImpl(MethodImplOptions.Synchronized)]
		protected void clearRunningThreads()
		{
			if (m_threadList == null)
				return;
			for (int idx = 0; idx < m_threadList.Count; idx++)
			{
				Thread thread = m_threadList[idx];
				if (thread != null && thread.IsAlive())
					thread.Interrupt();
			}
			m_threadList.Clear();
		}
		private bool isPattern(string path)
		{
			return (path.IndexOf("*") != -1 || path.IndexOf("?") != -1);
		}

		private bool isPatternEx(string path)
		{
			int i = path.Length - 1;
			while (i >= 0)
			{
				if (path[i] == '*' || path[i] == '?')
				{
					if (i > 0 && path[i - 1] == '\\')
						i--;
					else
						break;
				}
				i--;
			}
			return !(i < 0);
		}

		private int fill(byte[] buf, int s, int len)
		{
			int i = 0;
			int foo = s;
			while (len > 0)
			{
				i = m_io.m_ins.Read(buf, s, len);
				if (i <= 0)
					throw new System.IO.IOException("inputstream is closed");
				s += i;
				len -= i;
			}
			return s - foo;
		}

		private void skip(long foo)
		{
			while (foo > 0)
			{
				long bar = m_io.m_ins.Skip(foo);
				if (bar <= 0)
					break;
				foo -= bar;
			}
		}

		internal class Header
		{
			public int length;
			public int type;
			public int rid;
		}

		private Header fillHeader(Buffer buf, Header header)
		{
			buf.rewind();
			int i = fill(buf.m_buffer, 0, 9);
			header.length = buf.getInt() - 5;
			header.type = buf.getByte() & 0xFF;
			header.rid = buf.getInt();
			return header;
		}

		private string remoteAbsolutePath(string path)
		{
			if (path[0] == '/')
				return path;
			if (m_cwd.EndsWith("/"))
				return m_cwd + path;
			return m_cwd + "/" + path;
		}

		private string localAbsolutePath(string path)
		{
			if (isLocalAbsolutePath(path)) return path;
			if (m_lcwd.EndsWith(m_file_separator)) return m_lcwd + path;
			return m_lcwd + m_file_separator + path;
		}

		/// <summary>
		/// 
		/// </summary>
		public class LsEntry
		{
			private string m_filename;
			private string m_longname;
			private SftpATTRS m_attrs;

			internal LsEntry(string filename, string longname, SftpATTRS attrs)
			{
				Filename = filename;
				Longname = longname;
				Attrs = attrs;
			}

			public string Filename
			{
				get { return m_filename; }
				private set { m_filename = value; }
			}
			public string Longname
			{
				get { return m_longname; }
				private set { m_longname = value; }
			}

			public SftpATTRS Attrs
			{
				get { return m_attrs; }
				private set { m_attrs = value; }
			}

			public override string ToString() { return m_longname; }
		}

		/// <summary>
		/// 
		/// </summary>
		public class InputStreamGet : InputStream
		{
			ChannelSftp m_sftp;
			SftpProgressMonitor m_monitor;
			long m_offset = 0;
			bool m_closed = false;
			int m_rest_length = 0;
			byte[] m_data = new byte[1];
			byte[] m_rest_byte = new byte[1024];
			byte[] m_handle;
			Header m_header = new Header();

			public InputStreamGet(ChannelSftp sftp, byte[] handle, SftpProgressMonitor monitor)
			{
				m_sftp = sftp;
				m_handle = handle;
				m_monitor = monitor;
			}

			public override int ReadByte()
			{
				if (m_closed)
					return -1;
				int i = Read(m_data, 0, 1);
				if (i == -1)
					return -1;
				return m_data[0] & 0xff;
			}

			public override int Read(byte[] d)
			{
				if (m_closed)
					return -1;
				return Read(d, 0, d.Length);
			}

			public override int Read(byte[] dst, int start, int length)
			{
				if (m_closed)
					return -1;
				int i;
				int foo;

				if (dst == null)
					throw new System.NullReferenceException();
				if (start < 0 || length < 0 || start + length > dst.Length)
					throw new System.IndexOutOfRangeException();

				if (length == 0) { return 0; }

				if (m_rest_length > 0)
				{
					foo = m_rest_length;
					if (foo > length) foo = length;
					Array.Copy(m_rest_byte, 0, dst, start, foo);
					if (foo != m_rest_length)
					{
						Array.Copy(m_rest_byte, foo, m_rest_byte, 0, m_rest_length - foo);
					}
					if (m_monitor != null)
						if (!m_monitor.Count(foo))
						{
							close();
							return -1;
						}

					m_rest_length -= foo;
					return foo;
				}

				if (m_sftp.m_buffer.m_buffer.Length - 13 < length)
					length = m_sftp.m_buffer.m_buffer.Length - 13;

				if (m_sftp.m_server_version == 0 && length > 1024)
					length = 1024;

				try
				{
					m_sftp.sendREAD(m_handle, m_offset, length);
				}
				catch (Exception e)
				{
					throw new System.IO.IOException("Read Error", e);
				}

				m_header = m_sftp.fillHeader(m_sftp.m_buffer, m_header);
				m_rest_length = m_header.length;
				int type = m_header.type;
				int id = m_header.rid;

				if (type != SSH_FXP_STATUS && type != SSH_FXP_DATA)
				{
					throw new System.IO.IOException("Read Error");
				}
				if (type == SSH_FXP_STATUS)
				{
					m_sftp.m_buffer.rewind();
					m_sftp.fill(m_sftp.m_buffer.m_buffer, 0, m_rest_length);
					i = m_sftp.m_buffer.getInt();
					m_rest_length = 0;
					if (i == (int)ChannelSftpResult.SSH_FX_EOF)
					{
						close();
						return -1;
					}
					throw new System.IO.IOException("Read Error");
				}
				m_sftp.m_buffer.rewind();
				m_sftp.fill(m_sftp.m_buffer.m_buffer, 0, 4);
				i = m_sftp.m_buffer.getInt(); m_rest_length -= 4;

				m_offset += m_rest_length;
				foo = i;
				if (foo > 0)
				{
					int bar = m_rest_length;
					if (bar > length)
					{
						bar = length;
					}
					i = m_sftp.m_io.m_ins.Read(dst, start, bar);
					if (i < 0)
					{
						return -1;
					}
					m_rest_length -= i;

					if (m_rest_length > 0)
					{
						if (m_rest_byte.Length < m_rest_length)
						{
							m_rest_byte = new byte[m_rest_length];
						}
						int _s = 0;
						int _len = m_rest_length;
						int j;
						while (_len > 0)
						{
							j = m_sftp.m_io.m_ins.Read(m_rest_byte, _s, _len);
							if (j <= 0) break;
							_s += j;
							_len -= j;
						}
					}

					if (m_monitor != null)
						if (!m_monitor.Count(i))
						{
							close();
							return -1;
						}
					return i;
				}
				return 0;
			}

			public override void Close()
			{
				if (m_closed)
					return;
				m_closed = true;
				if (m_monitor != null)
					m_monitor.End();
				try
				{
					m_sftp._sendCLOSE(m_handle, m_header);
				}
				catch (Exception e)
				{
					throw new System.IO.IOException("Close Error", e);
				}
			}
		}
	}
}
