using Bootpd.Network.Sockets;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Bootpd.Network.Server
{
	public partial class BaseServer : IServer
	{
		public Guid Id { get; private set; }

		public string Hostname { get; private set; }
		public ServerType ServerType { get; private set; }

		public SocketType SocketType { get; private set; }

		public BaseServer(ServerType type, SocketType socketType)
		{

			Id = Guid.NewGuid();
			Hostname = Environment.MachineName;
			ServerType = type;
			SocketType = socketType;

			Sockets = new Dictionary<Guid, ISocket>();
		}

		public Dictionary<Guid, ISocket> Sockets { get; set; }
		public int Port { get; set; }

		public void AddSocket(IPEndPoint endpoint)
		{
			var socket = new BaseSocket(SocketType, Guid.NewGuid(), endpoint);
			socket.SocketDataReceived += (sender, e) =>
			{
				ServerDataReceived?.Invoke(this,
					new ServerDataReceivedEventArgs(Id, e.Socket, e.RemoteEndpoint, e.Data));
			};

			Sockets.Add(socket.Id, socket);
		}

		public void Bootstrap(int port)
		{
			switch (ServerType)
			{
				case ServerType.DHCP:
					Port = 67;
					break;
				case ServerType.BOOTP:
					Port = 4011;
					break;
				case ServerType.TFTP:
					port = 69;
					break;
				default:
					break;
			}

			foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
				foreach (var addr in netInterface.GetIPProperties().UnicastAddresses)
					AddSocket(new IPEndPoint(addr.Address, port));

			foreach (var socket in Sockets.Values)
				socket.Bootstrap();
		}

		public void Bootstrap()
		{
			foreach (var socket in Sockets.Values)
				socket.Bootstrap();
		}

		public void Dispose()
		{
			foreach (var socket in Sockets.Values)
				socket.Dispose();
		}

		public ISocket GetSocket(Guid id)
		{
			return Sockets[id];
		}

		public void RemoveSocket(Guid id)
		{
			lock (Sockets)
				Sockets.Remove(id);
		}

		public void Start()
		{
			foreach (var socket in Sockets.Values)
				socket.Start();
		}

		public void Stop()
		{
		}

		public void HeartBeat()
		{
		}
	}
}
