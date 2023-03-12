using Bootpd.Network.Sockets;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

namespace Bootpd.Network.Server
{
	public partial class BaseServer : IServer
	{
		#region "Mutexes"
		object __LocksocketsMutex = new object();
		#endregion

		public Guid Id { get; private set; }

		public string Hostname { get; private set; }
		public ServerType ServerType { get; private set; }

		public System.Net.Sockets.SocketType SocketType { get; private set; }

		public BaseServer(ServerType type)
		{
			Id = Guid.NewGuid();
			Hostname = Environment.MachineName;
			ServerType = type;

			switch (ServerType)
			{
				case ServerType.BOOTP:
				case ServerType.DHCP:
				case ServerType.TFTP:
					SocketType = System.Net.Sockets.SocketType.Dgram;
					break;
				default:
					SocketType = System.Net.Sockets.SocketType.Raw;
					break;
			}

			Sockets = new Dictionary<Guid, ISocket>();
		}

		public Dictionary<Guid, ISocket> Sockets { get; set; }
		public ushort Port { get; set; }

		public void AddSocket(IPEndPoint endpoint)
		{
			lock (__LocksocketsMutex)
			{
				var socket = new BaseSocket(SocketType, ServerType, endpoint);
				socket.SocketDataReceived += (sender, e) =>
				{
					ServerDataReceived?.Invoke(this,
						new ServerDataReceivedEventArgs(Id, e.Socket, e.RemoteEndpoint, e.Data));
				};

				Sockets.Add(socket.Id, socket);
			}

			Console.WriteLine("Added Socket {0}...", endpoint);
		}

		public void Bootstrap()
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
					Port = 69;
					break;
				default:
					break;
			}

			foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
				foreach (var addr in netInterface.GetIPProperties().UnicastAddresses)
					if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
						if (addr.SuffixOrigin != SuffixOrigin.LinkLayerAddress)
							AddSocket(new IPEndPoint(addr.Address, Port));

			lock (__LocksocketsMutex)
			{
				foreach (var socket in Sockets.Values)
					socket.Bootstrap();
			}
		}

		public void Dispose()
		{
			lock (__LocksocketsMutex)
			{
				foreach (var socket in Sockets.Values)
					socket.Dispose();
			}
		}

		public ISocket GetSocket(Guid id)
		{
			lock (__LocksocketsMutex)
			{
				return Sockets[id];
			}
		}

		public void RemoveSocket(Guid id)
		{
			lock (__LocksocketsMutex)
			{
				if (Sockets.ContainsKey(id))
				{
					Sockets[id].Stop();
					Sockets[id].Dispose();
					Sockets.Remove(id);
				}
			}
		}

		public void Start()
		{
			lock (__LocksocketsMutex)
			{
				foreach (var socket in Sockets.Values)
					socket.Start();
			}
		}

		public void Stop()
		{
			lock (__LocksocketsMutex)
			{
				foreach (var socket in Sockets.Values)
					socket.Stop();
			}
		}

		public void HeartBeat()
		{
			lock (__LocksocketsMutex)
			{
				foreach (var socket in Sockets.Values)
					socket.HeartBeat();
			}
		}
	}
}
