using System;

namespace SharpSsh.java.lang
{
	/// <summary>
	/// Summary description for Class.
	/// </summary>
	public class Class
	{
		Type m_type;

		private Class(Type type)
		{
			m_type = type;
		}

		private Class(string typeName)
			: this(Type.GetType(typeName))
		{
		}

		public static Class ForName(string name)
		{
			return new Class(name);
		}

		public object Instance()
		{
			return Activator.CreateInstance(m_type);
		}
	}
}
