using Bootpd.Network.Packet;
using System;
using System.Net;

namespace Bootpd.Network.Server
{
	public partial class BaseServer
	{
		public delegate void ServerDataReceivedEventHandler(object sender, ServerDataReceivedEventArgs e);
		public event ServerDataReceivedEventHandler ServerDataReceived;

		public class ServerDataReceivedEventArgs
		{

			public IPacket Data { get; }
			public Guid Socket { get; }
			public Guid Server { get; }

			public IPEndPoint RemoteEndpoint { get; }

			public ServerDataReceivedEventArgs(Guid server, Guid socket, IPEndPoint endPoint, IPacket data)
			{
				Socket = socket;
				Server = server;
				RemoteEndpoint = endPoint;
				Data = data;
			}
		}
	}
}
