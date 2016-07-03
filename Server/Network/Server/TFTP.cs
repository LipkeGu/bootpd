namespace bootpd
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Net;
	using System.Text;
	using System.Threading;

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
			var packet = (TFTPPacket)data;
			lock (Clients)
			{
				if (!Clients.ContainsKey(packet.Source.Address))
					return;

				if (packet.Block == Clients[packet.Source.Address].Blocks || packet.Block == 0)
				{
					if (packet.MSFTWindow != 0 && Settings.AllowVariableWindowSize)
						Clients[packet.Source.Address].WindowSize = packet.MSFTWindow;

					if (Clients[packet.Source.Address].WindowSize > 1)
						for (var i = 0; i < Clients[packet.Source.Address].WindowSize; i++)
						{
							this.Readfile(packet.Source);

							if (Clients[packet.Source.Address].Stage == TFTPStage.Done ||
								Clients[packet.Source.Address].Stage == TFTPStage.Error)
								break;
						}
					else
						this.Readfile(packet.Source);

					if (Clients[packet.Source.Address].Stage == TFTPStage.Done ||
						Clients[packet.Source.Address].Stage == TFTPStage.Error)
					{
						Clients[packet.Source.Address].Dispose();
						Clients.Remove(packet.Source.Address);
					}
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

					return;
				}

				Errorhandler.Report(LogTypes.Error, "[TFTP] {0}: {1}".F(error, message));
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

				if (Clients.ContainsKey(packet.Source.Address))
				{
					Clients[packet.Source.Address].Dispose();
					Clients.Remove(packet.Source.Address);
				}

				if (!Clients.ContainsKey(packet.Source.Address))
				{
					Clients.Add(packet.Source.Address, new TFTPClient(packet.Source));
					Clients[packet.Source.Address].Stage = TFTPStage.Handshake;

					this.ExtractOptions(ref packet);
				}

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
						Clients[packet.Source.Address].BlockSize = Functions.CalcBlocksize(Clients[packet.Source.Address].FileStream.Length,
							Convert.ToUInt16(Clients[packet.Source.Address].BlockSize));

						this.Handle_Option_request(Clients[packet.Source.Address].TransferSize,
						Clients[packet.Source.Address].BlockSize, Clients[packet.Source.Address].WindowSize,
						Clients[packet.Source.Address].MSFTWindow, Clients[packet.Source.Address].EndPoint);

						return;
					}

					Options.Clear();

					Clients[packet.Source.Address].Stage = TFTPStage.Transmitting;
					if (Clients[packet.Source.Address].WindowSize > 1)
						for (var i = 0; i < Clients[packet.Source.Address].WindowSize; i++)
							this.Readfile(packet.Source);
					else
						this.Readfile(packet.Source);
				}
				else
					this.Handle_Error_Request(TFTPErrorCode.FileNotFound, file, packet.Source);
			}
		}

		internal void ExtractOptions(ref TFTPPacket data)
		{
			var parts = Exts.ToParts(data.Data, "\0", Encoding.ASCII);

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
						Clients[data.Source.Address].BlockSize = ushort.Parse(Options["blksize"]);
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

					if (Clients.ContainsKey(data.Source.Address))
						Clients[data.Source.Address].WindowSize = ushort.Parse(Options["windowsize"]);
				}

				if (parts[i] == "msftwindow" && Settings.AllowVariableWindowSize)
				{
					if (!Options.ContainsKey(parts[i]))
						Options.Add(parts[i], parts[i + 1]);
					else
						Options[parts[i]] = parts[i + 1];

					if (Clients.ContainsKey(data.Source.Address))
						Clients[data.Source.Address].MSFTWindow = ushort.Parse(Options["msftwindow"]);
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
					var rrq_thread = new Thread(new ParameterizedThreadStart(this.Handle_RRQ_Request));
					rrq_thread.Start(request);
					break;
				case TFTPOPCodes.ERR:
					this.Handle_Error_Request(request.ErrorCode, request.ErrorMessage, request.Source, true);
					break;
				case TFTPOPCodes.ACK:
					var ack_thread = new Thread(new ParameterizedThreadStart(this.Handle_ACK_Request));
					ack_thread.Start(request);
					break;
				default:
					this.Handle_Error_Request(TFTPErrorCode.IllegalOperation, "Unknown OPCode: {0}".F(request.OPCode), request.Source);
					break;
			}
		}

		internal override void DataSend(object sender, DataSendEventArgs e)
		{
		}

		internal void Handle_Option_request(long tsize, ushort blksize, int winsize, ushort mswinsize, IPEndPoint client)
		{
			lock (Clients)
			{
				if (!Clients.ContainsKey(client.Address))
					return;

				Clients[client.Address].Stage = TFTPStage.Handshake;

				var tmpbuffer = new byte[100];
				var offset = 0;

				var blksizeopt = Exts.StringToByte("blksize", Encoding.ASCII);
				offset += Functions.CopyTo(ref blksizeopt, 0, ref tmpbuffer, offset, blksizeopt.Length) + 1;

				var blksize_value = Exts.StringToByte(blksize.ToString(), Encoding.ASCII);
				offset += Functions.CopyTo(ref blksize_value, 0, ref tmpbuffer, offset, blksize_value.Length) + 1;

				var tsizeOpt = Exts.StringToByte("tsize", Encoding.ASCII);
				offset += Functions.CopyTo(ref tsizeOpt, 0, ref tmpbuffer, offset, tsizeOpt.Length) + 1;

				var tsize_value = Exts.StringToByte(tsize.ToString(), Encoding.ASCII);
				offset += Functions.CopyTo(ref tsize_value, 0, ref tmpbuffer, offset, tsize_value.Length) + 1;

				if (winsize > 1)
				{
					var winOpt = Exts.StringToByte("windowsize", Encoding.ASCII);
					offset += Functions.CopyTo(ref winOpt, 0, ref tmpbuffer, offset, winOpt.Length) + 1;

					var winsize_value = Exts.StringToByte(winsize.ToString(), Encoding.ASCII);
					offset += Functions.CopyTo(ref winsize_value, 0, ref tmpbuffer, offset, winsize_value.Length) + 1;

					if (Settings.AllowVariableWindowSize)
					{
						var mswinOpt = Exts.StringToByte("msftwindow", Encoding.ASCII);
						offset += Functions.CopyTo(ref mswinOpt, 0, ref tmpbuffer, offset, mswinOpt.Length) + 1;

						var mswinsize_value = Exts.StringToByte(mswinsize.ToString(), Encoding.ASCII);
						offset += Functions.CopyTo(ref mswinsize_value, 0, ref tmpbuffer, offset, mswinsize_value.Length) + 1;
					}
				}

				var packet = new TFTPPacket(2 + offset, TFTPOPCodes.OCK, client);
				Array.Copy(tmpbuffer, 0, packet.Data, packet.Offset, offset);
				Array.Clear(tmpbuffer, 0, tmpbuffer.Length);

				packet.Offset += offset;
				this.Send(ref packet);
			}
		}

		internal void Readfile(IPEndPoint client)
		{
			lock (Clients)
			{
				try
				{
					var readedBytes = 0L;
					var done = false;

					// Align the last Block
					if (Clients[client.Address].TransferSize <= Clients[client.Address].BlockSize)
					{
						Clients[client.Address].BlockSize = Convert.ToUInt16(Clients[client.Address].TransferSize);
						done = true;
					}

					var chunk = new byte[Clients[client.Address].BlockSize];

					Clients[client.Address].BufferedStream.Seek(Clients[client.Address].BytesRead, SeekOrigin.Begin);
					readedBytes = Clients[client.Address].BufferedStream.Read(chunk, 0, chunk.Length);

					Clients[client.Address].BytesRead += readedBytes;
					Clients[client.Address].TransferSize -= readedBytes;
					Clients[client.Address].Blocks += 1;

					var response = new TFTPPacket(4 + chunk.Length, TFTPOPCodes.DAT, client);
					response.Block = Clients[client.Address].Blocks;
					response.Offset += Functions.CopyTo(chunk, 0, response.Data, response.Offset, chunk.Length);

					this.Send(ref response);

					if (done && Clients.ContainsKey(client.Address))
						Clients[client.Address].Stage = TFTPStage.Done;
				}
				catch (OverflowException ex)
				{
					this.Handle_Error_Request(TFTPErrorCode.AccessViolation, ex.Message, client);
				}
			}
		}
	}
}