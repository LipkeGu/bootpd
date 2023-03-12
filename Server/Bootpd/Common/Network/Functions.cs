using System;
using System.Net;

namespace bootpd
{
	public static class Functions
	{
		public static string GetHostName(this IPAddress ipAddress)
		{
			var entry = Dns.GetHostEntry(ipAddress);
			return (entry != null) ? entry.HostName : ipAddress.ToString();
		}

		public static int CopyTo(byte[] src, int srcoffset, byte[] dst, int dstoffset = 0, int length = 0)
		{
			var len = length == 0 ? src.Length : length;
			Array.Copy(src, srcoffset, dst, dstoffset, len);

			return len;
		}
	}
}
