using Bootpd.Network.Packet;
using Bootpd.Network.Sockets;
using Server.Network;
using System;
using System.Net;

namespace Bootpd.Network.Client
{
	public abstract partial class BaseClient : IClient
	{
		public BaseSocket Socket { get; set; }

		public bool LocalInstance { get; private set; } = false;
		public IPEndPoint RemoteEndpoint { get; set; }

		public Guid Id { get; }

		public BaseClient(ServerType type)
		{
			Id = Guid.NewGuid();
		}

		public ServerType ServerType { get; set; }

		public BaseClient()
		{

		}

		public BaseClient(ServerType type, IPEndPoint endpoint, bool localInstance)
		{
			LocalInstance = localInstance;
			RemoteEndpoint = endpoint;

			Id = Guid.NewGuid();

			if (LocalInstance)
			{
				switch (type)
				{
					case ServerType.BOOTP:
					case ServerType.DHCP:
						Socket = new BaseSocket(System.Net.Sockets.SocketType.Dgram, type, new System.Net.IPEndPoint(IPAddress.Any, 68));
						break;
					case ServerType.TFTP:
						var cPort = new Random(1).Next(40000, 50000);
						Socket = new BaseSocket(System.Net.Sockets.SocketType.Dgram, type, new System.Net.IPEndPoint(IPAddress.Any, cPort));
						break;
					default:
						break;
				}

				Socket.SocketDataReceived += (sender, e) =>
				{
					Console.WriteLine("[D] LocalClient: Got {1} packet from {0}!", e.RemoteEndpoint, type);
					RemoteEndpoint = e.RemoteEndpoint;
					ClientDataReceived?.Invoke(this, new ClientDataReceivedEventArgs(Id, e.Socket, e.RemoteEndpoint, e.Data));
				};
			}
		}

		public void Bootstrap()
		{
			if (!LocalInstance)
				return;

			Socket.Bootstrap();
		}

		public void Dispose()
		{
			Socket.Dispose();
		}

		public void Start()
		{
			if (!LocalInstance)
				return;

			Socket.Start();
		}

		public void Stop()
		{
			Socket.Stop();
		}

		public void Send(IPacket packet)
		{
			if (!LocalInstance)
				return;

			Socket.Send(RemoteEndpoint, packet);
		}

		public virtual void HeartBeat()
		{

		}
	}
}
