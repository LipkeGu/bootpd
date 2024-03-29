﻿using Bootpd.Common;
using Bootpd.Network.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace Bootpd.Network.Server
{
	public abstract partial class BaseServer : IServer
	{
		#region "Mutexes"
		object __LocksocketsMutex = new object();
		#endregion

		public string Id { get; private set; }

		public string Hostname { get; private set; }
		public ServerType ServerType { get; private set; }

		public System.Net.Sockets.SocketType SocketType { get; private set; }

		public BaseServer(ServerType type)
		{
			Id = Guid.NewGuid().ToString();
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

			Sockets = new Dictionary<string, BaseSocket>();
		}

		public Dictionary<string, BaseSocket> Sockets { get; set; }
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
							if (addr.SuffixOrigin != SuffixOrigin.WellKnown)
								AddSocket(new IPEndPoint(addr.Address, Port));

			lock (__LocksocketsMutex)
			{
				for (var i = Sockets.Count - 1; i >= 0; i--)
					Sockets.ElementAt(i).Value.Bootstrap();
			}
		}

		public void Dispose()
		{
			lock (__LocksocketsMutex)
			{
				for (var i = Sockets.Count - 1; i >= 0; i--)
					Sockets.ElementAt(i).Value.Dispose();
			}
		}

		public BaseSocket GetSocket(string id)
		{
			lock (__LocksocketsMutex)
			{
				return Sockets[id];
			}
		}

		public void RemoveSocket(string id)
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
				for (var i = Sockets.Count - 1; i >= 0; i--)
					Sockets.ElementAt(i).Value.Start();
			}
		}

		public void Stop()
		{
			lock (__LocksocketsMutex)
			{
				for (var i = Sockets.Count - 1; i >= 0; i--)
					Sockets.ElementAt(i).Value.Stop();
			}
		}

		public void HeartBeat()
		{
			lock (__LocksocketsMutex)
			{
				for (var i = Sockets.Count - 1; i >= 0; i--)
					Sockets.ElementAt(i).Value.HeartBeat();
			}
		}
	}
}
