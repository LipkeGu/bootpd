using System;
using System.IO;

namespace Bootpd.Network.Packet
{
	public abstract class BasePacket : IPacket, IDisposable
	{
		public MemoryStream Buffer { get; set; }
		public long Length { get; set; }

		public BasePacket(byte[] data)
		{
			Buffer = new MemoryStream(data);
			Length = Buffer.Length;
		}

		public BasePacket()
		{
		}

		public void SetCapacity(int size)
		{
			Buffer.Capacity = size;
		}

		public void Dump(string filename)
		{
			var buffer = new byte[Buffer.Length];
			Buffer.Read(buffer, 0, buffer.Length);

			File.WriteAllBytes(filename, buffer);
		}

		public abstract void ParsePacket();

		public void Dispose()
		{
			Buffer.Close();
			Buffer.Dispose();
		}

		public abstract void CommitOptions();
	}
}
