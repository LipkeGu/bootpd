namespace bootpd
{
	using System;
	using System.Net;

	public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

	public delegate void DataSendEventHandler(object sender, DataSendEventArgs e);

	public delegate void HTTPDataReceivedEventHandler(object sender, HTTPDataReceivedEventArgs e);

	public delegate void HTTPDataSendEventHandler(object sender, HTTPDataSendEventArgs e);

	public class DataReceivedEventArgs : EventArgs
	{
		private byte[] data;
		private IPEndPoint endpoint;
		private Definitions.SocketType type;

		public byte[] Data
		{
			get { return this.data; }
			set { this.data = value; }
		}

		public IPEndPoint RemoteEndpoint
		{
			get { return this.endpoint; }
			set { this.endpoint = value; }
		}

		public Definitions.SocketType Type
		{
			get { return this.type; }
			set { this.type = value; }
		}
	}

	public class DataSendEventArgs : EventArgs
	{
		public int BytesSend;
		public IPEndPoint RemoteEndpoint;
		public Definitions.SocketType Type;
	}

	public class HTTPDataReceivedEventArgs : EventArgs
	{
		public HttpListenerRequest Request;
	}

	public class HTTPDataSendEventArgs : EventArgs
	{
		public string Bytessend;
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

		public enum PXEFrameworks
		{
			Unknown,
			UNDI
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
			OFF = 10,
			NCR = 11
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

		public enum VendorIdents
		{
			PXEClient,
			PXEServer,
			BSDP /* Apple */
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

			End = 255
		}

		/// <summary>
		/// Options used by the WDSNBPOptions.Architecture
		/// </summary>
		public enum Architecture
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

		public enum SystemType
		{
			Desktop,
			Mobile,
			ThinClient,
			Virtual
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
		/// KnownOnly: Accept only known Clients
		/// </summary>
		public enum ServerMode
		{
			AllowAll,
			KnownOnly
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

		public enum PXEVendorEncOptions
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
			End = 255
		}

		public enum BcdBootMgrElements
		{
			DisplayOrder = 0x24000001,
			BootSequence = 0x24000002,
			DefaultObject = 0x23000003,
			Timeout = 0x25000004,
			AttemptResume = 0x26000005,
			ResumeObject = 0x23000006,
			ToolsDisplayOrder = 0x24000010,
			DisplayBootMenu = 0x26000020,
			NoErrorDisplay = 0x26000021,
			BcdDevice = 0x21000022,
			BcdFilePath = 0x22000023,
			ProcessCustomActionsFirst = 0x26000028,
			CustomActionsList = 0x27000030,
			PersistBootSequence = 0x26000031
		}

		public enum BcdDeviceObjectElementTypes
		{
			RamdiskImageOffset = 0x35000001,
			TftpClientPort = 0x35000002,
			SdiDevice = 0x31000003,
			SdiPath = 0x32000004,
			RamdiskImageLength = 0x35000005,
			RamdiskExportAsCd = 0x36000006,
			RamdiskTftpBlockSize = 0x36000007,
			RamdiskTftpWindowSize = 0x36000008,
			RamdiskMulticastEnabled = 0x36000009,
			RamdiskMulticastTftpFallback = 0x3600000A,
			RamdiskTftpVarWindow = 0x3600000B
		}

		public enum BcdLibraryElementTypes
		{
			ApplicationDevice = 0x11000001,
			ApplicationPath = 0x12000002,
			Description = 0x12000004,
			PreferredLocale = 0x12000005,
			InheritedObjects = 0x14000006,
			TruncatePhysicalMemory = 0x15000007,
			RecoverySequence = 0x14000008,
			AutoRecoveryEnabled = 0x16000009,
			BadMemoryList = 0x1700000a,
			AllowBadMemoryAccess = 0x1600000b,
			FirstMegabytePolicy = 0x1500000c,
			RelocatePhysicalMemory = 0x1500000D,
			AvoidLowPhysicalMemory = 0x1500000E,
			EmsEnabled = 0x16000020,
			EmsPort = 0x15000022,
			EmsBaudRate = 0x15000023,
			LoadOptionsString = 0x12000030,
			DisplayAdvancedOptions = 0x16000040,
			DisplayOptionsEdit = 0x16000041,
			BsdLogDevice = 0x11000043,
			BsdLogPath = 0x12000044,
			GraphicsModeDisabled = 0x16000046,
			ConfigAccessPolicy = 0x15000047,
			DisableIntegrityChecks = 0x16000048,
			AllowPrereleaseSignatures = 0x16000049,
			FontPath = 0x1200004A,
			SiPolicy = 0x1500004B,
			FveBandId = 0x1500004C,
			ConsoleExtendedInput = 0x16000050,
			GraphicsResolution = 0x15000052,
			RestartOnFailure = 0x16000053,
			GraphicsForceHighestMode = 0x16000054,
			IsolatedExecutionContext = 0x16000060,
			BootUxDisable = 0x1600006C,
			BootShutdownDisabled = 0x16000074,
			AllowedInMemorySettings = 0x17000077,
			ForceFipsCrypto = 0x16000079
		}

		public enum BcdOSLoaderElementTypes
		{
			OSDevice = 0x21000001,
			SystemRoot = 0x22000002,
			AssociatedResumeObject = 0x23000003,
			DetectKernelAndHal = 0x26000010,
			KernelPath = 0x22000011,
			HalPath = 0x22000012,
			DbgTransportPath = 0x22000013,
			NxPolicy = 0x25000020,
			PAEPolicy = 0x25000021,
			WinPEMode = 0x26000022,
			DisableCrashAutoReboot = 0x26000024,
			UseLastGoodSettings = 0x26000025,
			AllowPrereleaseSignatures = 0x26000027,
			NoLowMemory = 0x26000030,
			RemoveMemory = 0x25000031,
			IncreaseUserVa = 0x25000032,
			UseVgaDriver = 0x26000040,
			DisableBootDisplay = 0x26000041,
			DisableVesaBios = 0x26000042,
			DisableVgaMode = 0x26000043,
			ClusterModeAddressing = 0x25000050,
			UsePhysicalDestination = 0x26000051,
			RestrictApicCluster = 0x25000052,
			UseLegacyApicMode = 0x26000054,
			X2ApicPolicy = 0x25000055,
			UseBootProcessorOnly = 0x26000060,
			NumberOfProcessors = 0x25000061,
			ForceMaximumProcessors = 0x26000062,
			ProcessorConfigurationFlags = 0x25000063,
			MaximizeGroupsCreated = 0x26000064,
			ForceGroupAwareness = 0x26000065,
			GroupSize = 0x25000066,
			UseFirmwarePciSettings = 0x26000070,
			MsiPolicy = 0x25000071,
			SafeBoot = 0x25000080,
			SafeBootAlternateShell = 0x26000081,
			BootLogInitialization = 0x26000090,
			VerboseObjectLoadMode = 0x26000091,
			UsePlatformClock = 0x260000A2,
			ForceLegacyPlatform = 0x260000A3,
			TscSyncPolicy = 0x250000A6,
			EmsEnabled = 0x260000b0,
			DriverLoadFailurePolicy = 0x250000c1,
			BootMenuPolicy = 0x250000C2,
			AdvancedOptionsOneTime = 0x260000C3,
			BootStatusPolicy = 0x250000E0,
			DisableElamDrivers = 0x260000E1,
			BootUxPolicy = 0x250000F7,
			TpmBootEntropyPolicy = 0x25000100,
			XSaveDisable = 0x2500012b
		}
	}
}
