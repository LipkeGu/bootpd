using System;
using System.Net;
using System.Net.Sockets;

namespace Bootpd.Network.Sockets
{
	public class SocketState
	{
		public byte[] Buffer;
		public Socket Socket;


		internal SocketState(int buffersize)
		{
			Buffer = new byte[buffersize];
		}
	}

	public partial class BaseSocket : ISocket
	{
		public EndPoint LocalEndPoint;
		public Guid Id { get; }

		SocketState state;
		public BaseSocket(Guid id, IPEndPoint endPoint)
		{
			Id = id;
			LocalEndPoint = endPoint;
			state = new SocketState(1024);
		}

		public void Bootstrap()
		{
			state.Socket.Bind(LocalEndPoint);
		}

		public void Dispose()
		{
			state.Socket.Close();
		}

		void Received(IAsyncResult ar)
		{
			state = (SocketState)ar.AsyncState;

			var bytesRead = state.Socket.EndReceiveFrom(ar, ref LocalEndPoint);
			if (bytesRead == 0 || bytesRead == -1)
				return;

			var data = new byte[bytesRead];

			Array.Copy(state.Buffer, 0, data, 0, data.Length);

			SocketDataReceived?.Invoke(this, new SocketDataReceivedEventArgs(Id, (IPEndPoint)LocalEndPoint, data));

			state.Socket.BeginReceiveFrom(state.Buffer, 0, state.Buffer.Length,
				0, ref LocalEndPoint, new AsyncCallback(Received), state);
		}

		public void Start()
		{
			state.Socket.BeginReceiveFrom(state.Buffer, 0, state.Buffer.Length,
				0, ref LocalEndPoint, new AsyncCallback(Received), state);
		}

		public void Stop()
		{
			state.Socket.Shutdown(SocketShutdown.Both);
		}
	}
}
