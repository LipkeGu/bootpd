using Server.Network;
using System;
using System.Net;

namespace Bootpd.Network.Server
{
	public sealed class DHCPServer : BaseServer
	{
		public DHCPServer(ServerType serverType) : base(serverType)
		{

		}

		public void Handle_Discover_Request(Guid socket, Packet.DHCPPacket request)
		{
			var response = request.CreateResponse(GetSocket(socket).ServerIP);
			response.Bootfile = "\\Boot\\x86\\wdsnbp.com";
			response.CommitOptions();

			Sockets[socket].Send(new IPEndPoint(IPAddress.None, 68), response);
		}

		public void Handle_Request_Request(Guid socket, Packet.DHCPPacket request)
		{
			var response = request.CreateResponse(GetSocket(socket).ServerIP);
			response.Bootfile = "\\Boot\\x86\\wdsnbp.com";
			response.CommitOptions();

			Sockets[socket].Send(new IPEndPoint(response.Flags == BootpFlags.Unicast ?
				response.ClientIP : IPAddress.Broadcast, 68), response);
		}

		public void Handle_Inform_Request(Guid socket, Packet.DHCPPacket request)
		{
			var response = request.CreateResponse(GetSocket(socket).ServerIP);
			response.CommitOptions();

			Sockets[socket].Send(new IPEndPoint(response.ClientIP, 68), response);
		}
	}
}
