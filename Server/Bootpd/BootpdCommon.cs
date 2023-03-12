using Bootpd.Network.Client;
using Bootpd.Network.Server;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Bootpd
{
	public class BootpdCommon
	{
		#region "Mutexes"
		object __LockServersMutex = new object();
		object __LockClientsMutex = new object();
		#endregion

		readonly Dictionary<Guid, IServer> Servers;
		readonly Dictionary<Guid, IClient> Clients;


		public BootpdCommon(string[] args)
		{
			Servers = new Dictionary<Guid, IServer>();
			Clients = new Dictionary<Guid, IClient>();
		}

		public void AddServer(ServerType type)
		{
			BaseServer server = new BaseServer(type);

			server.ServerDataReceived += (sender, e) =>
			{
				Console.WriteLine("Got Packet!");
			};

			Servers.Add(server.Id, server);
		}

		public void AddClient(ServerType type)
		{
			BaseClient client = null;

			switch (type)
			{
				case ServerType.BOOTP:
				case ServerType.DHCP:
					client = new Network.Client.DHCPClient(type, true);
					break;
				case ServerType.TFTP:
					break;
				default:
					break;
			}

			Clients.Add(client.Id, client);
			Console.WriteLine("[D] Local Client added!");
		}

		public void Bootstrap()
		{
			AddServer(ServerType.DHCP);
			AddServer(ServerType.BOOTP);
			AddServer(ServerType.TFTP);


			lock (__LockServersMutex)
			{
				foreach (var server in Servers.Values)
					server.Bootstrap();
			}

			AddClient(ServerType.DHCP);

			lock (__LockClientsMutex)
			{
				foreach (var client in Clients.Values)
					client.Bootstrap();
			}
		}
		public void Start()
		{
			lock (__LockServersMutex)
			{
				foreach (var server in Servers.Values)
					server.Start();
			}

			lock (__LockClientsMutex)
			{
				foreach (var client in Clients.Values)
					client.Start();
			}
		}
		public void Stop()
		{
			lock (__LockServersMutex)
			{
				foreach (var server in Servers.Values)
					server.Stop();
			}

			lock (__LockClientsMutex)
			{
				foreach (var client in Clients.Values)
					client.Stop();
			}
		}

		public void Heartbeat()
		{
			lock (__LockServersMutex)
			{
				foreach (var server in Servers.Values)
					server.HeartBeat();
			}

			lock (__LockClientsMutex)
			{
				foreach (var client in Clients.Values)
					client.HeartBeat();
			}
		}

		public void Dispose()
		{
			lock (__LockServersMutex)
			{
				foreach (var server in Servers.Values)
					server.Dispose();
			}

			lock (__LockClientsMutex)
			{
				foreach (var client in Clients.Values)
					client.Dispose();
			}
		}
	}
}
