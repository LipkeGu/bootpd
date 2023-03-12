using Server.Network;
using System;
using System.Net.Sockets;

namespace Bootpd.Network.Client
{
	public class BaseClient : IClient
	{
		public Guid Id { get; private set; }

		public BaseClient(ServerType type, SocketType socketType)
		{
			Id = Guid.NewGuid();
		}

		public void Bootstrap(int port)
		{
		}

		public void Bootstrap()
		{
		}

		public void Dispose()
		{
		}

		public void Start()
		{
		}

		public void Stop()
		{
		}
	}
}
