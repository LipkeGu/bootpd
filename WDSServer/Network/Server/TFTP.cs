namespace WDSServer.Network
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Text;
	using WDSServer.Providers;

	public sealed class TFTP : ServerProvider, ITFTPServer_Provider, IDisposable
	{
		public static Dictionary<IPAddress, TFTPClient> Clients = new Dictionary<IPAddress, TFTPClient>();

		public static Dictionary<string, string> Options = new Dictionary<string, string>();

		TFTPSocket socket;

		public TFTP(IPEndPoint endpoint)
		{
			this.endp = endpoint;

			this.socket = new TFTPSocket(this.endp);
			this.socket.DataReceived += this.DataReceived;
			this.socket.DataSend += this.DataSend;

			Directories.Create(Settings.TFTPRoot);
		}

		~TFTP()
		{
			this.Dispose();
		}

		public override IPEndPoint LocalEndPoint
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
			Clients.Clear();
			this.socket.Dispose();
		}

		public void Handle_ACK_Request(TFTPPacket data, IPEndPoint client)
		{
			if (!Clients.ContainsKey(client.Address) || Clients[client.Address].FileName == string.Empty)
				return;

			if (data.Block == Clients[client.Address].Blocks)
			{
				Clients[client.Address].Blocks += 1;

				this.Readfile(Clients[client.Address].EndPoint);
			}
		}

		public void Handle_Error_Request(TFTPErrorCode error, string message, bool clientError = false)
		{
			if (Clients.ContainsKey(this.remoteEndp.Address))
			{
				Clients[this.remoteEndp.Address].Stage = TFTPStage.Error;

				if (!clientError)
				{
					var response = new TFTPPacket(5 + message.Length, TFTPOPCodes.ERR);
					response.ErrorCode = error;
					response.ErrorMessage = message;

					this.Send(ref response, Clients[this.remoteEndp.Address].EndPoint);
				}

				Errorhandler.Report(LogTypes.Error, "[TFTP] {0}: {1}".F(error, message));

				Clients.Remove(this.remoteEndp.Address);
			}
		}

		public void Send(ref TFTPPacket packet, IPEndPoint endpoint)
		{
			if (Clients.ContainsKey(this.remoteEndp.Address))
				this.socket.Send(endpoint, packet);
		}

		public void Handle_RRQ_Request(TFTPPacket packet, IPEndPoint client)
		{
			this.ExtractOptions(packet);

			Clients[client.Address].Stage = TFTPStage.Handshake;
			Clients[client.Address].Blocks = 0;

			var file = Filesystem.ResolvePath(Options["file"]);
			if (Filesystem.Exist(file))
			{
				Clients[client.Address].FileName = file;

				if (Options.ContainsKey("blksize"))
				{
					this.Handle_Option_request(Clients[client.Address].TransferSize,
					Clients[client.Address].BlockSize, Clients[client.Address].EndPoint);
					return;
				}

				Clients[client.Address].Stage = TFTPStage.Transmitting;

				this.Readfile(Clients[client.Address].EndPoint);
				Options.Clear();
			}
			else
			{
				this.Handle_Error_Request(TFTPErrorCode.FileNotFound, file);
				return;
			}

		}

		public void SetMode(TFTPMode mode)
		{
			if (mode != TFTPMode.Octet)
				this.Handle_Error_Request(TFTPErrorCode.InvalidOption, "Invalid Option");
			else
			{
				if (Clients.ContainsKey(this.remoteEndp.Address))
					Clients[this.remoteEndp.Address].Mode = mode;
			}
		}

		internal void ExtractOptions(TFTPPacket data)
		{
			var parts = Exts.ToParts(data.Data, "\0".ToCharArray());

			for (var i = 0; i < parts.Length; i++)
			{
				if (i == 0)
				{
					if (!Options.ContainsKey("file"))
						Options.Add("file", parts[i]);
					else
						Options["file"] = parts[i];
				}

				if (i == 1)
				{
					if (!Options.ContainsKey("mode"))
						Options.Add("mode", parts[i]);
					else
						Options["mode"] = parts[i];

					this.SetMode(TFTPMode.Octet);
				}

				if (parts[i] == "blksize")
				{
					if (!Options.ContainsKey(parts[i]))
						Options.Add(parts[i], parts[i + 1]);
					else
						Options[parts[i]] = parts[i + 1];

					Clients[this.remoteEndp.Address].BlockSize = int.Parse(Options["blksize"]);
				}

				if (parts[i] == "tsize")
				{
					if (!Options.ContainsKey(parts[i]))
						Options.Add(parts[i], parts[i + 1]);
					else
						Options[parts[i]] = parts[i + 1];
				}

				if (parts[i] == "windowsize")
				{
					if (!Options.ContainsKey(parts[i]))
						Options.Add(parts[i], parts[i + 1]);
					else
						Options[parts[i]] = parts[i + 1];
				}
			}
		}

		internal override void DataReceived(object sender, DataReceivedEventArgs e)
		{
			lock (this)
			{
				this.remoteEndp = e.RemoteEndpoint;

				var request = new TFTPPacket(e.Data.Length, TFTPOPCodes.UNK);
				request.Data = e.Data;

				request.Type = SocketType.TFTP;

				if (!Clients.ContainsKey(e.RemoteEndpoint.Address))
					Clients.Add(e.RemoteEndpoint.Address, new TFTPClient(e.RemoteEndpoint));
				else
					Clients[e.RemoteEndpoint.Address].EndPoint = e.RemoteEndpoint;

				switch (request.OPCode)
				{
					case (int)TFTPOPCodes.RRQ:
						this.Handle_RRQ_Request(request, Clients[e.RemoteEndpoint.Address].EndPoint);
						break;
					case (int)TFTPOPCodes.ERR:
						if (request.ErrorCode != 0)
							this.Handle_Error_Request(request.ErrorCode, request.ErrorMessage, true);
						else
						{
							if (Clients.ContainsKey(e.RemoteEndpoint.Address))
								Clients.Remove(e.RemoteEndpoint.Address);
						}

						break;
					case (int)TFTPOPCodes.ACK:
						if (!Clients.ContainsKey(e.RemoteEndpoint.Address))
							return;

						this.Handle_ACK_Request(request, Clients[e.RemoteEndpoint.Address].EndPoint);
						break;
					default:
						this.Handle_Error_Request(TFTPErrorCode.IllegalOperation, "Unknown OPCode: {0}".F(request.OPCode));
						break;
				}
			}
		}

		internal override void DataSend(object sender, DataSendEventArgs e)
		{
			if (Clients.ContainsKey(e.RemoteEndpoint.Address) && Clients[e.RemoteEndpoint.Address].Stage == TFTPStage.Done)
				Clients.Remove(e.RemoteEndpoint.Address);
		}

		internal void Handle_Option_request(long tsize, int blksize, IPEndPoint client)
		{
			if (!Clients.ContainsKey(client.Address))
				return;

			Clients[client.Address].Stage = TFTPStage.Handshake;

			var tmpbuffer = new byte[100];
			var offset = 0;

			var blksizeopt = Encoding.ASCII.GetBytes("blksize".ToCharArray());
			Array.Copy(blksizeopt, 0, tmpbuffer, offset, blksizeopt.Length);
			offset += blksizeopt.Length + 1;

			var blksize_value = Encoding.ASCII.GetBytes(blksize.ToString().ToCharArray());
			Array.Copy(blksize_value, 0, tmpbuffer, offset, blksize_value.Length);
			offset += blksize_value.Length + 1;

			var tsizeOpt = Encoding.ASCII.GetBytes("tsize".ToCharArray());
			Array.Copy(tsizeOpt, 0, tmpbuffer, offset, tsizeOpt.Length);
			offset += tsizeOpt.Length + 1;

			var tsize_value = Encoding.ASCII.GetBytes(tsize.ToString().ToCharArray());
			Array.Copy(tsize_value, 0, tmpbuffer, offset, tsize_value.Length);
			offset += tsize_value.Length + 1;

			var packet = new TFTPPacket(2 + offset, TFTPOPCodes.OCK);
			Array.Copy(tmpbuffer, 0, packet.Data, packet.Offset, offset);
			packet.Offset += offset;

			Array.Clear(tmpbuffer, 0, tmpbuffer.Length);

			this.Send(ref packet, Clients[client.Address].EndPoint);
		}

		internal void Readfile(IPEndPoint client)
		{
			var readedBytes = 0;
			var done = false;

			// Align the last Block
			if (Clients[client.Address].TransferSize <= Clients[client.Address].BlockSize)
			{
				Clients[client.Address].BlockSize = (int)Clients[client.Address].TransferSize;
				done = true;
			}

			var chunk = new byte[Clients[client.Address].BlockSize];

			Files.Read(Clients[client.Address].FileName, ref chunk, out readedBytes,
				chunk.Length, (int)Clients[client.Address].BytesRead);

			Clients[client.Address].BytesRead += readedBytes;
			Clients[client.Address].TransferSize -= readedBytes;

			var response = new TFTPPacket(4 + chunk.Length, TFTPOPCodes.DAT);

			if (Clients[client.Address].Blocks == 0)
				Clients[client.Address].Blocks += 1;

			response.Block = Clients[client.Address].Blocks;
			Array.Copy(chunk, 0, response.Data, response.Offset, chunk.Length);
			response.Offset += chunk.Length;

			this.Send(ref response, Clients[client.Address].EndPoint);

			if (Clients.ContainsKey(client.Address) && done)
				Clients[client.Address].Stage = TFTPStage.Done;
		}
	}
}