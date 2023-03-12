using Bootpd.Network.Packet;
using System;
using System.Net;

namespace Bootpd.Network.Client
{
	public abstract partial class BaseClient
	{
		public delegate void ClientDataReceivedEventHandler(object sender, ClientDataReceivedEventArgs e);
		public event ClientDataReceivedEventHandler ClientDataReceived;

		public class ClientDataReceivedEventArgs
		{
			public IPacket Data { get; }
			public Guid Socket { get; }
			public Guid Client { get; }

			public IPEndPoint RemoteEndpoint { get; }

			public ClientDataReceivedEventArgs(Guid client, Guid socket, IPEndPoint endPoint, IPacket data)
			{
				Socket = socket;
				Client = client;
				RemoteEndpoint = endPoint;
				Data = data;
			}
		}
	}
}
