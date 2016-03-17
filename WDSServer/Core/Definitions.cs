using System;
using System.Collections.Specialized;
using System.Net;

namespace WDSServer
{
	public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);
	public delegate void DataSendEventHandler(object sender, DataSendEventArgs e);

	public delegate void HTTPDataReceivedEventHandler(object sender, HTTPDataReceivedEventArgs e);
	public delegate void HTTPDataSendEventHandler(object sender, HTTPDataSendEventArgs e);

	public class DataReceivedEventArgs : EventArgs
	{
		public byte[] Data;
		public IPEndPoint RemoteEndpoint;
		public Definitions.SocketType Type;
	}

	public class DataSendEventArgs : EventArgs
	{
		public int BytesSend;
		public IPEndPoint RemoteEndpoint;
		public Definitions.SocketType Type;
	}

	public class HTTPDataReceivedEventArgs : EventArgs
	{
		public string Filename;
		public NameValueCollection Arguments;
		public NameValueCollection Headers;
		public string ContentType;
	}

	public class HTTPDataSendEventArgs : EventArgs
	{
		public string bytessend;
	}

	public class Definitions
	{
		public enum Fileystem
		{
			Unknown,
			FAT,
			FAT32,
			NTFS,
		}

		public enum BootMessageType
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

		public enum LogTypes
		{
			Info,
			Warning,
			Error,
			Other
		}

		public enum UserGroup
		{
			Users,
			PowerUsers,
			Ádministrators
		}

		public enum TFTPOPCodes
		{
			UNK = 0,

			RRQ = 1,
			WRQ = 2,

			DAT = 3,
			ACK = 4,
			ERR = 5,
			OCK = 6
		}

		public enum TFTPErrorCode
		{
			Unknown,
			FileNotFound,
			AccessViolation,
			DiskFull,           // and Allocation Exceeded
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
			OFF = 10
		}

		public enum DHCPMsgType
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

		// TODO: This List is not complete!
		public enum DHCPOptionEnum
		{
			Pad = 0,
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
			GUID = 97,

			WDSNBP = 250,
			BCDPath = 252,
			End = 255
		}

		/// <summary>
		/// Options used by the WDSNBPOptions.Architecture
		/// </summary>
		public enum Architecture
		{
			INTEL_X86 = 0,
			NEC_PC98 = 1,
			INTEL_IA64 = 2,
			DEC_ALPHA = 3,
			ARC_X86 = 4,
			INTEL_LEAN = 5,
			INTEL_X64 = 6,
			INTEL_EFI = 7
		}

		public enum SystemType
		{
			Desktop,
			Mobile,
			ThinClient
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
			End = 255
		}

		/// <summary>
		/// Options used by the WDSNBPOptions.NextAction
		/// </summary>
		public enum NextActionOptionValues
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

		/// <summary>
		/// Options used by the WDSNBPOptions.NBPVersion
		/// </summary>
		public enum NBPVersionValues
		{
			Seven = 7,
			Eight = 8,
			Unknown = 255
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
		/// KnownOnly: Accept only known Clients but ignore unknown Clients
		/// </summary>
		public enum ServerMode
		{
			AllowAll,
			KnownOnly
		}
	}
}
