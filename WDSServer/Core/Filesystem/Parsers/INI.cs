using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace WDSServer
{
	internal class INIFile
	{

		#region "Declarations"

		// *** Lock for thread-safe access to file and local cache ***
		private object m_Lock = new object();

		// *** File name ***
		private string m_FileName = null;
		internal string FileName => m_FileName;

		// *** Lazy loading flag ***
		private bool m_Lazy = false;

		// *** Local cache ***
		private Dictionary<string, Dictionary<string, string>> m_Sections = new Dictionary<string, Dictionary<string, string>>();

		// *** Local cache modified flag ***
		private bool m_CacheModified = false;

		#endregion

		#region "Methods"

		// *** Constructor ***
		public INIFile(string FileName)
		{
			Initialize(FileName, false);
		}

		public INIFile(string FileName, bool Lazy)
		{
			Initialize(FileName, Lazy);
		}

		// *** Initialization ***
		private void Initialize(string FileName, bool Lazy)
		{
			m_FileName = FileName;
			m_Lazy = Lazy;
			if (!m_Lazy) Refresh();
		}

		// *** Read file contents into local cache ***
		internal void Refresh()
		{
			lock (m_Lock)
			{
				using (var sr = new StreamReader(m_FileName))
				{
					try
					{
						// *** Clear local cache ***
						m_Sections.Clear();

						// *** Read up the file content ***
						var CurrentSection = new Dictionary<string, string>();
						var s = string.Empty;

						while ((s = sr.ReadLine()) != null)
						{
							s = s.Trim();

							// *** Check for section names ***
							if (s.StartsWith("[") && s.EndsWith("]"))
							{
								if (s.Length > 2)
								{
									var SectionName = s.Substring(1, s.Length - 2);

									// *** Only first occurrence of a section is loaded ***
									if (!m_Sections.ContainsKey(SectionName))
										m_Sections.Add(SectionName, CurrentSection);
								}
							}
							else if (CurrentSection != null)
							{
								// *** Check for key+value pair ***
								var i = 0;
								if ((i = s.IndexOf('=')) > 0)
								{
									var j = (s.Length - i - 1);
									var Key = s.Substring(0, i).Trim();
									if (Key.Length > 0)
									{
										// *** Only first occurrence of a key is loaded ***
										if (!CurrentSection.ContainsKey(Key))
										{
											var Value = (j > 0) ? (s.Substring(i + 1, j).Trim()) : (string.Empty);
											CurrentSection.Add(Key, Value);
										}
									}
								}
							}
						}
					}
					catch
					{
						return;
					}
				}
			}
		}

		// *** Flush local cache content ***
		internal void Flush()
		{
			lock (m_Lock)
			{
				// *** If local cache was not modified, exit ***
				if (!m_CacheModified)
					return;

				m_CacheModified = false;

				// *** Open the file ***
				using (var sw = new StreamWriter(m_FileName))
				{
					try
					{
						// *** Cycle on all sections ***
						var First = false;
						foreach (KeyValuePair<string, Dictionary<string, string>> SectionPair in m_Sections)
						{
							var Section = SectionPair.Value;
							if (First)
								sw.WriteLine();

							First = true;

							// *** Write the section name ***
							sw.Write('[');
							sw.Write(SectionPair.Key);
							sw.WriteLine(']');

							// *** Cycle on all key+value pairs in the section ***
							foreach (KeyValuePair<string, string> ValuePair in Section)
							{
								// *** Write the key+value pair ***
								sw.Write(ValuePair.Key);
								sw.Write('=');
								sw.WriteLine(ValuePair.Value);
							}
						}
					}
					catch
					{
						return;
					}
				}
			}
		}

		// *** Read a value from local cache ***
		internal string GetValue(string SectionName, string Key, string DefaultValue)
		{
			// *** Lazy loading ***
			if (m_Lazy)
			{
				m_Lazy = false;
				Refresh();
			}

			lock (m_Lock)
			{
				// *** Check if the section exists ***
				var Section = (Dictionary<string, string>)null;
				if (!m_Sections.TryGetValue(SectionName, out Section))
					return DefaultValue;

				// *** Check if the key exists ***
				var Value = string.Empty;
				if (!Section.TryGetValue(Key, out Value))
					return DefaultValue;

				// *** Return the found value ***
				return Value;
			}
		}

		// *** Insert or modify a value in local cache ***
		internal void SetValue(string SectionName, string Key, string Value)
		{
			// *** Lazy loading ***
			if (m_Lazy)
			{
				m_Lazy = false;
				Refresh();
			}

			lock (m_Lock)
			{
				// *** Flag local cache modification ***
				m_CacheModified = true;

				// *** Check if the section exists ***
				var Section = (Dictionary<string, string>)null;
				if (!m_Sections.TryGetValue(SectionName, out Section))
				{
					// *** If it doesn't, add it ***
					Section = new Dictionary<string, string>();
					m_Sections.Add(SectionName, Section);
				}

				// *** Modify the value ***
				if (Section.ContainsKey(Key)) Section.Remove(Key);
				Section.Add(Key, Value);
			}
		}

		// *** Encode byte array ***
		private string EncodeByteArray(byte[] Value)
		{
			if (Value == null) return null;

			var sb = new StringBuilder();
			foreach (var b in Value)
			{
				var hex = Convert.ToString(b, 16);
				var l = hex.Length;
				if (l > 2)
					sb.Append(hex.Substring(l - 2, 2));
				else
				{
					if (l < 2)
						sb.Append("0");

					sb.Append(hex);
				}
			}

			return sb.ToString();
		}

		// *** Decode byte array ***
		private byte[] DecodeByteArray(string Value)
		{
			if (Value == null)
				return null;

			var l = Value.Length;
			if (l < 2)
				return new byte[0];

			l /= 2;
			var Result = new byte[l];
			for (var i = 0; i < l; i++)
				Result[i] = Convert.ToByte(Value.Substring(i * 2, 2), 16);

			return Result;
		}

		// *** Getters for various types ***
		internal bool GetValue(string SectionName, string Key, bool DefaultValue)
		{
			var StringValue = GetValue(SectionName, Key, DefaultValue.ToString(CultureInfo.InvariantCulture));
			var Value = 0;
			if (int.TryParse(StringValue, out Value))
				return (Value != 0);

			return DefaultValue;
		}

		internal int GetValue(string SectionName, string Key, int DefaultValue)
		{
			var StringValue = GetValue(SectionName, Key, DefaultValue.ToString(CultureInfo.InvariantCulture));
			var Value = 0;
			if (int.TryParse(StringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out Value))
				return Value;

			return DefaultValue;
		}

		internal double GetValue(string SectionName, string Key, double DefaultValue)
		{
			var StringValue = GetValue(SectionName, Key, DefaultValue.ToString(CultureInfo.InvariantCulture));
			var Value = 0D;
			if (double.TryParse(StringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out Value))
				return Value;

			return DefaultValue;
		}

		internal byte[] GetValue(string SectionName, string Key, byte[] DefaultValue)
		{
			var StringValue = GetValue(SectionName, Key, EncodeByteArray(DefaultValue));
			try
			{
				return DecodeByteArray(StringValue);
			}
			catch (FormatException)
			{
				return DefaultValue;
			}
		}

		// *** Setters for various types ***
		internal void SetValue(string SectionName, string Key, bool Value)
		{
			SetValue(SectionName, Key, (Value) ? ("1") : ("0"));
		}

		internal void SetValue(string SectionName, string Key, int Value)
		{
			SetValue(SectionName, Key, Value.ToString(CultureInfo.InvariantCulture));
		}

		internal void SetValue(string SectionName, string Key, double Value)
		{
			SetValue(SectionName, Key, Value.ToString(CultureInfo.InvariantCulture));
		}

		internal void SetValue(string SectionName, string Key, byte[] Value)
		{
			SetValue(SectionName, Key, EncodeByteArray(Value));
		}

		#endregion

	}
}
