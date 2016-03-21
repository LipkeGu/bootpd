namespace WDSServer.Network
{
	using System;
	using System.Net;
	using WDSServer.Providers;

	public sealed class TFTPClient : ClientProvider
	{
		string id;
		string filename;

		TFTPMode mode;

		int blksize;
		int winsize;
		int blocks;

		long tsize;
		long bytesread;

		TFTPStage stage;
		Guid guid;

		public TFTPClient(IPEndPoint endpoint)
		{
			this.endp = endpoint;
			this.type = SocketType.TFTP;
			this.id = this.endp.Address.ToString();
			this.filename = string.Empty;
			this.tsize = 0;
			this.bytesread = 0;
			this.winsize = 1;
			this.blksize = 512;
			this.blocks = 0;
		}

		public string ID
		{
			get
			{
				return this.id;
			}

			set
			{
				this.id = value;
			}
		}

		public int BlockSize
		{
			get
			{
				if (this.tsize <= this.blksize)
					return (int)this.tsize;
				else
					return this.blksize;
			}

			set
			{
				this.blksize = value;
			}
		}

		public int WindowSize
		{
			get
			{
				return this.winsize;
			}

			set
			{
				this.winsize = value;
			}
		}

		public int Blocks
		{
			get
			{
				return this.blocks;
			}

			set
			{
				this.blocks = value;
			}
		}

		public long BytesRead
		{
			get
			{
				return this.bytesread;
			}

			set
			{
				this.bytesread = value;
			}
		}

		public TFTPMode Mode
		{
			get
			{
				return this.mode;
			}

			set
			{
				this.mode = value;
			}
		}

		public long TransferSize
		{
			get
			{
				return this.tsize;
			}

			set
			{
				this.tsize = value;
			}
		}

		public string FileName
		{
			get
			{
				return this.filename;
			}

			set
			{
				var file = value;

				this.filename = file;
				this.tsize = Filesystem.Size(file);
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

		public TFTPStage Stage
		{
			get
			{
				return this.stage;
			}

			set
			{
				this.stage = value;
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
