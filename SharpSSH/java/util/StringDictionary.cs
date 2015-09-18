using System;
using System.Collections.Generic;

namespace SharpSsh.java.util
{
    /// <summary>
    /// Summary description for Hashtable.
    /// </summary>
    public class StringDictionary
    {
        private Dictionary<string, string> m_dictionary;

        public bool ContainKey(string key)
        {
            return m_dictionary.ContainsKey(key);
        }

		public StringDictionary()
		{
			m_dictionary = new Dictionary<string, string>();
		}

		public StringDictionary(Dictionary<string, string> h)
		{
			m_dictionary = h;
		}

		public StringDictionary(int capacity)
		{
			m_dictionary = new Dictionary<string, string>(capacity);
		}

		public void Add(string key, string value)
		{
			m_dictionary.Add(key, value);
		}

		public List<string> Keys
        {
            get { return new List<string>(m_dictionary.Keys); }
        }

		public string this[string key]
		{
			get { return m_dictionary[key]; }
			set { m_dictionary[key] = value; }
		}
	}
}