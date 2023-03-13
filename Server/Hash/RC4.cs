namespace bootpd
{
	using Bootpd;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	public static class RC4
	{
		public static string Encrypt(string key, string data, Encoding encoding) =>
			Exts.ToBase64(Encrypt(Exts.StringToByte(key, encoding), Exts.StringToByte(data, encoding)));

		public static byte[] Encrypt(byte[] key, byte[] data) => EncryptOutput(key, data).ToArray();

		public static string Decrypt(string key, string data, Encoding encoding) =>
			Exts.EncodeTo(Encrypt(Exts.StringToByte(key, encoding), Convert.FromBase64String(data)), encoding);

		public static byte[] Decrypt(byte[] key, byte[] data) => EncryptOutput(key, data).ToArray();

		private static byte[] EncryptInitalize(byte[] key)
		{
			var s = Enumerable.Range(0, 256).Select(i => Convert.ToByte(i)).ToArray();

			for (int i = 0, j = 0; i < 256; i++)
			{
				j = (j + key[i % key.Length] + s[i]) & byte.MaxValue;
				Swap(s, i, j);
			}

			return s;
		}

		private static IEnumerable<byte> EncryptOutput(byte[] key, IEnumerable<byte> data)
		{
			var s = EncryptInitalize(key);

			var i = 0;
			var j = 0;

			return data.Select((b) =>
			{
				i = (i + 1) & byte.MaxValue;
				j = (j + s[i]) & byte.MaxValue;

				Swap(s, i, j);

				return Convert.ToByte((b ^ s[(s[i] + s[j]) & byte.MaxValue]));
			});
		}

		private static void Swap(byte[] s, int i, int j)
		{
			var c = s[i];

			s[i] = s[j];
			s[j] = c;
		}
	}
}
