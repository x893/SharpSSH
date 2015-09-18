/*
 * Based on: http://www.dotnet247.com/247reference/msgs/45/225703.aspx
 * 20/9/2005
 * */
using System;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace SharpSshTest.jsch_samples
{
	/// <summary>
	/// Summary description for ClearConsole.
	/// </summary><BR/>
	public class ConsoleProgressBar
	{
		private const int STD_OUTPUT_HANDLE = -11;

		private int m_HConsoleHandle;
		private StringBuilder m_progressBar = new StringBuilder();
		private COORD m_barCoord;

		[StructLayout(LayoutKind.Sequential)]
		public struct COORD
		{
			public short X;
			public short Y;
			public COORD(short x, short y)
			{
				X = x;
				Y = y;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		struct SMALL_RECT
		{
			public short Left;
			public short Top;
			public short Right;
			public short Bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct CONSOLE_SCREEN_BUFFER_INFO
		{
			public COORD dwSize;
			public COORD dwCursorPosition;
			public int wAttributes;
			public SMALL_RECT srWindow;
			public COORD dwMaximumWindowSize;
		}

		[DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		private static extern int GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll", EntryPoint = "GetConsoleScreenBufferInfo", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		private static extern int GetConsoleScreenBufferInfo(int hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

		[DllImport("kernel32.dll", EntryPoint = "SetConsoleCursorPosition", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		private static extern int SetConsoleCursorPosition(int hConsoleOutput, COORD dwCursorPosition);

		/// <summary>Cosntructor</summary>
		public ConsoleProgressBar()
		{
			m_HConsoleHandle = GetStdHandle(STD_OUTPUT_HANDLE);
			m_barCoord = GetCursorPos();
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine();
		}

		public void SetCursorPos(short x, short y)
		{
			SetConsoleCursorPosition(m_HConsoleHandle, new COORD(x, y));
		}

		public COORD GetCursorPos()
		{
			CONSOLE_SCREEN_BUFFER_INFO res;
			GetConsoleScreenBufferInfo(m_HConsoleHandle, out res);
			return res.dwCursorPosition;
		}

		/// <summary>
		/// Updates this ProgressBar with the task's progress
		/// </summary>
		/// <param name="transferredBytes">The bytes already copied</param>
		/// <param name="totalBytes">The total bytes to be copied</param>
		/// <param name="message">A progress message</param>
		public void Update(long transferredBytes, long totalBytes, string message)
		{
			Update((int)transferredBytes, (int)totalBytes, message);
		}

		/// <summary>
		/// Updates this ProgressBar with the task's progress
		/// </summary>
		/// <param name="transferredBytes">The bytes already copied</param>
		/// <param name="totalBytes">The total bytes to be copied</param>
		/// <param name="message">A progress message</param>
		public void Update(int transferredBytes, int totalBytes, string message)
		{
			COORD cur = this.GetCursorPos();
			this.SetCursorPos(m_barCoord.X, m_barCoord.Y);

			int progress;
			if (totalBytes != 0)
				progress = (int)(transferredBytes * 100.0 / totalBytes);
			else
				progress = 0;

			m_progressBar.Length = 0;
			m_progressBar.Append(progress);
			m_progressBar.Append("% [");

			for (double i = 0; i < 50; i++)
				if (i * 2 < progress)
					m_progressBar.Append("#");
				else
					m_progressBar.Append("-");

			m_progressBar.Append("] ");

			if (totalBytes != 0)
			{
				int transferredKB = (int)(transferredBytes / 1000.0);
				int totalKB = (int)(totalBytes / 1000.0);
				m_progressBar.Append((comma(transferredKB) + "K/" + comma(totalKB) + "K\n"));
			}
			else
				m_progressBar.Append("0.0K\n");

			m_progressBar.Append(message);
			m_progressBar.Append("                        \n");

			Console.Write(m_progressBar);
			SetCursorPos(cur.X, cur.Y);
		}

		/// <summary>
		/// Return a string representation of the given n. The string will be comma seperated (e.g. 19,500,200)
		/// </summary>
		private string comma(int n)
		{
			string s = n.ToString();
			for (int i = s.Length - 3; i > 0; i = i - 3)
				s = s.Insert(i, ",");
			return s;
		}
	}
}
