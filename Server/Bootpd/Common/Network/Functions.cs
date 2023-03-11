using System.Net;

namespace Bootpd.Network
{
	public static class Functions
	{
		public static string GetHostName(this IPAddress ipAddress)
		{
			var entry = Dns.GetHostEntry(ipAddress);
			return (entry != null) ? entry.HostName : ipAddress.ToString();
		}
	}
}
