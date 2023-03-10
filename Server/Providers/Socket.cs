namespace Server.Network
{
	using System;
	using System.Net;
	using System.Net.Sockets;

	public abstract class SocketProvider
	{
		protected SocketState state;
		protected SocketType type;
		protected Socket socket;

		protected EndPoint localEndPoint;
		protected EndPoint remoteEndPoint;

		protected int sendBuffer;
		protected int buffersize;

		protected bool reuseAddress;
		protected bool broadcast;
		protected bool enablemulticast;
		
		public event DataReceivedEventHandler DataReceived;

		public event DataSendEventHandler DataSend;

		public abstract SocketType Type
		{
			get; set;
		}

		internal abstract void Received(IAsyncResult ar);

		internal void OnDataSend(int bytessend, IPEndPoint endpoint, SocketType type)
		{
			var evtargs = new DataSendEventArgs
			{
				BytesSend = bytessend,
				RemoteEndpoint = endpoint
			};

			DataSend?.Invoke(this, evtargs);
		}

		internal void OnDataReceived(byte[] data, IPEndPoint endpoint, SocketType type)
		{
			var evtargs = new DataReceivedEventArgs
			{
				Type = type,
				Data = data,
				RemoteEndpoint = endpoint
			};

			DataReceived?.Invoke(this, evtargs);
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
