using System;
using System.Text;

namespace SharpSsh.jsch
{
	public class SftpATTRS
	{

		static int S_ISUID = 04000; // set user ID on execution
		static int S_ISGID = 02000; // set group ID on execution
									// static  int S_ISVTX = 01000; // sticky bit   ****** NOT DOCUMENTED *****

		static int S_IRUSR = 00400; // read by owner
		static int S_IWUSR = 00200; // write by owner
		static int S_IXUSR = 00100; // execute/search by owner
									// static  int S_IREAD = 00400; // read by owner
									// static  int S_IWRITE= 00200; // write by owner
									// static  int S_IEXEC = 00100; // execute/search by owner

		static int S_IRGRP = 00040; // read by group
		static int S_IWGRP = 00020; // write by group
		static int S_IXGRP = 00010; // execute/search by group

		static int S_IROTH = 00004; // read by others
		static int S_IWOTH = 00002; // write by others
		static int S_IXOTH = 00001; // execute/search by others

		public static int SSH_FILEXFER_ATTR_SIZE = 0x00000001;
		public static int SSH_FILEXFER_ATTR_UIDGID = 0x00000002;
		public static int SSH_FILEXFER_ATTR_PERMISSIONS = 0x00000004;
		public static int SSH_FILEXFER_ATTR_ACMODTIME = 0x00000008;
		public static uint SSH_FILEXFER_ATTR_EXTENDED = 0x80000000;

		static int S_IFDIR = 0x4000;
		static int S_IFLNK = 0xa000;

		private static int m_pmask = 0xFFF;

		private int m_flags = 0;
		private long m_size;
		internal int m_uid;
		internal int m_gid;
		private int m_permissions;
		private int m_atime;
		private int m_mtime;
		private string[] m_extended = null;

		private SftpATTRS()
		{
		}

		public string getPermissionsString()
		{
			StringBuilder buf = new StringBuilder(10);

			if (isDir()) buf.Append('d');
			else if (isLink()) buf.Append('l');
			else buf.Append('-');

			if ((m_permissions & S_IRUSR) != 0) buf.Append('r');
			else buf.Append('-');

			if ((m_permissions & S_IWUSR) != 0) buf.Append('w');
			else buf.Append('-');

			if ((m_permissions & S_ISUID) != 0) buf.Append('s');
			else if ((m_permissions & S_IXUSR) != 0) buf.Append('x');
			else buf.Append('-');

			if ((m_permissions & S_IRGRP) != 0) buf.Append('r');
			else buf.Append('-');

			if ((m_permissions & S_IWGRP) != 0) buf.Append('w');
			else buf.Append('-');

			if ((m_permissions & S_ISGID) != 0) buf.Append('s');
			else if ((m_permissions & S_IXGRP) != 0) buf.Append('x');
			else buf.Append('-');

			if ((m_permissions & S_IROTH) != 0) buf.Append('r');
			else buf.Append('-');

			if ((m_permissions & S_IWOTH) != 0) buf.Append('w');
			else buf.Append('-');

			if ((m_permissions & S_IXOTH) != 0) buf.Append('x');
			else buf.Append('-');

			return (buf.ToString());
		}

		public string getAtimeString()
		{
			DateTime d = Util.Time_T2DateTime((uint)m_atime);
			return d.ToShortDateString();
		}

		public string getMtimeString()
		{
			DateTime d = Util.Time_T2DateTime((uint)m_mtime);
			return d.ToString();
		}

		internal static SftpATTRS getATTR(Buffer buf)
		{
			SftpATTRS attr = new SftpATTRS();

			attr.m_flags = buf.getInt();

			if ((attr.m_flags & SSH_FILEXFER_ATTR_SIZE) != 0)
				attr.m_size = buf.getLong();
			if ((attr.m_flags & SSH_FILEXFER_ATTR_UIDGID) != 0)
			{
				attr.m_uid = buf.getInt();
				attr.m_gid = buf.getInt();
			}
			if ((attr.m_flags & SSH_FILEXFER_ATTR_PERMISSIONS) != 0)
				attr.m_permissions = buf.getInt();
			if ((attr.m_flags & SSH_FILEXFER_ATTR_ACMODTIME) != 0)
				attr.m_atime = buf.getInt();
			if ((attr.m_flags & SSH_FILEXFER_ATTR_ACMODTIME) != 0)
				attr.m_mtime = buf.getInt();

			if ((attr.m_flags & SSH_FILEXFER_ATTR_EXTENDED) != 0)
			{
				int count = buf.getInt();
				if (count > 0)
				{
					attr.m_extended = new String[count * 2];
					for (int i = 0; i < count; i++)
					{
						attr.m_extended[i * 2] = Util.getString(buf.getString());
						attr.m_extended[i * 2 + 1] = Util.getString(buf.getString());
					}
				}
			}
			return attr;
		}

		internal int Length()
		{
			return length();
		}

		internal int length()
		{
			int len = 4;

			if ((m_flags & SSH_FILEXFER_ATTR_SIZE) != 0) { len += 8; }
			if ((m_flags & SSH_FILEXFER_ATTR_UIDGID) != 0) { len += 8; }
			if ((m_flags & SSH_FILEXFER_ATTR_PERMISSIONS) != 0) { len += 4; }
			if ((m_flags & SSH_FILEXFER_ATTR_ACMODTIME) != 0) { len += 8; }
			if ((m_flags & SSH_FILEXFER_ATTR_EXTENDED) != 0)
			{
				len += 4;
				int count = m_extended.Length / 2;
				if (count > 0)
					for (int i = 0; i < count; i++)
					{
						len += 4; len += m_extended[i * 2].Length;
						len += 4; len += m_extended[i * 2 + 1].Length;
					}
			}
			return len;
		}

		internal void dump(Buffer buf)
		{
			buf.putInt(m_flags);
			if ((m_flags & SSH_FILEXFER_ATTR_SIZE) != 0)
				buf.putLong(m_size);
			if ((m_flags & SSH_FILEXFER_ATTR_UIDGID) != 0)
				buf.putInt(m_uid); buf.putInt(m_gid);
			if ((m_flags & SSH_FILEXFER_ATTR_PERMISSIONS) != 0)
				buf.putInt(m_permissions);

			if ((m_flags & SSH_FILEXFER_ATTR_ACMODTIME) != 0) { buf.putInt(m_atime); }
			if ((m_flags & SSH_FILEXFER_ATTR_ACMODTIME) != 0) { buf.putInt(m_mtime); }
			if ((m_flags & SSH_FILEXFER_ATTR_EXTENDED) != 0)
			{
				int count = m_extended.Length / 2;
				if (count > 0)
					for (int i = 0; i < count; i++)
					{
						buf.putString(Util.getBytes(m_extended[i * 2]));
						buf.putString(Util.getBytes(m_extended[i * 2 + 1]));
					}
			}
		}

		internal void setFLAGS(int flags)
		{
			m_flags = flags;
		}

		public void setSIZE(long size)
		{
			m_flags |= SSH_FILEXFER_ATTR_SIZE;
			m_size = size;
		}
		public void setUIDGID(int uid, int gid)
		{
			m_flags |= SSH_FILEXFER_ATTR_UIDGID;
			m_uid = uid;
			m_gid = gid;
		}
		public void setACMODTIME(int atime, int mtime)
		{
			m_flags |= SSH_FILEXFER_ATTR_ACMODTIME;
			m_atime = atime;
			m_mtime = mtime;
		}
		public void setPERMISSIONS(int permissions)
		{
			m_flags |= SSH_FILEXFER_ATTR_PERMISSIONS;
			permissions = (m_permissions & ~m_pmask) | (permissions & m_pmask);
			m_permissions = permissions;
		}

		public bool isDir()
		{
			return ((m_flags & SSH_FILEXFER_ATTR_PERMISSIONS) != 0 &&
				((m_permissions & S_IFDIR) == S_IFDIR));
		}

		public bool isLink()
		{
			return ((m_flags & SSH_FILEXFER_ATTR_PERMISSIONS) != 0 &&
				((m_permissions & S_IFLNK) == S_IFLNK));
		}

		public int getFlags() { return m_flags; }
		public long getSize() { return m_size; }
		public int getUId() { return m_uid; }
		public int getGId() { return m_gid; }
		public int getPermissions() { return m_permissions; }
		public int getATime() { return m_atime; }
		public int getMTime() { return m_mtime; }
		public string[] getExtended() { return m_extended; }

		public string toString()
		{
			return (getPermissionsString() + " " + getUId() + " " + getGId() + " " + getSize() + " " + getMtimeString());
		}

		public override string ToString()
		{
			return toString();
		}
	}
}
