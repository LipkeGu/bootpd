namespace WDSServer.Network
{
	using System;
	using System.Net;
	using WDSServer.Providers;

	public sealed class DHCPClient : ClientProvider, IDHCPClient_Provider
	{
		string bootfile;
		string bcdpath;
		string id;
		string adminMessage;
		string mac;

		int retrycount;
		int pollInterval;
		int requestID;

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
			this.pollInterval = Settings.PollInterval;
			this.retrycount = Settings.RetryCount;

			this.requestID = 0;

			this.actionDone = false;
			this.guid = guid;
			this.mac = mac;
			this.id = string.Format("{0}-{1}", this.guid, this.mac);
			this.adminMessage = "Client ID: {0}".F(this.id);

			this.msgType = DHCPMsgType.Offer;
			this.arch = Architecture.INTEL_X86;
			this.bootfile = string.Empty;
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

		public int PollIntervall
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

		public int RequestID
		{
			get
			{
				return this.requestID;
			}

			set
			{
				this.requestID = value;
			}
		}

		public int RetryCount
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
