namespace Server.Network
{
	using Bootpd;
	using Bootpd.Common.Network.Protocol.TFTP;
	using Extensions;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Net;
	using System.Text;
	using System.Threading;
	using static Bootpd.Functions;
	using static Functions;
	public sealed class TFTP : ServerProvider, ITFTPServer_Provider, IDisposable
	{
		public Dictionary<IPAddress, aTFTPClient> Clients;
		public Dictionary<string, string> Options;

		TFTPSocket socket;

		public TFTP(IPEndPoint endpoint)
		{
			Clients = new Dictionary<IPAddress, aTFTPClient>();
			Options = new Dictionary<string, string>();
			endp = endpoint;
			socket = new TFTPSocket(endp);
			socket.DataReceived += DataReceived;
			socket.DataSend += DataSend;

			Directories.Create(Settings.TFTPRoot);
		}

		~TFTP()
		{
			Dispose();
		}

		public void Dispose()
		{
			foreach (var client in Clients)
				client.Value.Dispose();

			Clients.Clear();
			Options.Clear();

			socket.Dispose();
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
							Readfile(packet.Source);

							if (Clients[packet.Source.Address].Stage == TFTPStage.Done ||
								Clients[packet.Source.Address].Stage == TFTPStage.Error)
								break;
						}
					else
						Readfile(packet.Source);

					if (Clients[packet.Source.Address].Stage == TFTPStage.Done ||
						Clients[packet.Source.Address].Stage == TFTPStage.Error)
					{
						Errorhandler.Report(LogTypes.Info, "[TFTP] {0}: {1}"
							.F(Clients[packet.Source.Address].ID, "Transfer completed!"));

						Clients[packet.Source.Address].Dispose();
						Clients.Remove(packet.Source.Address);

					}
				}
			}
		}

		public void Handle_ERR_Request(TFTPErrorCode error, string message, IPEndPoint client, bool clientError = false)
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

					Send(ref response);

					return;
				}

				if (error != TFTPErrorCode.Unknown)
					Errorhandler.Report(LogTypes.Error, "[TFTP] {0}: {1}".F(error, message));
			}
		}

		public void Send(ref TFTPPacket packet)
		{
			socket.Send(packet.Source, packet);
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
					Clients.Add(packet.Source.Address, new aTFTPClient(packet.Source));
					Clients[packet.Source.Address].Stage = TFTPStage.Handshake;

					ExtractOptions(ref packet);
				}

				var file = Filesystem.ResolvePath(Options["file"], Settings.TFTPRoot);
				if (file == Settings.TFTPRoot.ToLowerInvariant())
				{
					Handle_ERR_Request(TFTPErrorCode.AccessViolation,
						"Directories are not supported!", packet.Source);

					return;
				}

				if (Filesystem.Exist(file) && !string.IsNullOrEmpty(file))
				{
					Clients[packet.Source.Address].FileName = file;
					Clients[packet.Source.Address].FileStream = new FileStream(Clients[packet.Source.Address].FileName,
					 FileMode.Open, FileAccess.Read, FileShare.Read, Settings.ReadBuffer, FileOptions.SequentialScan);

					Clients[packet.Source.Address].TransferSize = Clients[packet.Source.Address].FileStream.Length;

					if (Options.ContainsKey("blksize"))
					{
						Clients[packet.Source.Address].BlockSize = CalcBlocksize(Clients[packet.Source.Address].FileStream.Length,
							Convert.ToUInt16(Clients[packet.Source.Address].BlockSize));

						Handle_Option_request(Clients[packet.Source.Address].TransferSize,
						Clients[packet.Source.Address].BlockSize, Clients[packet.Source.Address].WindowSize,
						Clients[packet.Source.Address].MSFTWindow, Clients[packet.Source.Address].EndPoint);

						return;
					}

					Options.Clear();

					Clients[packet.Source.Address].Stage = TFTPStage.Transmitting;
					if (Clients[packet.Source.Address].WindowSize > 1)
						for (var i = 0; i < Clients[packet.Source.Address].WindowSize; i++)
							Readfile(packet.Source);
					else
						Readfile(packet.Source);
				}
				else
					Handle_ERR_Request(TFTPErrorCode.FileNotFound, file, packet.Source);
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
					var rrq_thread = new Thread(new ParameterizedThreadStart(Handle_RRQ_Request));
					rrq_thread.Start(request);
					break;
				case TFTPOPCodes.ERR:
					Handle_ERR_Request(request.ErrorCode, request.ErrorMessage, request.Source, true);
					break;
				case TFTPOPCodes.ACK:
					var ack_thread = new Thread(new ParameterizedThreadStart(Handle_ACK_Request));
					ack_thread.Start(request);
					break;
				default:
					Handle_ERR_Request(TFTPErrorCode.IllegalOperation, "Unknown TFTP OPCode: {0}".F(request.OPCode), request.Source);
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
				offset += CopyTo(blksizeopt, 0, tmpbuffer, offset) + 1;

				var blksize_value = Exts.StringToByte(blksize.ToString(), Encoding.ASCII);
				offset += CopyTo(blksize_value, 0, tmpbuffer, offset) + 1;

				var tsizeOpt = Exts.StringToByte("tsize", Encoding.ASCII);
				offset += CopyTo(tsizeOpt, 0, tmpbuffer, offset) + 1;

				var tsize_value = Exts.StringToByte(tsize.ToString(), Encoding.ASCII);
				offset += CopyTo(tsize_value, 0, tmpbuffer, offset) + 1;

				if (winsize > 1)
				{
					var winOpt = Exts.StringToByte("windowsize", Encoding.ASCII);
					offset += CopyTo(winOpt, 0, tmpbuffer, offset) + 1;

					var winsize_value = Exts.StringToByte(winsize.ToString(), Encoding.ASCII);
					offset += CopyTo(winsize_value, 0, tmpbuffer, offset) + 1;

					if (Settings.AllowVariableWindowSize)
					{
						var mswinOpt = Exts.StringToByte("msftwindow", Encoding.ASCII);
						offset += CopyTo(mswinOpt, 0, tmpbuffer, offset) + 1;

						var mswinsize_value = Exts.StringToByte(mswinsize.ToString(), Encoding.ASCII);
						offset += CopyTo(mswinsize_value, 0, tmpbuffer, offset) + 1;
					}
				}

				var packet = new TFTPPacket(2 + offset, TFTPOPCodes.OCK, client);
				Array.Copy(tmpbuffer, 0, packet.Data, packet.Offset, offset);
				Array.Clear(tmpbuffer, 0, tmpbuffer.Length);

				packet.Offset += offset;
				Send(ref packet);
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

					Clients[client.Address].FileStream.Seek(Clients[client.Address].BytesRead, SeekOrigin.Begin);
					readedBytes = Clients[client.Address].FileStream.Read(chunk, 0, chunk.Length);

					Clients[client.Address].BytesRead += readedBytes;
					Clients[client.Address].TransferSize -= readedBytes;
					Clients[client.Address].Blocks += 1;

					var response = new TFTPPacket(4 + chunk.Length, TFTPOPCodes.DAT, client);
					response.Block = Clients[client.Address].Blocks;
					response.Offset += CopyTo(chunk, 0, response.Data, response.Offset, chunk.Length);

					Send(ref response);

					if (done && Clients.ContainsKey(client.Address))
						Clients[client.Address].Stage = TFTPStage.Done;
				}
				catch (OverflowException ex)
				{
					Handle_ERR_Request(TFTPErrorCode.AccessViolation, ex.Message, client);
				}
			}
		}
	}
}
