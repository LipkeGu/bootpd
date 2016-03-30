namespace bootpd
{
	using System;
	using System.Globalization;
	using System.Linq;
	using System.Net;
	using System.Net.Sockets;
	using System.Text;

	public static class Exts
	{
		public static string F(this string fmt, params object[] objs) => string.Format(fmt, objs);

		public static string ToBase64(string s) => Convert.ToBase64String(StringToByte(s));

		public static string FromBase64(string s) => Encoding.ASCII.GetString(Convert.FromBase64String(s));

		public static int HexToInt(string input) => int.Parse(input, NumberStyles.HexNumber);

		public static int HexToInt(byte input) => int.Parse(F("{0:x2}", input), NumberStyles.HexNumber);

		public static byte[] IntToHex(int input)
		{
			var result = new byte[1];
			result[0] = byte.Parse(F("{0:x2}", input), NumberStyles.HexNumber);

			return result;
		}

		public static string GetDataAsString(byte[] input, int index, int length, string delimeter = "")
		{
			var value = new byte[length];
			Functions.CopyTo(ref input, index, ref value, 0, length);

			var fmt = string.Empty;
			for (var i = 0; i < length; i++)
				fmt += F("{0:x2}{1}", value[i], delimeter);

			return fmt.ToUpperInvariant();
		}

		public static string GetGuidAsString(byte[] guidBytes, int index, int length, bool patch)
		{
			var guid = new byte[length];
			var fmt = string.Empty;

			Functions.CopyTo(ref guidBytes, index, ref guid, 0, length);

			for (var i = 0; i < length; i++)
			{
				if (i == 0)
					fmt += F("{0:x2}", guid[i]);
				else if (i == 4)
					fmt += F("{0:x2}{1}", guid[i], "-");
				else if (i == 6)
					fmt += F("{0:x2}{1}", guid[i], "-");
				else if (i == 8)
					fmt += F("{0:x2}{1}", guid[i], "-");
				else if (i == 10)
					fmt += F("{0:x2}{1}", guid[i], "-");
				else
					fmt += F("{0:x2}", guid[i]);
			}

			return patch ? fmt.Remove(0, 2) : fmt;
		}

		public static byte[] GetOptionValue(byte[] data, Definitions.DHCPOptionEnum option)
		{
			var value = new byte[1];
			value[0] = byte.MaxValue;

			try
			{
				for (var i = 0; i < data.Length; i++)
					if (HexToInt(data[i]) == (int)option)
					{
						var length = HexToInt(data[i + 1]);
						value = new byte[length];

						if (value.Length <= length && data.Length > length)
							Functions.CopyTo(ref data, i + 2, ref value, 0, value.Length);
						break;
					}

				return value;
			}
			catch (Exception)
			{
				value[0] = byte.MaxValue;

				return value;
			}
		}

		public static byte[] SetDHCPOption(Definitions.DHCPOptionEnum option, byte[] value)
		{
			var opt = new byte[(2 + value.Length)];

			opt[0] = IntToHex((byte)option)[0];
			opt[1] = IntToHex(value.Length)[0];

			Array.Copy(value, 0, opt, 2, opt.Length - 2);

			return opt;
		}

		public static byte[] StringToByte(string input) => Encoding.ASCII.GetBytes(input.ToCharArray());

		public static IPAddress GetServerIP() => (from a in Dns.GetHostEntry(Dns.GetHostName()).AddressList where a.AddressFamily == AddressFamily.InterNetwork select a).FirstOrDefault();

		public static string Replace(string input, string oldValue, string newValue) => input.Replace(oldValue, newValue);

		public static byte[] Replace(byte[] input, string oldValue, string newValue)
		{
			var tmp = Encoding.ASCII.GetString(input, 0, input.Length);
			tmp = Replace(tmp, oldValue, newValue);

			return StringToByte(tmp);
		}

		public static string[] ToParts(byte[] input, char[] seperator) => Encoding.ASCII.GetString(input, 2, input.Length - 2).Split(seperator);
	}
}