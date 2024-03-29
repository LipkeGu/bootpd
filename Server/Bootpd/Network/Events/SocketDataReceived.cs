﻿using Bootpd.Network.Packet;
using System.Net;

namespace Bootpd.Network.Sockets
{
	public partial class BaseSocket
	{
		public delegate void SocketDataReceivedEventHandler(object sender, SocketDataReceivedEventArgs e);
		public event SocketDataReceivedEventHandler SocketDataReceived;

		public class SocketDataReceivedEventArgs
		{
			public IPacket Data { get; }
			public string Socket { get; }

			public IPEndPoint RemoteEndpoint { get; }

			public SocketDataReceivedEventArgs(string id, IPEndPoint endPoint, IPacket data)
			{
				Socket = id;
				RemoteEndpoint = endPoint;
				Data = data;
			}
		}
	}
}
