namespace bootpd
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Net;
	using System.Threading;
	using System.Threading.Tasks;

	public sealed class TFTP : ServerProvider, ITFTPServer_Provider, IDisposable
	{
		public Dictionary<IPAddress, TFTPClient> Clients;
		public Dictionary<string, string> Options;

		TFTPSocket socket;

		public TFTP(IPEndPoint endpoint)
		{
			this.Clients = new Dictionary<IPAddress, TFTPClient>();
			this.Options = new Dictionary<string, string>();
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
			foreach (var client in Clients)
				client.Value.Dispose();

			Clients.Clear();
			Options.Clear();

			this.socket.Dispose();
		}

		public void Handle_ACK_Request(object data)
		{
			lock (Clients)
			{
				var packet = (TFTPPacket)data;
				if (!Clients.ContainsKey(packet.Source.Address))
					return;

				if (packet.Block == Clients[packet.Source.Address].Blocks)
				{
					Clients[packet.Source.Address].Blocks += 1;

					this.Readfile(packet.Source);
				}
			}
		}

		public void Handle_Error_Request(TFTPErrorCode error, string message, IPEndPoint client, bool clientError = false)
		{
			lock (client)
			{
				if (!Clients.ContainsKey(client.Address))
					return;

				Clients[client.Address].Stage = TFTPStage.Error;

				if (!clientError)
				{
					var response = new TFTPPacket(5 + message.Length, TFTPOPCodes.ERR, client);
					response.ErrorCode = error;
					response.ErrorMessage = message;

					this.Send(ref response);
				}

				Errorhandler.Report(LogTypes.Error, "[TFTP] {0}: {1}".F(error, message));

				if (Clients[client.Address].FileStream != null)
					Clients[client.Address].FileStream.Close();

				if (Clients[client.Address].BufferedStream != null)
					Clients[client.Address].BufferedStream.Close();

				Clients.Remove(client.Address);
			}
		}

		public void Send(ref TFTPPacket packet)
		{
			this.socket.Send(packet.Source, packet);
		}

		public void Handle_RRQ_Request(object request)
		{
			lock (Clients)
			{
				var packet = (TFTPPacket)request;

				if (!Clients.ContainsKey(packet.Source.Address))
					Clients.Add(packet.Source.Address, new TFTPClient(packet.Source));
				else
					Clients[packet.Source.Address].EndPoint = packet.Source;

				this.ExtractOptions(ref packet);

				if (!Clients.ContainsKey(packet.Source.Address))
					return;

				Clients[packet.Source.Address].Stage = TFTPStage.Handshake;
				Clients[packet.Source.Address].Blocks = 0;

				lock (Options)
				{
					var file = Filesystem.ResolvePath(Options["file"]);

					if (file == Settings.TFTPRoot.ToLowerInvariant())
					{
						this.Handle_Error_Request(TFTPErrorCode.AccessViolation, "Directories are not supported!", packet.Source);
						return;
					}

					if (Filesystem.Exist(file) && !string.IsNullOrEmpty(file))
					{
						Clients[packet.Source.Address].FileName = file;

						Clients[packet.Source.Address].FileStream = new FileStream(Clients[packet.Source.Address].FileName,
						 FileMode.Open, FileAccess.Read, FileShare.Read, Settings.ReadBuffer, FileOptions.SequentialScan);
						Clients[packet.Source.Address].TransferSize = Clients[packet.Source.Address].FileStream.Length;

						Clients[packet.Source.Address].BufferedStream = new BufferedStream(Clients[packet.Source.Address].FileStream, Settings.ReadBuffer);

						if (Options.ContainsKey("blksize"))
						{
							this.Handle_Option_request(Clients[packet.Source.Address].TransferSize,
							Clients[packet.Source.Address].BlockSize, Clients[packet.Source.Address].WindowSize, Clients[packet.Source.Address].EndPoint);

							return;
						}

						Options.Clear();

						Clients[packet.Source.Address].Stage = TFTPStage.Transmitting;
						this.Readfile(Clients[packet.Source.Address].EndPoint);
					}
					else
						this.Handle_Error_Request(TFTPErrorCode.FileNotFound, file, packet.Source);
				}
			}
		}

		internal void ExtractOptions(ref TFTPPacket data)
		{
			var parts = Exts.ToParts(data.Data, "\0");

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

					if (Clients.ContainsKey(data.Source.Address))
						Clients[data.Source.Address].Mode = TFTPMode.Octet;
				}

				if (parts[i] == "blksize")
				{
					if (!Options.ContainsKey(parts[i]))
						Options.Add(parts[i], parts[i + 1]);
					else
						Options[parts[i]] = parts[i + 1];

					if (Clients.ContainsKey(data.Source.Address))
						Clients[data.Source.Address].BlockSize = int.Parse(Options["blksize"]);
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
			var request = new TFTPPacket(e.Data.Length, TFTPOPCodes.UNK, e.RemoteEndpoint);
			request.Data = e.Data;
			request.Type = SocketType.TFTP;

			switch (request.OPCode)
			{
				case TFTPOPCodes.RRQ:
					var rrq_thread = new Thread(new ParameterizedThreadStart(Handle_RRQ_Request));
					rrq_thread.Start(request);
					break;
				case TFTPOPCodes.ERR:
					this.Handle_Error_Request(request.ErrorCode, request.ErrorMessage, request.Source, true);
					break;
				case TFTPOPCodes.ACK:
					var ack_thread = new Thread(new ParameterizedThreadStart(Handle_ACK_Request));
					ack_thread.Start(request);
					break;
				default:
					this.Handle_Error_Request(TFTPErrorCode.IllegalOperation, "Unknown OPCode: {0}".F(request.OPCode), request.Source);
					break;
			}
		}

		internal override void DataSend(object sender, DataSendEventArgs e)
		{
			lock (Clients)
			{
				if (!Clients.ContainsKey(e.RemoteEndpoint.Address))
					return;

				if (Clients[e.RemoteEndpoint.Address].Stage != TFTPStage.Done)
					return;

				if (Clients[e.RemoteEndpoint.Address].FileStream != null)
					Clients[e.RemoteEndpoint.Address].FileStream.Close();

				if (Clients[e.RemoteEndpoint.Address].BufferedStream != null)
					Clients[e.RemoteEndpoint.Address].BufferedStream.Close();

				Clients.Remove(e.RemoteEndpoint.Address);
			}
		}

		internal void Handle_Option_request(long tsize, int blksize, int winsize, IPEndPoint client)
		{
			lock (Clients)
			{
				if (!Clients.ContainsKey(client.Address))
					return;

				Clients[client.Address].Stage = TFTPStage.Handshake;

				var tmpbuffer = new byte[100];
				var offset = 0;


				var blksizeopt = Exts.StringToByte("blksize");
				offset += Functions.CopyTo(ref blksizeopt, 0, ref tmpbuffer, offset, blksizeopt.Length) + 1;
				
				var blksize_value = Exts.StringToByte(blksize.ToString());
				offset += Functions.CopyTo(ref blksize_value, 0, ref tmpbuffer, offset, blksize_value.Length) + 1;
				

				var tsizeOpt = Exts.StringToByte("tsize");
				offset += Functions.CopyTo(ref tsizeOpt, 0, ref tmpbuffer, offset, tsizeOpt.Length) + 1;
				
				var tsize_value = Exts.StringToByte(tsize.ToString());
				offset += Functions.CopyTo(ref tsize_value, 0, ref tmpbuffer, offset, tsize_value.Length) + 1;

				var winOpt = Exts.StringToByte("windowsize");
				offset += Functions.CopyTo(ref winOpt, 0, ref tmpbuffer, offset, winOpt.Length) + 1;

				var winsize_value = Exts.StringToByte(winsize.ToString());
				offset += Functions.CopyTo(ref winsize_value, 0, ref tmpbuffer, offset, winsize_value.Length) + 1;
				
				var packet = new TFTPPacket(2 + offset, TFTPOPCodes.OCK, client);
				Array.Copy(tmpbuffer, 0, packet.Data, packet.Offset, offset);
				packet.Offset += offset;

				Array.Clear(tmpbuffer, 0, tmpbuffer.Length);

				this.Send(ref packet);
			}
		}

		internal void Readfile(IPEndPoint client)
		{
			lock (Clients)
			{
				var readedBytes = 0L;
				var done = false;

				if (!Clients.ContainsKey(client.Address))
					return;

				if (Clients[client.Address].FileStream == null)
				{
					Clients[client.Address].FileStream = new FileStream(Clients[client.Address].FileName,
					FileMode.Open, FileAccess.Read, FileShare.Read, Settings.ReadBuffer, FileOptions.SequentialScan);

					if (Clients[client.Address].BufferedStream == null)
						Clients[client.Address].BufferedStream = new BufferedStream(Clients[client.Address].FileStream, Settings.ReadBuffer);
				}

				// Align the last Block
				if (Clients[client.Address].TransferSize <= Clients[client.Address].BlockSize)
				{
					Clients[client.Address].BlockSize = Convert.ToInt32(Clients[client.Address].TransferSize);
					done = true;
				}

				var chunk = new byte[Clients[client.Address].BlockSize];

				Clients[client.Address].BufferedStream.Seek(Clients[client.Address].BytesRead, SeekOrigin.Begin);
				readedBytes = Clients[client.Address].BufferedStream.Read(chunk, 0, chunk.Length);

				Clients[client.Address].BytesRead += readedBytes;
				Clients[client.Address].TransferSize -= readedBytes;

				var response = new TFTPPacket(4 + chunk.Length, TFTPOPCodes.DAT, client);

				if (Clients[client.Address].Blocks == 0)
					Clients[client.Address].Blocks += 1;

				response.Block = Convert.ToInt16(Clients[client.Address].Blocks);
				Array.Copy(chunk, 0, response.Data, response.Offset, chunk.Length);
				response.Offset += chunk.Length;

				this.Send(ref response);

				if (done && Clients.ContainsKey(client.Address))
					Clients[client.Address].Stage = TFTPStage.Done;
			}
		}
	}
}