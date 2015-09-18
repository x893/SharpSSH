using System;
using System.Threading;
using SystemThreading = System.Threading;

namespace SharpSsh.java.lang
{
	/// <summary>
	/// Summary description for Thread.
	/// </summary>
	public class Thread
	{
		SystemThreading.Thread m_thread;

		public Thread(SystemThreading.Thread thread)
		{
			m_thread = thread;
		}

		public Thread(ThreadStart threadStart)
			: this(new SystemThreading.Thread(threadStart))
		{
		}

		public Thread(IRunnable runnable)
			: this(new ThreadStart(runnable.Run))
		{
		}

		public string Name
		{
			set { m_thread.Name = value; }
			get { return m_thread.Name; }
		}

		public void Start()
		{
			m_thread.Start();
		}

		public bool IsAlive()
		{
			return m_thread.IsAlive;
		}

		public void yield()
		{
		}

		public void Interrupt()
		{
			try
			{
				m_thread.Interrupt();
			}
			catch { }
		}

		public void notifyAll()
		{
			Monitor.PulseAll(this);
		}

		public static void Sleep(int t)
		{
			SystemThreading.Thread.Sleep(t);
		}

		public static void sleep(int t)
		{
			Sleep(t);
		}

		public static Thread currentThread()
		{
			return new Thread(SystemThreading.Thread.CurrentThread);
		}
	}
}
