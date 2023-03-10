namespace Server.Network
{
	using System.Net;

	public abstract class ServerProvider
	{
		protected IPEndPoint endp;
		protected IPEndPoint remoteEndp;
		protected SocketType type;

		protected IPEndPoint LocalEndPoint
		{
			get; set;
		}

		protected SocketType Type
		{
			get; set;
		}

		internal abstract void DataReceived(object sender, DataReceivedEventArgs e);
		internal abstract void DataSend(object sender, DataSendEventArgs e);
	}
}
