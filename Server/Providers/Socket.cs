namespace bootpd
{
	using System;
	using System.Net;
	using System.Net.Sockets;

	public abstract class SocketProvider : Definitions
	{
		protected SocketState state;
		protected MulticastOption mcastoption;
		protected EndPoint localEndPoint;
		protected IPAddress multicstAddress;
		protected EndPoint remoteEndPoint;
		protected Socket socket;
		protected int sendBuffer;
		protected bool reuseAddress;
		protected SocketType type;
		protected bool broadcast;
		protected bool enablemulticast;
		protected int buffersize;

		public event DataReceivedEventHandler DataReceived;

		public event DataSendEventHandler DataSend;

		public abstract SocketType Type
		{
			get; set;
		}

		internal abstract void Received(IAsyncResult ar);

		internal void OnDataSend(int bytessend, IPEndPoint endpoint, SocketType type)
		{
			var evtargs = new DataSendEventArgs();

			evtargs.BytesSend = bytessend;
			evtargs.RemoteEndpoint = endpoint;

			this.DataSend?.Invoke(this, evtargs);
		}

		internal void OnDataReceived(byte[] data, IPEndPoint endpoint, SocketType type)
		{
			var evtargs = new DataReceivedEventArgs();
			evtargs.Type = type;
			evtargs.Data = data;
			evtargs.RemoteEndpoint = endpoint;

			this.DataReceived?.Invoke(this, evtargs);
		}

		protected class SocketState
		{
			public byte[] Buffer;
			public int Buffersize;
			public Socket Socket;
			public int Length;
			public SocketType Type;
		}
	}
}
