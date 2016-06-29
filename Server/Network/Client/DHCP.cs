namespace bootpd
{
	using System;
	using System.Net;

	public sealed class DHCPClient : ClientProvider, IDHCPClient_Provider
	{
		string bootfile;
		string bcdpath;
		string id;
		string adminMessage;
		string mac;
		VendorIdents vendorIdent;

		NextActionOptionValues nextAction;
		PXEFrameworks pxeframework;

		ushort retrycount;
		ushort pollInterval;

		ushort undi_major;
		ushort undi_minor;

		bool wdsclient;
		bool actionDone;

		Guid guid;
		DHCPMsgType msgType;
		Architecture arch;

		public DHCPClient(Guid guid, string mac, SocketType type, IPEndPoint endpoint)
		{
			this.type = type;
			this.endp = endpoint;
			this.wdsclient = false;
			this.pollInterval = Convert.ToUInt16(Settings.PollInterval);
			this.retrycount = Convert.ToUInt16(Settings.RetryCount);
			this.pxeframework = PXEFrameworks.UNDI;

			this.actionDone = false;
			this.guid = guid;
			this.mac = mac;
			this.id = string.Format("{0}-{1}", this.guid, this.mac);
			this.adminMessage = "Client ID: {0}".F(this.id);
			this.nextAction = NextActionOptionValues.Approval;
			this.msgType = DHCPMsgType.Offer;
			this.arch = Architecture.Intelx86PC;
			this.bootfile = string.Empty;

			this.undi_major = 2;
			this.undi_minor = 1;
		}

		public string ID
		{
			get
			{
				return this.id;
			}

			set
			{
				this.ID = value;
			}
		}

		public PXEFrameworks PXEFramework
		{
			get
			{
				return this.pxeframework;
			}

			set
			{
				this.pxeframework = value;
			}
		}

		public string MACAddress => this.mac;

		public Architecture Arch
		{
			get
			{
				return this.arch;
			}

			set
			{
				this.arch = value;
			}
		}

		public NextActionOptionValues NextAction
		{
			get
			{
				return this.nextAction;
			}

			set
			{
				this.nextAction = value;
			}
		}

		public VendorIdents VendorIdent
		{
			get
			{
				return this.vendorIdent;
			}

			set
			{
				this.vendorIdent = value;
			}
		}

		public bool IsWDSClient
		{
			get
			{
				return this.wdsclient;
			}

			set
			{
				this.wdsclient = value;
			}
		}

		public ushort PollInterval
		{
			get
			{
				return this.pollInterval;
			}

			set
			{
				this.pollInterval = value;
			}
		}

		public ushort RetryCount
		{
			get
			{
				return this.retrycount;
			}

			set
			{
				this.retrycount = value;
			}
		}

		public bool ActionDone
		{
			get
			{
				return this.actionDone;
			}

			set
			{
				this.actionDone = value;
			}
		}

		public string BootFile
		{
			get
			{
				return this.bootfile;
			}

			set
			{
				this.bootfile = Filesystem.ReplaceSlashes(value);
			}
		}

		public string BCDPath
		{
			get
			{
				return this.bcdpath;
			}

			set
			{
				this.bcdpath = value;
			}
		}

		public ushort UNDI_Major
		{
			get
			{
				return this.undi_major;
			}

			set
			{
				this.undi_major = value;
			}
		}

		public ushort UNDI_Minor
		{
			get
			{
				return this.undi_minor;
			}

			set
			{
				this.undi_minor = value;
			}
		}

		public override IPEndPoint EndPoint
		{
			get
			{
				return this.endp;
			}

			set
			{
				this.endp = value;
			}
		}

		public string AdminMessage
		{
			get
			{
				return this.adminMessage;
			}

			set
			{
				this.adminMessage = value;
			}
		}

		public Guid Guid
		{
			get
			{
				return this.guid;
			}

			set
			{
				this.guid = value;
			}
		}

		public DHCPMsgType MsgType
		{
			get
			{
				return this.msgType;
			}

			set
			{
				this.msgType = value;
			}
		}

		public override SocketType Type
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
}
