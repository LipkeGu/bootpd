using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Server.Network
{
	public enum DHCPMsgType : byte
	{
		Discover = 1,
		Offer = 2,
		Request = 3,
		Decline = 4,
		Ack = 5,
		Nak = 6,
		Release = 7,
		Inform = 8
	}

	/// <summary>
	/// Options used by the WDSNBPOptions.Architecture
	/// </summary>
	public enum Architecture : ushort
	{
		Intelx86PC = 0,
		NEC_PC98 = 1,
		EFIItanium = 2,
		DECAlpha = 3,
		Arcx86 = 4,
		IntelLeanClient = 5,
		EFIIA32 = 6,
		EFIBC = 7,
		EFIXscale = 8,
		EFIx8664 = 9
	}

	public enum TFTPOPCodes : ushort
	{
		UNK = 0,

		RRQ = 1,
		WRQ = 2,

		DAT = 3,
		ACK = 4,
		ERR = 5,
		OCK = 6
	}

	public enum TFTPErrorCode : ushort
	{
		Unknown,
		FileNotFound,
		AccessViolation,
		DiskFull,
		IllegalOperation,
		UnknownTID,
		FileExists,
		NoSuchUser,
		InvalidOption
	}

	/// <summary>
	/// ClientTypes
	/// </summary>
	public enum SocketType
	{
		TFTP = 0,
		DHCP = 1,
		BINL = 2,
		RIS = 3
	}

	/// <summary>
	/// TFTP Stage Indicator Types
	/// </summary>
	public enum TFTPStage
	{
		Handshake,
		Transmitting,
		Done,
		Error,
	}

	public enum RISOPCodes
	{
		NEG = 1,
		CHL = 2,
		AUT = 3,
		RQU = 4,
		RSU = 5,
		REQ = 6,
		RSP = 7,
		NCQ = 8,
		RES = 9,
		OFF = 10,
		NCR = 11
	}

	public enum VendorIdents
	{
		PXEClient,
		PXEServer,
		BSDP /* Apple */
	}

	// TODO: This List is not complete!
	public enum DHCPOptionEnum : byte
	{
		Pad = byte.MinValue,
		SubnetMask = 1,
		TimeOffset = 2,
		Router = 3,
		TimeServer = 4,
		NameServer = 5,
		DomainNameServer = 6,
		LogServer = 7,
		CookieServer = 8,
		LPRServer = 9,
		ImpressServer = 10,
		ResourceLocServer = 11,
		ClientHostName = 12,
		BootFileSize = 13,
		MeritDump = 14,
		DomainName = 15,
		SwapServer = 16,
		RootPath = 17,
		ExtensionsPath = 18,
		IpForwarding = 19,
		NonLocalSourceRouting = 20,
		PolicyFilter = 21,
		MaximumDatagramReAssemblySize = 22,
		DefaultIPTimeToLive = 23,
		PathMTUAgingTimeout = 24,
		PathMTUPlateauTable = 25,
		InterfaceMTU = 26,
		AllSubnetsAreLocal = 27,
		BroadcastAddress = 28,
		PerformMaskDiscovery = 29,
		MaskSupplier = 30,
		PerformRouterDiscovery = 31,
		RouterSolicitationAddress = 32,
		StaticRoute = 33,
		TrailerEncapsulation = 34,
		ARPCacheTimeout = 35,
		EthernetEncapsulation = 36,
		TCPDefaultTTL = 37,
		TCPKeepaliveInterval = 38,
		TCPKeepaliveGarbage = 39,
		NetworkInformationServiceDomain = 40,
		NetworkInformationServers = 41,
		NetworkTimeProtocolServers = 42,
		VendorSpecificInformation = 43,
		NetBIOSoverTCPIPNameServer = 44,
		NetBIOSoverTCPIPDatagramDistributionServer = 45,
		NetBIOSoverTCPIPNodeType = 46,
		NetBIOSoverTCPIPScope = 47,
		XWindowSystemFontServer = 48,
		XWindowSystemDisplayManager = 49,
		RequestedIPAddress = 50,
		IPAddressLeaseTime = 51,
		OptionOverload = 52,
		DHCPMessageType = 53,
		ServerIdentifier = 54,
		ParameterRequestList = 55,
		Message = 56,
		MaximumDHCPMessageSize = 57,
		RenewalTimeValue_T1 = 58,
		RebindingTimeValue_T2 = 59,
		Vendorclassidentifier = 60,
		ClientIdentifier = 61,
		NetworkInformationServicePlusDomain = 64,
		NetworkInformationServicePlusServers = 65,
		TFTPServerName = 66,
		BootfileName = 67,
		MobileIPHomeAgent = 68,
		SMTPServer = 69,
		POP3Server = 70,
		NNTPServer = 71,
		DefaultWWWServer = 72,
		DefaultFingerServer = 73,
		DefaultIRCServer = 74,
		StreetTalkServer = 75,
		STDAServer = 76,
		Architecture = 93,
		ClientInterfaceIdent = 94,

		GUID = 97,

		#region "PXELinux"
		MAGICOption = 208,
		ConfigurationFile = 209,
		PathPrefix = 210,
		RebootTime = 211,
		#endregion

		#region "Windows Deployment Server"
		WDSNBP = 250,
		BCDPath = 252,
		#endregion

		End = byte.MaxValue
	}

	public enum BootServerTypes : sbyte
	{
		PXEBootstrapServer,
		MicrosoftWindowsNTBootServer,
		IntelLCMBootServer,
		DOSUNDIBootServer,
		NECESMPROBootServer,
		IBMWSoDBootServer,
		IBMLCCMBootServer,
		CAUnicenterTNGBootServer,
		HPOpenViewBootServer
	}

	public enum PXEVendorEncOptions : byte
	{
		MultiCastIPAddress = 1,
		MulticastClientPort = 2,
		MulticastServerPort = 3,
		MulticastTFTPTimeout = 4,
		MulticastTFTPDelay = 5,
		DiscoveryControl = 6,
		DiscoveryMulticastAddress = 7,
		BootServers = 8,
		BootMenue = 9,
		MenuPrompt = 10,
		MulticastAddressAllocation = 11,
		CredentialTypes = 12,
		BootItem = 71,
		End = byte.MaxValue
	}

	/// <summary>
	/// Options used by the Windows Deployment Server NBP
	/// </summary>
	public enum WDSNBPOptions
	{
		Unknown,
		Architecture,
		NextAction,
		PollInterval,
		PollRetryCount,
		RequestID,
		Message,
		VersionQuery,
		ServerVersion,
		ReferralServer,
		PXEClientPrompt,
		PxePromptDone,
		NBPVersion,
		ActionDone,
		AllowServerSelection,
		End = byte.MaxValue
	}

	/// <summary>
	/// Options used by the WDSNBPOptions.NextAction
	/// </summary>
	public enum NextActionOptionValues : int
	{
		Drop = 0,
		Approval = 1,
		Referral = 3,
		Abort = 5
	}

	/// <summary>
	/// Options used by the WDSNBPOptions.PXEClientPrompt and WDSNBPOptions.PXEPromptDone
	/// </summary>
	public enum PXEPromptOptionValues
	{
		Unknown,
		OptIn,
		NoPrompt,
		OptOut
	}

	public enum PXEFrameworks
	{
		Unknown,
		UNDI
	}

	public enum BootMessageType : byte
	{
		Request = 1,
		Reply = 2,

		RISRequest = 81,
		RISReply = 82
	}

	public enum TFTPMode
	{
		Octet,
		Mail,
		NetASCII
	}

	/// <summary>
	/// Options used by the WDSNBPOptions.NBPVersion
	/// </summary>
	public enum NBPVersionValues
	{
		Seven = 7,
		Eight = 8,
		Unknown = byte.MaxValue
	}

	/// <summary>
	/// Definitions for Packets used by the Server
	/// </summary>
	public enum PacketType
	{
		DHCP, RIS, TFTP
	}

	/// <summary>
	/// Defines the Modes in which the Server can operate
	/// AllowAll: Accapt All (known and unknown) Clients
	/// KnownOnly: Accept only known Clients
	/// </summary>
	public enum ServerMode
	{
		AllowAll,
		KnownOnly
	}

	public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

	public delegate void DataSendEventHandler(object sender, DataSendEventArgs e);

	public delegate void HTTPDataReceivedEventHandler(object sender, HTTPDataReceivedEventArgs e);

	public delegate void HTTPDataSendEventHandler(object sender, HTTPDataSendEventArgs e);

	public class DataReceivedEventArgs : EventArgs
	{
		private byte[] data;
		private IPEndPoint endpoint;
		private SocketType type;

		public byte[] Data
		{
			get
			{
				return this.data;
			}
			set
			{
				this.data = value;
			}
		}

		public IPEndPoint RemoteEndpoint
		{
			get
			{
				return this.endpoint;
			}
			set
			{
				this.endpoint = value;
			}
		}

		public SocketType Type
		{
			get
			{
				return this.type;
			}
			set
			{
				this.type = value;
			}
		}
	}

	public class DataSendEventArgs : EventArgs
	{
		public int BytesSend;
		public IPEndPoint RemoteEndpoint;
		public SocketType Type;
	}

	public class HTTPDataReceivedEventArgs : EventArgs
	{
		public HttpListenerRequest Request;
	}

	public class HTTPDataSendEventArgs : EventArgs
	{
		public string Bytessend;
	}

}
