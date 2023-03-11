using Bootpd.Network.Server;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Bootpd
{
	public class BootpdCommon
	{
		Dictionary<Guid, IServer> Servers;


		public BootpdCommon(string[] args)
		{
			Servers = new Dictionary<Guid, IServer>();
		}


		public void AddServer(ServerType type)
		{
			var server = new BaseServer(type);





			Servers.Add(server.Id, server);
		}


		public void Bootstrap()
		{
			AddServer(ServerType.DHCP);

			foreach (var server in Servers.Values)
				server.Bootstrap();
		}

		public void Start()
		{
			foreach (var server in Servers.Values)
				server.Start();
		}

		public void Stop()
		{
			foreach (var server in Servers.Values)
				server.Stop();
		}

		public void Dispose()
		{
			foreach (var server in Servers.Values)
				server.Dispose();
		}
	}
}
