using Server.Network;
using System;

namespace Bootpd.Network.Server
{
	public class TFTPServer : BaseServer
	{
		public TFTPServer(ServerType serverType) : base(serverType)
		{

		}

		public void Handle_Read_Request(Guid client, TFTPPacket request)
		{

		}

		public void Handle_Ack_Request(Guid client, TFTPPacket request)
		{

		}
	}
}
