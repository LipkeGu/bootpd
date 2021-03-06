﻿namespace Server.Network
{
	using System;
	using System.IO;
	using System.Net;

	public sealed class TFTPClient : ClientProvider, IDisposable, ITFTPClient_Provider
	{
		string id;
		string filename;

		TFTPMode mode;
		TFTPStage stage;

		ushort blksize;
		ushort blocks;
		ushort winsize;
		ushort msftwindow;

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
			this.msftwindow = Settings.SendBuffer;
			this.blksize = Settings.MaximumAllowedBlockSize;
			this.windowssizemode = false;
			this.blocks = ushort.MinValue;
		}

		~TFTPClient()
		{
			this.Dispose();
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

		public ushort BlockSize
		{
			get
			{
				return this.blksize <= Settings.MaximumAllowedBlockSize ? this.blksize : Settings.MaximumAllowedBlockSize;
			}

			set
			{
				this.blksize = value <= Settings.MaximumAllowedBlockSize ? value : Settings.MaximumAllowedBlockSize;
			}
		}

		public ushort WindowSize
		{
			get
			{
				return this.winsize <= Settings.MaximumAllowedWindowSize ? this.winsize : Settings.MaximumAllowedWindowSize;
			}

			set
			{
				this.winsize = value <= Settings.MaximumAllowedWindowSize ? value : Settings.MaximumAllowedWindowSize;
			}
		}

		public ushort MSFTWindow
		{
			get
			{
				return this.msftwindow;
			}

			set
			{
				if (value <= this.msftwindow)
					this.msftwindow = value;
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

		public ushort Blocks
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

		public void Dispose()
		{
			this.filestream?.Dispose();
			this.bufferedstream?.Dispose();
		}
	}
}
