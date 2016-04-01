namespace bootpd
{
	public abstract class PacketProvider : Definitions
	{
		protected byte[] data;
		protected int offset;
		protected int length;
		protected SocketType type;

		public abstract byte[] Data
		{
			get; set;
		}

		public abstract int Length
		{
			get; set;
		}

		public abstract byte[] Add
		{
			set;
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
