namespace WDSServer.Network
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Text;
	using System.Threading;
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

		public void Handle_ACK_Request(object data)
		{
			if (!Clients.ContainsKey(this.remoteEndp.Address) || Clients[this.remoteEndp.Address].FileName == string.Empty)
				return;

			var request = (TFTPPacket)data;
			if (request.Block == Clients[this.remoteEndp.Address].Blocks)
			{
				Clients[this.remoteEndp.Address].Blocks += 1;

				this.Readfile();
			}
		}

		public void Handle_Error_Request(TFTPErrorCode error, string message, bool clientError = false)
		{
			if (Clients.ContainsKey(this.remoteEndp.Address))
				Clients[this.remoteEndp.Address].Stage = TFTPStage.Error;

			if (!clientError)
			{
				var response = new TFTPPacket(5 + message.Length, TFTPOPCodes.ERR);
				response.ErrorCode = error;
				response.ErrorMessage = message;

				this.Send(ref response, Clients[this.remoteEndp.Address].EndPoint);
			}

			Errorhandler.Report(LogTypes.Error, "[TFTP] Error {0}: {1}".F(error, message));

			if (Clients.ContainsKey(this.remoteEndp.Address))
				Clients.Remove(this.remoteEndp.Address);
		}

		public void Send(ref TFTPPacket packet, IPEndPoint endpoint)
		{
			if (Clients.ContainsKey(this.remoteEndp.Address))
				this.socket.Send(endpoint, packet);
		}

		public void Handle_RRQ_Request(object packet)
		{
			if (Clients.Count == 0)
				return;

			var request = (TFTPPacket)packet;
			this.ExtractOptions(request);

			Clients[this.remoteEndp.Address].Stage = TFTPStage.Handshake;
			Clients[this.remoteEndp.Address].Blocks = 0;

			Clients[this.remoteEndp.Address].FileName = Filesystem.ResolvePath(Options["file"]);
			if (!Filesystem.Exist(Clients[this.remoteEndp.Address].FileName))
			{
				this.Handle_Error_Request(TFTPErrorCode.FileNotFound, Clients[this.remoteEndp.Address].FileName);
				return;
			}
			else
			{
				Clients[this.remoteEndp.Address].TransferSize = Filesystem.Size(Clients[this.remoteEndp.Address].FileName);

				if (Options.ContainsKey("blksize"))
				{
					this.Handle_Option_request(Clients[this.remoteEndp.Address].TransferSize, Clients[this.remoteEndp.Address].BlockSize);
					return;
				}

				Clients[this.remoteEndp.Address].Stage = TFTPStage.Transmitting;

				this.Readfile();
				Options.Clear();
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

				if (!Clients.ContainsKey(this.remoteEndp.Address))
					Clients.Add(this.remoteEndp.Address, new TFTPClient(this.remoteEndp));

				Clients[e.RemoteEndpoint.Address].EndPoint = e.RemoteEndpoint;

				switch (request.OPCode)
				{
					case (int)TFTPOPCodes.RRQ:
						var rrq_thread = new Thread(new ParameterizedThreadStart(this.Handle_RRQ_Request));
						rrq_thread.Start(request);
						break;
					case (int)TFTPOPCodes.ERR:
						if (request.ErrorCode != 0)
							this.Handle_Error_Request(request.ErrorCode, request.ErrorMessage, true);
						else
						{
							if (Clients.ContainsKey(this.remoteEndp.Address))
								Clients.Remove(this.remoteEndp.Address);
						}

						break;
					case (int)TFTPOPCodes.ACK:
						if (!Clients.ContainsKey(this.remoteEndp.Address))
							return;

						var ack_thread = new Thread(new ParameterizedThreadStart(this.Handle_ACK_Request));
						ack_thread.Start(request);
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

		internal void Handle_Option_request(long tsize, int blksize)
		{
			Clients[this.remoteEndp.Address].Stage = TFTPStage.Handshake;

			var tmpbuffer = new byte[512];
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

			this.Send(ref packet, Clients[this.remoteEndp.Address].EndPoint);
		}

		internal void Readfile()
		{
			var readedBytes = 0;
			var done = false;

			// Align the last Block
			if (Clients[this.remoteEndp.Address].TransferSize <= Clients[this.remoteEndp.Address].BlockSize)
			{
				Clients[this.remoteEndp.Address].BlockSize = (int)Clients[this.remoteEndp.Address].TransferSize;
				done = true;
			}

			var chunk = new byte[Clients[this.remoteEndp.Address].BlockSize];

			Files.Read(Clients[this.remoteEndp.Address].FileName, ref chunk, out readedBytes,
				chunk.Length, (int)Clients[this.remoteEndp.Address].BytesRead);

			Clients[this.remoteEndp.Address].BytesRead += readedBytes;
			Clients[this.remoteEndp.Address].TransferSize -= readedBytes;

			var response = new TFTPPacket(4 + chunk.Length, TFTPOPCodes.DAT);

			if (Clients[this.remoteEndp.Address].Blocks == 0)
				Clients[this.remoteEndp.Address].Blocks += 1;

			response.Block = Clients[this.remoteEndp.Address].Blocks;
			Array.Copy(chunk, 0, response.Data, response.Offset, chunk.Length);
			response.Offset += chunk.Length;

			Array.Clear(chunk, 0, chunk.Length);

			this.Send(ref response, Clients[this.remoteEndp.Address].EndPoint);

			if (Clients.ContainsKey(this.remoteEndp.Address) && done)
				Clients[this.remoteEndp.Address].Stage = TFTPStage.Done;
		}
	}
}