using System;
using System.IO;
using System.Collections.Generic;

namespace SharpSsh.java.io
{
	/// <summary>
	/// Summary description for File.
	/// </summary>
	public class File
	{
		private string m_file;
		private FileInfo m_info;

		public FileInfo Info
		{
			get { return m_info; }
		}

		public File(string file)
		{
			m_file = file;
			m_info = new FileInfo(file);
		}

		public string CanonicalPath
		{
			get { return Path.GetFullPath(m_file); }
		}

		public bool IsDirectory
		{
			get { return Directory.Exists(m_file); }
		}

		public long Length
		{
			get { return m_info.Length; }
		}

		public bool IsAbsolute
		{
			get { return Path.IsPathRooted(m_file); }
		}

		public List<string> List()
		{
			string[] dirs = Directory.GetDirectories(m_file);
			string[] files = Directory.GetFiles(m_file);
			List<string> list = new List<string>(dirs.Length + files.Length);

			foreach (string dir in dirs)
				list.Add(dir);

			foreach (string file in files)
				list.Add(file);

			return list;
		}

		/// <summary>
		/// Directory separator as string
		/// </summary>
		public static string Separator
		{
			get { return Path.DirectorySeparatorChar.ToString(); }
		}
		/// <summary>
		/// Directory separator as char
		/// </summary>
		public static char SeparatorChar
		{
			get { return Path.DirectorySeparatorChar; }
		}
	}
}
