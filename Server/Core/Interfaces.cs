namespace bootpd
{
	using System;
	using System.IO;
	using static Definitions;
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
		void Handle_RRQ_Request(object packet);

		void Handle_ACK_Request(object data);
	}

	interface ITFTPClient_Provider
	{
		FileStream FileStream
		{
			get; set;
		}

		BufferedStream BufferedStream
		{
			get; set;
		}

		ushort MSFTWindow
		{
			get; set;
		}

		bool WindowSizeMode
		{
			get;  set;
		}

		ushort Blocks
		{
			get; set;
		}

		long BytesRead
		{
			get; set;
		}

		TFTPMode Mode
		{
			get; set;
		}

		long TransferSize
		{
			get; set;
		}

		string FileName
		{
			get; set;
		}

		TFTPStage Stage
		{
			get; set;
		}
	}
}
