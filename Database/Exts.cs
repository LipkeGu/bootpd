namespace Server.Extensions
{
	using System;
	using System.Linq;
	using System.Net;
	using System.Net.Sockets;
	using System.Text;

	public static class Exts
	{
		public static string F(this string fmt, params object[] objs) => string.Format(fmt, objs);

		public static string ToBase64(string s) => Convert.ToBase64String(StringToByte(s, Encoding.ASCII));
		public static string ToBase64(string s, Encoding encoding) => Convert.ToBase64String(StringToByte(s, encoding));
		public static string ToBase64(byte[] b) => Convert.ToBase64String(b);

		public static string FromBase64(string s, Encoding encoding)
		{
			var x = Convert.FromBase64String(s);
			return BytesToString(x, encoding, 0, x.Length);
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

		public static string EncodeTo(string data, Encoding src, Encoding target)
		{
			return target.GetString(src.GetBytes(data));
		}

		public static string EncodeTo(byte[] data, Encoding target, int index = 0, int count = 0)
		{
			var x = target.GetString(data, index, count == 0 ? data.Length : count);
			
			return x;
		}

		public static string GetGuidAsString(byte[] guid, int length, bool patch)
		{
			var fmt = string.Empty;
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

			if (fmt.Length > 2)
				return patch ? fmt.Remove(0, 2) : fmt;
			else
				return null;
		}

		public static byte[] GetOptionValue(byte[] data, int option)
		{
			var opt = option;
			for (var i = 0; i < data.Length; i++)
				if (Convert.ToInt32(data[i]) == opt)
				{
					var value = new byte[Convert.ToInt32(data[i + 1])];
					Functions.CopyTo(ref data, i + 2, ref value, 0, value.Length);

					return value;
				}

			return new byte[1] { byte.MaxValue };
		}

		public static byte[] SetDHCPOption(int option, byte[] value, bool includesendoption = false)
		{
			var opt = new byte[(2 + value.Length)];
			var len = Convert.ToByte(value.Length);
			opt[0] = Convert.ToByte(option);

			if (includesendoption)
				len += 1;

			opt[1] = len;
			Functions.CopyTo(ref value, 0, ref opt, 2, opt.Length - 2);

			return opt;
		}

		public static byte[] StringToByte(string input, Encoding encoding) => encoding.GetBytes(input.ToCharArray());

		public static string BytesToString(byte[] input, Encoding encoding, int index = 0, int count = 0) => encoding.GetString(input, index, count);


		public static IPAddress GetServerIP() => (
			from a in Dns.GetHostEntry(Dns.GetHostName()).AddressList
			where a.AddressFamily == AddressFamily.InterNetwork
			select a).FirstOrDefault();

		public static string Replace(string input, string oldValue, string newValue) => input.Replace(oldValue, newValue);

		public static byte[] Replace(byte[] input, string oldValue, string newValue, Encoding encoding)
		{
			return StringToByte(Replace(encoding.GetString(input, 0, input.Length), oldValue, newValue), encoding);
		}

		public static byte[] UINT32ToBytes(uint input, bool reverse = false)
		{
			var data = BitConverter.GetBytes(input);

			if (reverse)
				Array.Reverse(data);

			return data;
		}

		public static byte[] UINT16ToBytes(ushort input, bool reverse = false)
		{
			var data = BitConverter.GetBytes(input);

			if (reverse)
				Array.Reverse(data);

			return data;
		}

		public static string[] ToParts(byte[] input, string seperator, Encoding encoding) => encoding.GetString(input, 2, input.Length - 2).Split(seperator.ToCharArray());
	}
}
