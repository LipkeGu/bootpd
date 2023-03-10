namespace Server.Network
{
	using Extensions;
	using System;
	using System.Net;

	public sealed class DHCPClient : ClientProvider, IDHCPClient_Provider
	{
		string bootfile;
		Guid guid;

		public class RBCPClient
		{
			public ushort Layer { get; set; }

			public ushort Item { get; set; }

			public RBCPClient()
			{
			}
		}

		public RBCPClient RBCP { get; private set; }

		public WDSClient WDS { get; private set; }

		public class WDSClient
		{
			public bool AllowServerSelection { get; set; }
			public ushort PollInterval { get; set; }

			public ushort RetryCount { get; set; }

			public bool ActionDone { get; set; }
			public NextActionOptionValues NextAction { get; set; }
			public string BCDPath { get; set; }
			public string AdminMessage { get; set; }
			public Architecture Architecure { get; set; }
			public bool ServerSelection { get; set; }

			public uint RequestId { get; set; } = 0;
			public string VersionQuery { get; set; }
			public NBPVersionValues ServerVersion { get; set; }
			public IPAddress ReferralServer { get; set; }
			public PXEPromptOptionValues ClientPrompt { get; set; } = PXEPromptOptionValues.OptIn;
			public PXEPromptOptionValues PromptDone { get; set; } = PXEPromptOptionValues.OptIn;
			public NBPVersionValues NBPVersiopn { get; set; }

			public WDSClient()
			{
				PollInterval = Convert.ToUInt16(5);
				RetryCount = ushort.MaxValue;
				ActionDone = false;
				AdminMessage = "Waiting for Approval...";
				NextAction = NextActionOptionValues.Approval;
				Architecure = Architecture.Intelx86PC;
			}
		}

		public DHCPClient(string mac, SocketType type, IPEndPoint endpoint)
		{
			WDS = new WDSClient();
			RBCP = new RBCPClient();

			this.type = type;
			endp = endpoint;
			IsWDSClient = false;
			PXEFramework = PXEFrameworks.UNDI;

			MACAddress = mac;
			ID = "{0}".F(MACAddress);
			MsgType = DHCPMsgType.Offer;
			Arch = Architecture.Intelx86PC;
			bootfile = string.Empty;

			UNDI_Major = 2;
			UNDI_Minor = 1;
		}

		public string ID
		{
			get; set;
		}

		public PXEFrameworks PXEFramework { get; set; }

		public string MACAddress { get; }

		public Architecture Arch { get; set; }


		public VendorIdents VendorIdent { get; set; }

		public bool IsWDSClient { get; set; }

		public string BootFile
		{
			get
			{
				return Filesystem.ReplaceSlashes(bootfile);
			}

			set
			{
				bootfile = Filesystem.ReplaceSlashes(value);
			}
		}



		public ushort UNDI_Major { get; set; }

		public ushort UNDI_Minor { get; set; }

		public override IPEndPoint EndPoint
		{
			get
			{
				return endp;
			}

			set
			{
				endp = value;
			}
		}



		public Guid Guid
		{
			get
			{
				return guid;
			}

			set
			{
				guid = value;
			}
		}

		public DHCPMsgType MsgType { get; set; }

		public override SocketType Type
		{
			get
			{
				return type;
			}

			set
			{
				type = value;
			}
		}
	}
}
