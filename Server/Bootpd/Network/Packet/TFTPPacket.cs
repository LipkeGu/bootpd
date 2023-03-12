using Bootpd.Common.Network.Protocol.TFTP;

namespace Bootpd.Network.Packet
{
	public class TFTPPacket : BasePacket
	{
		public TFTPMsgType MessageType
		{
			get
			{
				Buffer.Position = 0;
				return (TFTPMsgType)Buffer.ToUInt16().LE16();
			}
			set
			{

				Buffer.Position = 0;
				Buffer.Write(((ushort)value).LE16());
			}
		}

		public ushort Block
		{
			get
			{
				Buffer.Position = 2;
				return Buffer.ToUInt16().LE16();
			}

			set
			{
				Buffer.Position = 2;
				Buffer.Write(value.LE16());
			}
		}

		public TFTPPacket(byte[] data) : base(data)
		{

			ParsePacket();
		}

		public override void ParsePacket()
		{

		}

		public override void CommitOptions()
		{
		}
	}
}
