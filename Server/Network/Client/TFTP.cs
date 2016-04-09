namespace bootpd
{
	using System;
	using System.IO;
	using System.Net;

	public sealed class TFTPClient : ClientProvider, IDisposable
	{
		string id;
		string filename;

		TFTPMode mode;
		TFTPStage stage;

		int blksize;
		ushort winsize;
		int blocks;

		long tsize;
		long bytesread;

		bool windowssizemode;

		Guid guid;

		FileStream filestream;
		BufferedStream bufferedstream;

		public TFTPClient(IPEndPoint endpoint)
		{
			this.endp = endpoint;
			this.type = SocketType.TFTP;
			this.id = this.endp.Address.ToString();
			this.filename = string.Empty;
			this.tsize = 0;
			this.bytesread = 0;
			this.winsize = 1;
			this.blksize = Settings.MaximumAllowedBlockSize;
			this.windowssizemode = false;
			this.blocks = 0;
		}

		~TFTPClient()
		{
			this.Dispose();
		}

		public FileStream FileStream
		{
			get
			{
				return this.filestream;
			}

			set
			{
				this.filestream = value;
			}
		}

		public BufferedStream BufferedStream
		{
			get
			{
				return this.bufferedstream;
			}

			set
			{
				this.bufferedstream = value;
			}
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
				if (this.blksize <= Settings.MaximumAllowedBlockSize)
					return this.blksize;
				else
					return Settings.MaximumAllowedBlockSize;
			}

			set
			{
				if (value <= Settings.MaximumAllowedBlockSize)
					this.blksize = value;
				else
					this.blksize = Settings.MaximumAllowedBlockSize;
			}
		}

		public ushort WindowSize
		{
			get
			{
				if (this.winsize <= Settings.MaximumAllowedWindowSize)
					return this.winsize;
				else
					return Settings.MaximumAllowedWindowSize;
			}

			set
			{
				if (value <= Settings.MaximumAllowedWindowSize)
					this.winsize = value;
				else
					this.winsize = Settings.MaximumAllowedWindowSize;
			}
		}

		public bool WindowSizeMode
		{
			get
			{
				return this.windowssizemode;
			}

			set
			{
				this.windowssizemode = value;
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
				this.filename = value;
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

		public void Dispose()
		{
			if (this.filestream != null)
				this.filestream.Dispose();

			if (this.bufferedstream != null)
				this.bufferedstream.Dispose();
		}
	}
}
