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

		public void Dispose()
		{
			Buffer.Close();
			Buffer.Dispose();
		}

		public abstract void CommitOptions();
	}
}
