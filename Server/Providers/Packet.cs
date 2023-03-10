namespace Server.Network
{
	public abstract class PacketProvider
	{
		public abstract byte[] Data
		{
			get; set;
		}

		public abstract int Length
		{
			get; set;
		}

		public abstract int Offset
		{
			get; set;
		}

		public abstract SocketType Type
		{
			get; set;
		}
	}
}
