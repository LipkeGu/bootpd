using Server.Network;
using System;

namespace Bootpd.Network.Server
{
	public sealed class DHCPServer : BaseServer
	{
		public DHCPServer(ServerType serverType) : base(serverType)
		{

		}

		public void Handle_Discover_Request(Guid client, Guid socket, Packet.DHCPPacket request)
		{
			var response = request.CreateResponse(GetSocket(socket).ServerIP);
			response.Bootfile = "wdsnbp.com";
			response.CommitOptions();

			Sockets[socket].Send(BootpdCommon.Clients[client].RemoteEndpoint, response);
		}

		public void Handle_Request_Request(Guid client, Guid socket, Packet.DHCPPacket request)
		{
			var response = request.CreateResponse(GetSocket(socket).ServerIP);
			response.Bootfile = "wdsnbp.com";
			response.CommitOptions();

			Sockets[socket].Send(BootpdCommon.Clients[client].RemoteEndpoint, response);
		}

		public void Handle_Inform_Request(Guid client, Guid socket, Packet.DHCPPacket request)
		{
			var response = request.CreateResponse(GetSocket(socket).ServerIP);
			response.CommitOptions();

			Sockets[socket].Send(BootpdCommon.Clients[client].RemoteEndpoint, response);
		}
	}
}
