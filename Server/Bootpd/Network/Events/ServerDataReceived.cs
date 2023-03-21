using Bootpd.Network.Packet;
using System.Net;

namespace Bootpd.Network.Server
{
	public abstract partial class BaseServer
	{
		public delegate void ServerDataReceivedEventHandler(object sender, ServerDataReceivedEventArgs e);
		public event ServerDataReceivedEventHandler ServerDataReceived;

		public class ServerDataReceivedEventArgs
		{

			public IPacket Data { get; }
			public string Socket { get; }
			public string Server { get; }

			public IPEndPoint RemoteEndpoint { get; }

			public ServerDataReceivedEventArgs(string server, string socket, IPEndPoint endPoint, IPacket data)
			{
				Socket = socket;
				Server = server;
				RemoteEndpoint = endPoint;
				Data = data;
			}
		}
	}
}
