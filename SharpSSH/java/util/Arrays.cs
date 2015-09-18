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
			int len1 = array1.Length;

			if (len1 != array2.Length)
				return false;

			for (int idx = 0; idx < len1; idx++)
			{
				if (array1[idx] != array2[idx])
					return false;
			}
			return true;
		}
	}
}
