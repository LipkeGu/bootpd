namespace Server.Network
{
	using Bootpd.Common.Network.Protocol.DHCP;
	using Bootpd.Common.Network.Protocol.TFTP;
	using System;
	using System.IO;
	using System.Net;

	interface IDHCPServer_Provider
	{
	}

	interface IDHCPClient_Provider
	{
		Guid Guid
		{
			get; set;
		}

		DHCPMsgType MsgType
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

		void Handle_ERR_Request(TFTPErrorCode error, string message, IPEndPoint client, bool clientError = false);
	}

	interface ITFTPClient_Provider
	{
		FileStream FileStream
		{
			get; set;
		}

		ushort MSFTWindow
		{
			get; set;
		}

		bool WindowSizeMode
		{
			get; set;
		}

		ushort Blocks
		{
			get; set;
		}

		ushort BlockSize
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

	interface IRISClient_Provider
	{
		RISOPCodes OPCode
		{
			get; set;
		}

		bool Workstation_Supplied
		{
			get; set;
		}

		bool Domain_Supplied
		{
			get; set;
		}

		bool NTLMSSP_NEGOTIATE_SEAL
		{
			get; set;
		}

		bool NTLMSSP_NEGOTIATE_SIGN
		{
			get; set;
		}
	}
}
