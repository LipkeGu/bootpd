using System.IO;

namespace bootpd.Bootpd.Network.Packet
{
	public class BasePacket : IPacket
	{
		public MemoryStream Buffer { get; set; }

		public BasePacket(byte[] data)
		{
			Buffer = new MemoryStream(data);
		}
	}
}
