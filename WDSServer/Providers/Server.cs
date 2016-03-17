using System.Net;

namespace WDSServer.Providers
{
	abstract public class ServerProvider : Definitions
	{
		protected IPEndPoint endp;
		public abstract IPEndPoint LocalEndPoint
		{
			get; set;
		}

		protected IPEndPoint remoteEndp;

		protected SocketType type;
		public abstract SocketType Type
		{
			get; set;
		}

		internal abstract void DataReceived(object sender, DataReceivedEventArgs e);
		internal abstract void DataSend(object sender, DataSendEventArgs e);
	}
}
