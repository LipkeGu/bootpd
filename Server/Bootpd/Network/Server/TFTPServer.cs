using Bootpd.Common;
using Bootpd.Common.Network.Protocol.TFTP;
using Bootpd.Network.Client;
using Bootpd.Network.Packet;
using Server.Extensions;
using System;
using System.IO;

namespace Bootpd.Network.Server
{
	public class TFTPServer : BaseServer
	{
		public TFTPServer(ServerType serverType) : base(serverType)
		{
		}

		public void Handle_Error_Request(string client, string socket, TFTPPacket request)
		{
			request.Buffer.Position = 2;
			Console.WriteLine("[E] TFTP: ({0}): {1}!", request.ErrorCode, request.ErrorMessage);

			BootpdCommon.Clients.Remove(client);
		}

		public void Handle_Read_Request(string client, string socket, TFTPPacket request)
		{
			if (request.Options.ContainsKey("tsize"))
				((TFTPClient)BootpdCommon.Clients[client]).BytesToRead = ushort.Parse(request.Options["tsize"]);

			((TFTPClient)BootpdCommon.Clients[client]).FileName = Filesystem.ResolvePath(Path.Combine(BootpdCommon.TFTPRoot, request.Options["file"]));

			var FileisOpen = ((TFTPClient)BootpdCommon.Clients[client]).OpenFile();

			var response = new TFTPPacket(!FileisOpen ? TFTPMsgType.ERR : TFTPMsgType.OCK, 2);

			if (!FileisOpen)
			{
				response.Block = (ushort)TFTPErrorCode.FileNotFound;
				response.Buffer.Write(request.Options["file"].GetBytes(), 4);
				response.SetCapacity(4 + request.Options["file"].GetBytes().Length);
				Console.WriteLine("[E] TFTP: File not found: {0}", request.Options["file"]);
			}
			else
			{
				if (request.Options.ContainsKey("blksize"))
				{
					((TFTPClient)BootpdCommon.Clients[client]).Blocksize = ushort.Parse(request.Options["blksize"]);
				}

				if (request.Options.ContainsKey("windowsize"))
					((TFTPClient)BootpdCommon.Clients[client]).WindowSize = ushort.Parse(request.Options["windowsize"]);

				((TFTPClient)BootpdCommon.Clients[client]).Block = 0;

				if (request.Options.ContainsKey("tsize"))
					response.Options.Add("tsize", string.Format("{0}",
						((TFTPClient)BootpdCommon.Clients[client]).BytesToRead));

				if (request.Options.ContainsKey("blksize"))
					response.Options.Add("blksize", string.Format("{0}", ((TFTPClient)BootpdCommon.Clients[client]).Blocksize));

				if (request.Options.ContainsKey("windowsize"))
					response.Options.Add("windowsize", string.Format("{0}", ((TFTPClient)BootpdCommon.Clients[client]).WindowSize));

				response.CommitOptions();
			}

			Sockets[socket].Send(((TFTPClient)BootpdCommon.Clients[client]).RemoteEndpoint, response);

			if (!FileisOpen)
				BootpdCommon.Clients.Remove(client);
		}

		public void Handle_Ack_Request(string client, string socket, TFTPPacket request)
		{
			if (request.Block != ((TFTPClient)BootpdCommon.Clients[client]).Block)
			{
				Console.WriteLine("[E] Client is out of Sync!");
				((TFTPClient)BootpdCommon.Clients[client]).FileStream?.Close();
				BootpdCommon.Clients.Remove(client);
				return;
			}

			if (!BootpdCommon.Clients.ContainsKey(client))
				return;

			for (var i = 0; i < ((TFTPClient)BootpdCommon.Clients[client]).WindowSize; i++)
			{
				((TFTPClient)BootpdCommon.Clients[client]).OpenFile();
				var readedBytes = 0;

				var data = ((TFTPClient)BootpdCommon.Clients[client]).ReadChunk(out readedBytes);

				((TFTPClient)BootpdCommon.Clients[client]).BytesRead += readedBytes;

				using (var response = new TFTPPacket(TFTPMsgType.DAT, readedBytes))
				{
					((TFTPClient)BootpdCommon.Clients[client]).Block++;

					response.Block = ((TFTPClient)BootpdCommon.Clients[client]).Block;
					response.Buffer.Write(data, 4);
					response.SetCapacity(4 + readedBytes);
					response.CommitOptions();

					Sockets[socket].Send(((TFTPClient)BootpdCommon.Clients[client]).RemoteEndpoint, response);
				}

				if (data.Length < ((TFTPClient)BootpdCommon.Clients[client]).Blocksize)
					break;

				if (((TFTPClient)BootpdCommon.Clients[client]).BytesRead == ((TFTPClient)BootpdCommon.Clients[client]).BytesToRead)
					break;

				((TFTPClient)BootpdCommon.Clients[client]).CloseFile();
			}


			if (((TFTPClient)BootpdCommon.Clients[client]).BytesRead == ((TFTPClient)BootpdCommon.Clients[client]).BytesToRead)
				BootpdCommon.Clients.Remove(client);
		}
	}
}
