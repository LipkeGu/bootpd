using System;
using System.Net;
using System.Net.Sockets;

namespace WDSServer.Providers
{
	abstract public class SocketProvider : Definitions
	{
		public event DataReceivedEventHandler DataReceived;
		public event DataSendEventHandler DataSend;

		protected class SocketState
		{
			public byte[] Buffer;
			public int Buffersize;
			public Socket Socket;
			public int Length;
			public SocketType Type;
		}

		protected SocketState state;
		protected MulticastOption mcastoption;
		protected EndPoint localEndPoint;
		protected IPAddress multicstAddress;
		protected EndPoint remoteEndPoint;
		protected Socket socket;
		protected Int32 sendBuffer;
		protected bool reuseAddress;
		protected SocketType type;

		public abstract SocketType Type
		{
			get; set;
		}

		protected bool broadcast;
		protected bool enablemulticast;
		protected int buffersize;

		internal abstract void received(IAsyncResult ar);

		internal void OnDataSend(int bytessend, IPEndPoint endpoint, SocketType type)
		{
			var evtargs = new DataSendEventArgs();

			evtargs.BytesSend = bytessend;
			evtargs.RemoteEndpoint = endpoint;

			if (DataSend != null)
				DataSend(this, evtargs);
		}

		internal void OnDataReceived(byte[] data, IPEndPoint endpoint, SocketType type)
		{
			var evtargs = new DataReceivedEventArgs();
			evtargs.Type = type;
			evtargs.Data = data;
			evtargs.RemoteEndpoint = endpoint;

			if (DataReceived != null)
				DataReceived(this, evtargs);
		}
	}
}
