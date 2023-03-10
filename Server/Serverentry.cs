namespace Server.Network
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;


	public class BootServer
	{
		public List<IPAddress> Addresses
		{
			get; private set;
		}

		public string Hostname { get; private set; }
		public BootServerTypes Type { get; private set; }

		public BootServer(string hostname, BootServerTypes type = BootServerTypes.CAUnicenterTNGBootServer)
		{
			Type = type;
			Hostname = hostname;
			Addresses = Functions.DNSLookup(Hostname)
				.Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToList();
		}

		public BootServer(IPAddress addr, BootServerTypes bootServerType = BootServerTypes.PXEBootstrapServer)
		{
			Hostname = addr.ToString();
			Type = bootServerType;

			Addresses = new List<IPAddress>
			{
				addr
			};
		}
	}
}
