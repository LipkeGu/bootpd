using System;
using System.Net;
using System.Text;

namespace Server.Network
{

	public class BootMenueEntry
	{
		public ushort Id { get; private set; }

		public string Description { get; private set; }

		public BootMenueEntry(ushort id, string desc)
		{
			Id = id;
			Description = desc;
		}
	}

	/// <summary>
	/// Indicates the type of data in a subblock 
	/// </summary>
	public enum NTLMSSPSubBlockTypes : ushort
	{
		Terminator = 0x0000,
		ServerName = 0x0001,
		DomainName = 0x0002,
		DNSHostname = 0x0003,
		DNSDomainName = 0x0004,
		DnsTreeName = 0x0005,
		Flags = 0x0006,
		Timestamp = 0x0007,
		SingleHost = 0x0008,
		TargetName = 0x0009,
		ChannelBindings = 0x000a
	}

	public enum ntlmssp_role
	{
		NTLMSSP_SERVER,
		NTLMSSP_CLIENT
	}

	public enum ntlmssp_sign_version : byte
	{
		ONE = 1
	}

	public enum ntlmssp_sig_size : int
	{
		SIXTEEN = 16
	}

	public enum ntlmssp_message_type
	{
		NTLMSSP_INITIAL = 0 /* samba internal state */,
		NTLMSSP_NEGOTIATE = 1,
		NTLMSSP_CHALLENGE = 2,
		NTLMSSP_AUTH = 3,
		NTLMSSP_UNKNOWN = 4,
		NTLMSSP_DONE = 5 /* samba final state */
	}

	[Flags]
	public enum ntlmssp_flags : uint
	{
		/// <summary>
		/// This flag is set to indicate that the server/client will be using UNICODE strings. (Server <-> Client)
		/// </summary>
		NTLMSSP_NEGOTIATE_UNICODE = 0x00000001,
		/// <summary>
		/// This flag is set to indicate that the server/client will be using OEM strings. (Server <-> Client)
		/// </summary>
		NTLMSSP_NEGOTIATE_OEM = 0x00000002,
		/// <summary>
		/// If set, a TargetName field of the CHALLENGE_MESSAGE MUST be supplied.
		/// </summary>
		NTLMSSP_REQUEST_TARGET = 0x00000004,
		/// <summary>
		/// requests session key negotiation for message signatures. If the client sends NTLMSSP_NEGOTIATE_SIGN 
		/// to the server in the NEGOTIATE_MESSAGE, the server MUST return NTLMSSP_NEGOTIATE_SIGN to the client in the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_NEGOTIATE_SIGN = 0x00000010,
		/// <summary>
		/// requests session key negotiation for message confidentiality. If the client sends NTLMSSP_NEGOTIATE_SEAL
		/// to the server in the NEGOTIATE_MESSAGE, the server MUST return NTLMSSP_NEGOTIATE_SEAL to the client in the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_NEGOTIATE_SEAL = 0x00000020,
		/// <summary>
		/// requests connectionless authentication. If NTLMSSP_NEGOTIATE_DATAGRAM is set, then NTLMSSP_NEGOTIATE_KEY_EXCH MUST 
		/// always be set in the AUTHENTICATE_MESSAGE to the server and the CHALLENGE_MESSAGE to the client.
		/// </summary>
		NTLMSSP_NEGOTIATE_DATAGRAM = 0x00000040,
		/// <summary>
		/// requests LAN Manager (LM) session key computation.
		/// </summary>
		NTLMSSP_NEGOTIATE_LM_KEY = 0x00000080,
		/// <summary>
		/// Netware 
		/// </summary>
		NTLMSSP_NEGOTIATE_NETWARE = 0x00000100,
		/// <summary>
		/// Indicates that NTLM authentication is supported. (Server <-> Client)
		/// </summary>
		NTLMSSP_NEGOTIATE_NTLM = 0x00000200,
		/// <summary>
		/// requests only NT session key computation
		/// </summary>
		NTLMSSP_NEGOTIATE_NT_ONLY = 0x00000400,
		/// <summary>
		/// The connection SHOULD be anonymous
		/// </summary>
		NTLMSSP_NEGOTIATE_ANONYMOUS = 0x00000800,
		/// <summary>
		/// The domain name is provided.
		/// </summary>
		NTLMSSP_NEGOTIATE_OEM_DOMAIN_SUPPLIED = 0x00001000,
		/// <summary>
		/// This flag indicates whether the Workstation field is present.
		/// </summary>
		NTLMSSP_NEGOTIATE_OEM_WORKSTATION_SUPPLIED = 0x00002000,
		/// <summary>
		/// The server sets this flag to inform the client that the server and client are on the same machine.
		/// The server provides a local security context handle with the message.
		/// </summary>
		NTLMSSP_NEGOTIATE_LOCAL_CALL = 0x00004000,
		/// <summary>
		/// requests the presence of a signature block on all messages. NTLMSSP_NEGOTIATE_ALWAYS_SIGN MUST
		/// be set in the NEGOTIATE_MESSAGE to the server and the CHALLENGE_MESSAGE to the client.
		/// </summary>
		NTLMSSP_NEGOTIATE_ALWAYS_SIGN = 0x00008000,
		/// <summary>
		/// TargetName MUST be a domain name. The data corresponding to this flag is
		/// provided by the server in the TargetName field of the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_TARGET_TYPE_DOMAIN = 0x00010000,
		/// <summary>
		/// If set, TargetName MUST be a server name. The data corresponding to this flag is provided
		/// by the server in the TargetName field of the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_TARGET_TYPE_SERVER = 0x00020000,
		/// <summary>
		/// TargetName MUST be a share name. The data corresponding to this flag is
		/// provided by the server in the TargetName field of the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_TARGET_TYPE_SHARE = 0x00040000,
		/// <summary>
		/// requests usage of the NTLM v2 session security. 
		/// </summary>
		NTLMSSP_NEGOTIATE_EXTENDED_SESSIONSECURITY = 0x00080000,
		/// <summary>
		/// Requests an identify level token.
		/// </summary>
		NTLMSSP_NEGOTIATE_IDENTIFY = 0x00100000,
		/// <summary>
		/// requests the usage of the LMOWF
		/// </summary>
		NTLMSSP_REQUEST_NON_NT_SESSION_KEY = 0x00400000,
		/// <summary>
		/// indicates that the TargetInfo fields in the CHALLENGE_MESSAGE are populated.
		/// </summary>
		NTLMSSP_NEGOTIATE_TARGET_INFO = 0x00800000,
		/// <summary>
		/// If set, requests the protocol version number. The data corresponding to this flag
		/// is provided in the Version field of the NEGOTIATE_MESSAGE, the CHALLENGE_MESSAGE,
		/// and the AUTHENTICATE_MESSAGE.
		/// </summary>
		NTLMSSP_NEGOTIATE_VERSION = 0x02000000,
		/// <summary>
		/// If the client sends NTLMSSP_NEGOTIATE_128 to the server in the NEGOTIATE_MESSAGE,
		/// the server MUST return NTLMSSP_NEGOTIATE_128 to the client in the CHALLENGE_MESSAGE
		/// only if the client sets NTLMSSP_NEGOTIATE_SEAL or NTLMSSP_NEGOTIATE_SIGN.
		/// </summary>
		NTLMSSP_NEGOTIATE_128 = 0x20000000,
		/// <summary>
		/// This requests an explicit key exchange. This capability SHOULD be used because it improves security for message integrity or confidentiality.
		/// </summary>
		NTLMSSP_NEGOTIATE_KEY_EXCH = 0x40000000,
		/// <summary>
		/// If the client sends NTLMSSP_NEGOTIATE_SEAL or NTLMSSP_NEGOTIATE_SIGN with NTLMSSP_NEGOTIATE_56 to the server in the NEGOTIATE_MESSAGE,
		/// the server MUST return NTLMSSP_NEGOTIATE_56 to the client in the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_NEGOTIATE_56 = 0x80000000
	}

	public enum ntlmssp_Feature
	{
		NTLMSSP_FEATURE_SESSION_KEY = 0x00000001,
		NTLMSSP_FEATURE_SIGN = 0x00000002,
		NTLMSSP_FEATURE_SEAL = 0x00000004,
		NTLMSSP_FEATURE_CCACHE = 0x00000008
	}

	public enum NTLMTargets
	{
		Domain,
		Server,
		Share,
		Local
	}

	[Flags]
	public enum NTLMFlags : int
	{
		/// <summary>
		/// This flag is set to indicate that the server/client will be using UNICODE strings. (Server <-> Client)
		/// </summary>
		NTLMSSP_NEGOTIATE_UNICODE = 0x00000001,
		/// <summary>
		/// This flag is set to indicate that the server/client will be using OEM strings. (Server <-> Client)
		/// </summary>
		NTLMSSP_NEGOTIATE_OEM = 0x00000002,
		/// <summary>
		/// If set, a TargetName field of the CHALLENGE_MESSAGE MUST be supplied.
		/// </summary>
		NTLMSSP_REQUEST_TARGET = 0x00000004,
		/// <summary>
		/// requests session key negotiation for message signatures. If the client sends NTLMSSP_NEGOTIATE_SIGN 
		/// to the server in the NEGOTIATE_MESSAGE, the server MUST return NTLMSSP_NEGOTIATE_SIGN to the client in the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_NEGOTIATE_SIGN = 0x00000010,
		/// <summary>
		/// requests session key negotiation for message confidentiality. If the client sends NTLMSSP_NEGOTIATE_SEAL
		/// to the server in the NEGOTIATE_MESSAGE, the server MUST return NTLMSSP_NEGOTIATE_SEAL to the client in the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_NEGOTIATE_SEAL = 0x00000020,
		/// <summary>
		/// requests connectionless authentication. If NTLMSSP_NEGOTIATE_DATAGRAM is set, then NTLMSSP_NEGOTIATE_KEY_EXCH MUST 
		/// always be set in the AUTHENTICATE_MESSAGE to the server and the CHALLENGE_MESSAGE to the client.
		/// </summary>
		NTLMSSP_NEGOTIATE_DATAGRAM = 0x00000040,
		/// <summary>
		/// requests LAN Manager (LM) session key computation.
		/// </summary>
		NTLMSSP_NEGOTIATE_LM_KEY = 0x00000080,
		/// <summary>
		/// Indicates that NTLM authentication is supported. (Server <-> Client)
		/// </summary>
		NTLMSSP_NEGOTIATE_NTLM = 0x00000200,
		/// <summary>
		/// The connection SHOULD be anonymous
		/// </summary>
		NTLMSSP_NEGOTIATE_ANONYMOUS = 0x00000800,
		/// <summary>
		/// The domain name is provided.
		/// </summary>
		NTLMSSP_NEGOTIATE_OEM_DOMAIN_SUPPLIED = 0x00001000,
		/// <summary>
		/// This flag indicates whether the Workstation field is present.
		/// </summary>
		NTLMSSP_NEGOTIATE_OEM_WORKSTATION_SUPPLIED = 0x00002000,
		/// <summary>
		/// The server sets this flag to inform the client that the server and client are on the same machine.
		/// The server provides a local security context handle with the message.
		/// </summary>
		NTLMSSP_NEGOTIATE_LOCAL_CALL = 0x00004000,
		/// <summary>
		/// requests the presence of a signature block on all messages. NTLMSSP_NEGOTIATE_ALWAYS_SIGN MUST
		/// be set in the NEGOTIATE_MESSAGE to the server and the CHALLENGE_MESSAGE to the client.
		/// </summary>
		NTLMSSP_NEGOTIATE_ALWAYS_SIGN = 0x00008000,
		/// <summary>
		///	TargetName MUST be a domain name. The data corresponding to this flag is
		///	provided by the server in the TargetName field of the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_TARGET_TYPE_DOMAIN = 0x00010000,
		/// <summary>
		/// If set, TargetName MUST be a server name. The data corresponding to this flag is provided
		/// by the server in the TargetName field of the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_TARGET_TYPE_SERVER = 0x00020000,
		/// <summary>
		///	requests usage of the NTLM v2 session security. 
		/// </summary>
		NTLMSSP_NEGOTIATE_EXTENDED_SESSIONSECURITY = 0x00080000,
		/// <summary>
		/// Requests an identify level token.
		/// </summary>
		NTLMSSP_NEGOTIATE_IDENTIFY = 0x00100000,
		/// <summary>
		/// requests the usage of the LMOWF
		/// </summary>
		NTLMSSP_REQUEST_NON_NT_SESSION_KEY = 0x00400000,
		/// <summary>
		/// indicates that the TargetInfo fields in the CHALLENGE_MESSAGE are populated.
		/// </summary>
		NTLMSSP_NEGOTIATE_TARGET_INFO = 0x00800000,
		/// <summary>
		/// If the client sends NTLMSSP_NEGOTIATE_128 to the server in the NEGOTIATE_MESSAGE,
		/// the server MUST return NTLMSSP_NEGOTIATE_128 to the client in the CHALLENGE_MESSAGE
		/// only if the client sets NTLMSSP_NEGOTIATE_SEAL or NTLMSSP_NEGOTIATE_SIGN.
		/// </summary>
		NTLMSSP_NEGOTIATE_128 = 0x20000000,
		/// <summary>
		/// This requests an explicit key exchange. This capability SHOULD be used because it improves security for message integrity or confidentiality.
		/// </summary>
		NTLMSSP_NEGOTIATE_KEY_EXCH = 0x40000000,
		/// <summary>
		/// If the client sends NTLMSSP_NEGOTIATE_SEAL or NTLMSSP_NEGOTIATE_SIGN with NTLMSSP_NEGOTIATE_56 to the server in the NEGOTIATE_MESSAGE,
		/// the server MUST return NTLMSSP_NEGOTIATE_56 to the client in the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_NEGOTIATE_56 = (unchecked((int)0x80000000)),
	}

	public enum NTLMMessageType : uint
	{
		/// <summary>
		/// Initial State
		/// </summary>
		NTLMInitial = 0,
		/// <summary>
		/// The NEGOTIATE_MESSAGE defines an NTLM Negotiate message that is sent from the client to the server.
		/// This message allows the client to specify its supported NTLM options to the server.
		/// </summary>
		NTLMNegotiate = 1,
		/// <summary>
		/// The CHALLENGE_MESSAGE defines an NTLM challenge message that is sent from the server to the client.
		/// The CHALLENGE_MESSAGE is used by the server to challenge the client to prove its identity. 
		/// For connection-oriented requests, the CHALLENGE_MESSAGE generated by the server is in response to the NEGOTIATE_MESSAGE from the client.
		/// </summary>
		NTLMChallenge = 2,
		/// <summary>
		/// The AUTHENTICATE_MESSAGE defines an NTLM authenticate message that is sent
		/// from the client to the server after the CHALLENGE_MESSAGE is processed by the client.
		/// </summary>
		NTLMAuthenticate = 3,
		/// <summary>
		/// Unknown Message
		/// </summary>
		NTLMUnknown = 4,
		/// <summary>
		/// Final State
		/// </summary>
		NTLMDone = 5
	}

	public enum NTLMSSP_Role : uint
	{
		Server,
		Client
	}
	public enum Fileystem
	{
		Unknown,
		FAT,
		FAT32,
		NTFS,
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

	public enum SystemType
	{
		Desktop,
		Mobile,
		ThinClient,
		Virtual
	}

	public enum NTSTATUS : uint
	{
		ERROR_SUCCESS = uint.MinValue,
		SSPI_LOGON_DENIED = 0x8009030c,
		NT_STATUS_INVALID_PARAMETER = 0xC000000D
	}

	public enum NTLMRevisions : byte
	{
		NTLMSSP_REVISION_W2K3 = 0x0f
	}

	public enum DHCPMsgType : byte
	{
		Discover = 1,
		Offer = 2,
		Request = 3,
		Decline = 4,
		Ack = 5,
		Nak = 6,
		Release = 7,
		Inform = 8,
		ForceRenew = 9,
		LeaseQuery = 10,
		LeaseUnassined = 11,
		LeaseUnknown = 12,
		LeaseActive = 13,
		BulkLeaseQuery = 14,
		LeaseQueryDone = 15,
		ActiveLeaseQuery = 16,
		LeasequeryStatus = 17,
		Tls = 18
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
		BSDP, /* Apple */
		MSFT,
		UNKN
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
		VOIPTFTPServer = 120,

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
	public enum BSDPImageAttributes : ushort
	{
		Diagnostic = 0x0300
	}

	public class BSDPImageListEntry
	{
		public uint Id { get; private set; }

		public byte Count { get; private set; }

		// utf-8
		public byte[] Name { get; private set; }



		public BSDPImageListEntry(uint id, byte count, string name)
		{
			Id = id;
			Count = count;
			Name = Encoding.UTF8.GetBytes(name);
		}

		public byte[] GetBytes()
		{
			var offset = 0;
			var buffer = new byte[sizeof(uint) + sizeof(byte) + Name.Length];
			offset += Functions.CopyTo(BitConverter.GetBytes(Id), 0, buffer, offset);
			offset += Functions.CopyTo(Count, buffer, offset);
			offset += Functions.CopyTo(Name, 0, buffer, offset);

			return buffer;
		}

	}

	public enum BSDPArch
	{
		PPC = 0x0000,
		I386 = 0x0001
	}

	public enum BSDPEncOptions
	{
		MessageType = 1,
		Version = 2,
		ServerIdent = 3,
		ServerPriority = 4,
		ReplyPort = 5,
		BoorImageListPath = 6,
		DefaultBootimageId = 7,
		SelectedBootImage = 8,
		BootImageList = 9,
		Netboot10Firmware = 10,
		BootimageAttribs = 11,
		MaxMessageSize = 12

	}


	public enum BSDPMsgType
	{
		List = 1, Select = 2, Failed = 3
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

	public enum ServerType
	{
		DHCP,
		BOOTP,
		TFTP
	}

	public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

	public delegate void DataSendEventHandler(object sender, DataSendEventArgs e);

	public delegate void HTTPDataReceivedEventHandler(object sender, HTTPDataReceivedEventArgs e);

	public delegate void HTTPDataSendEventHandler(object sender, HTTPDataSendEventArgs e);

	public class DataReceivedEventArgs : EventArgs
	{
		public byte[] Data { get; set; }

		public IPEndPoint RemoteEndpoint { get; set; }

		public SocketType Type { get; set; }
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
