namespace Server.Network
{
	using System.Net;

	public abstract class ClientProvider
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
