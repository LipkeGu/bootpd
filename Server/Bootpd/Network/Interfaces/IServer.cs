using Bootpd.Network.Sockets;
using System;
using System.Collections.Generic;
using System.Net;

namespace Bootpd.Network.Server
{
	public interface IServer : IDisposable
	{
		ushort Port { get; set; }
		Dictionary<Guid, ISocket> Sockets { get; set; }
		void Bootstrap();
		void Start();
		void Stop();
		void HeartBeat();

		void AddSocket(IPEndPoint endpoint);
		void RemoveSocket(Guid id);
		ISocket GetSocket(Guid id);
	}
}
