using System.Net;

namespace WDSServer.Providers
{
	public abstract class ClientProvider : Definitions
	{
		protected IPEndPoint endp;
		public abstract IPEndPoint EndPoint
		{
			get; set;
		}

		protected SocketType type;
		public abstract SocketType Type
		{
			get; set;
		}
	}
}