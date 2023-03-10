namespace Server.Network
{
	using System;
	using System.IO;
	using System.Net;

	public sealed class TFTPClient : ClientProvider, IDisposable, ITFTPClient_Provider
	{
		ushort blksize;
		ushort winsize;
		ushort msftwindow;
		Guid guid;

		public TFTPClient(IPEndPoint endpoint)
		{
			endp = endpoint;
			type = SocketType.TFTP;
			ID = endp.Address.ToString();
			FileName = string.Empty;
			TransferSize = 0;
			BytesRead = 0;
			winsize = 1;
			msftwindow = Settings.SendBuffer;
			blksize = Settings.MaximumAllowedBlockSize;
			WindowSizeMode = false;
			Blocks = ushort.MinValue;
		}

		~TFTPClient()
		{
			Dispose();
		}

		public string ID { get; set; }

		public ushort BlockSize
		{
			get
			{
				return blksize <= Settings.MaximumAllowedBlockSize ? blksize : Settings.MaximumAllowedBlockSize;
			}

			set
			{
				blksize = value <= Settings.MaximumAllowedBlockSize ? value : Settings.MaximumAllowedBlockSize;
			}
		}

		public ushort WindowSize
		{
			get
			{
				return winsize <= Settings.MaximumAllowedWindowSize ? winsize : Settings.MaximumAllowedWindowSize;
			}

			set
			{
				winsize = value <= Settings.MaximumAllowedWindowSize ? value : Settings.MaximumAllowedWindowSize;
			}
		}

		public ushort MSFTWindow
		{
			get
			{
				return msftwindow;
			}

			set
			{
				if (value <= msftwindow)
					msftwindow = value;
			}
		}

		public bool WindowSizeMode { get; set; }

		public ushort Blocks { get; set; }

		public long BytesRead { get; set; }

		public TFTPMode Mode { get; set; }

		public long TransferSize { get; set; }

		public string FileName { get; set; }

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

		public TFTPStage Stage { get; set; }

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

		public FileStream FileStream { get; set; }


		public void Dispose()
		{
			FileStream?.Dispose();
		}
	}
}
