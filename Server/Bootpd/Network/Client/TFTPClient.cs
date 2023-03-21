using Bootpd.Common;
using Server.Extensions;
using System;
using System.IO;
using System.Net;

namespace Bootpd.Network.Client
{
	public class TFTPClient : BaseClient
	{
		public FileStream FileStream { get; set; }

		public long BytesToRead { get; set; } = 0;

		public string FileName { get; set; } = string.Empty;

		public long BytesRead { get; set; } = 0;

		public long WindowSize { get; set; } = 1;

		public ushort Blocksize { get; set; } = 0;

		public ushort Block { get; set; } = 0;

		public TFTPClient(string id, ServerType serverType, IPEndPoint endpoint, bool local) : base(id, serverType, endpoint, local)
		{

		}

		public bool OpenFile()
		{
			try
			{
				var fil = new FileInfo(Filesystem.ResolvePath(Path.Combine(BootpdCommon.TFTPRoot, FileName)));
				if (fil.Exists)
				{

					FileStream = new FileStream(fil.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
					BytesToRead = FileStream.Length;
					FileStream.Position = 0;

					if (BytesRead == BytesToRead)
						CloseFile();

					return true;
				}
				else
					return false;
			}
			catch (Exception ex)
			{
				if (BytesRead == BytesToRead)
					CloseFile();

				Console.WriteLine(ex.Message);
				return false;
			}

		}

		public byte[] ReadChunk(out int readedBytes)
		{
			var chunksize = Blocksize;

			if (BytesToRead - BytesRead < Blocksize)
				chunksize = (ushort)(BytesToRead - BytesRead);

			var buffer = new byte[chunksize];

			FileStream.Seek(BytesRead, SeekOrigin.Current);
			readedBytes = FileStream.Read(buffer, 0, buffer.Length);

			return buffer;
		}

		public void CloseFile()
		{
			if (FileStream == null)
				return;

			FileStream.Close();
			FileStream.Dispose();
		}
	}
}
