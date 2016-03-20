namespace WDSServer.Providers
{
	using System;
	using System.Net;
	using WDSServer.Network;

	interface IDHCPServer_Provider
	{
		void Handle_DHCP_Request(DHCPPacket data, ref DHCPClient client);
	}

	interface IDHCPClient_Provider
	{
		Guid Guid
		{
			get; set;
		}

		Definitions.DHCPMsgType MsgType
		{
			get; set;
		}

		string BootFile
		{
			get; set;
		}
	}

	interface ITFTPServer_Provider
	{
		void Handle_RRQ_Request(TFTPPacket packet, IPEndPoint client);

		void Handle_ACK_Request(TFTPPacket data, IPEndPoint client);
	}
}
