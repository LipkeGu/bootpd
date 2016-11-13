namespace bootpd
{
	using System.Net;

	public abstract class ServerProvider : Definitions
	{
		protected IPEndPoint endp;
		protected IPEndPoint remoteEndp;
		protected SocketType type;

		public abstract IPEndPoint LocalEndPoint
		{
			get; set;
		}

		public abstract SocketType Type
		{
			get; set;
		}

		internal abstract void DataReceived(object sender, DataReceivedEventArgs e);
		internal abstract void DataSend(object sender, DataSendEventArgs e);
	}
}
