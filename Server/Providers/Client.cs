namespace bootpd
{
	using System.Net;

	public abstract class ClientProvider : Definitions
	{
		protected IPEndPoint endp;

		protected SocketType type;

		public abstract IPEndPoint EndPoint
		{
			get; set;
		}

		public abstract SocketType Type
		{
			get; set;
		}
	}
}