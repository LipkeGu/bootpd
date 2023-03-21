using Bootpd.Common;
using Bootpd.Network.Packet;
using Bootpd.Network.Sockets;
using System;
using System.Net;

namespace Bootpd.Network.Client
{
	public abstract partial class BaseClient : IClient
	{
		public BaseSocket Socket { get; set; }

		public bool LocalInstance { get; set; } = false;
		public IPEndPoint RemoteEndpoint { get; set; }

		public string Id { get; }

		public BaseClient(string id, ServerType type)
		{
			Id = id == string.Empty ? Guid.NewGuid().ToString() : id;

		}

		public ServerType ServerType { get; set; }

		public BaseClient()
		{

		}

		public BaseClient(string id, ServerType type, IPEndPoint endpoint, bool localInstance)
		{
			LocalInstance = localInstance;
			Id = id == string.Empty ? Guid.NewGuid().ToString() : id;

			if (LocalInstance)
			{
				RemoteEndpoint = endpoint;
				switch (type)
				{
					case ServerType.BOOTP:
					case ServerType.DHCP:
						Socket = new BaseSocket(System.Net.Sockets.SocketType.Dgram, type, new IPEndPoint(IPAddress.Any, 68));
						break;
					case ServerType.TFTP:
						var cPort = new Random(1).Next(40000, 50000);
						Socket = new BaseSocket(System.Net.Sockets.SocketType.Dgram, type, new IPEndPoint(IPAddress.Any, cPort));
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
			else
			{
				RemoteEndpoint = endpoint;
				if (RemoteEndpoint.Address.Equals(IPAddress.Any))
					RemoteEndpoint.Address = IPAddress.Broadcast;
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
			if (Socket == null)
				return;

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
			if (Socket == null)
				return;

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
