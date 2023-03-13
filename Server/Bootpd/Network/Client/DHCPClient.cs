using Server.Network;
using System.Collections.Generic;
using System.Net;
using DHCPPacket = Bootpd.Network.Packet.DHCPPacket;

namespace Bootpd.Network.Client
{
	public sealed class DHCPClient : BaseClient
	{
		public DHCPClient() : base()
		{
			ServerType = ServerType.DHCP;
		}

		public DHCPClient(ServerType serverType, IPEndPoint endpoint, bool localInstance) : base(serverType, endpoint, localInstance)
		{
		}

		public override void HeartBeat()
		{
			if (LocalInstance)
			{
				var reqList = new List<byte>() { 1, 3, 43, 60 };
				var packet = new DHCPPacket(Common.Network.Protocol.DHCP.DHCPMsgType.Discover);
				packet.AddOption(new Common.Network.Protocol.DHCP.DHCPOption(60, "PXEClient:Arch:00000:UNDI:002001"));
				packet.AddOption(new Common.Network.Protocol.DHCP.DHCPOption(57, ((ushort)1500).LE16()));
				packet.AddOption(new Common.Network.Protocol.DHCP.DHCPOption(97, Id));

				packet.AddOption(new Common.Network.Protocol.DHCP.DHCPOption(55, reqList.ToArray()));

				packet.AddOption(new Common.Network.Protocol.DHCP.DHCPOption(94, new byte[3] { 0x01, 0x02, 0x01 }));
				packet.CommitOptions();

				Socket.Send(new IPEndPoint(IPAddress.Broadcast, 67), packet);
			}
		}
	}
}
