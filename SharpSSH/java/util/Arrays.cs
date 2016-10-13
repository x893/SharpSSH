using System;

namespace SharpSsh.java.util
{
	/// <summary>
	/// Summary description for Arrays.
	/// </summary>
	public class Arrays
	{
		internal static bool Equals(byte[] array1, byte[] array2)
		{
			if (array1 == null)
				return (array2 == null);
			if (array2 == null)
				return (array1 == null);

			int length = array1.Length;
			if (length != array2.Length)
				return false;

			for (int idx = 0; idx < length; idx++)
			{
				if (array1[idx] != array2[idx])
					return false;
			}
			return true;
		}
	}
}
