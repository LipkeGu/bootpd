using System.IO;

namespace bootpd.Bootpd.Network
{
	public interface IPacket
	{
		MemoryStream Buffer { get; set; }

		int Length { get; set; }

	}
}
