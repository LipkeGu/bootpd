using System.IO;

namespace Bootpd.Network.Packet
{
	public interface IPacket
	{
		MemoryStream Buffer { get; set; }

		long Length { get; set; }

		void CommitOptions();
	}
}
