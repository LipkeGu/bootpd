using bootpd.Bootpd.Network;
using System;
using System.Net;
using System.Net.Sockets;

namespace Bootpd.Network.Sockets
{
	public class SocketState
	{
		public byte[] Buffer;
		public Socket Socket;


		internal SocketState(int buffersize, AddressFamily addressFamily, SocketType socketType)
		{
			var proto = ProtocolType.Unknown;
			var sockType = SocketType.Raw;


			switch (socketType)
			{
				case SocketType.Stream:
					proto = ProtocolType.Tcp;
					sockType = SocketType.Stream;
					break;
				case SocketType.Dgram:

					proto = ProtocolType.Udp;
					sockType = SocketType.Dgram;
					break;
				default:
				case SocketType.Raw:
					proto = ProtocolType.Raw;
					sockType = SocketType.Unknown;
					break;
			}

			Socket = new Socket(addressFamily, sockType, proto);
			Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
			Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

			Buffer = new byte[buffersize];
		}
	}

	public partial class BaseSocket : ISocket
	{
		public EndPoint LocalEndPoint;
		public Guid Id { get; }

		public bool Bound { get; private set; }

		SocketState state;
		public BaseSocket(SocketType socketType, Guid id, IPEndPoint endPoint, ushort size = 1500)
		{
			Id = id;
			LocalEndPoint = endPoint;
			state = new SocketState(size, endPoint.AddressFamily, socketType);
		}

		public void Bootstrap()
		{
			state.Socket.Bind(LocalEndPoint);
			Bound = true;
		}

		public void Dispose()
		{
			if (Bound)
				Stop();

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
			if (Bound)
				return;

			state.Socket.BeginReceiveFrom(state.Buffer, 0, state.Buffer.Length,
				0, ref LocalEndPoint, new AsyncCallback(Received), state);
		}

		public void Stop()
		{
			if (!Bound)
				return;

			state.Socket.Shutdown(SocketShutdown.Both);
		}

		public void Send(IPEndPoint target, IPacket packet)
		{
			Send(target, packet);
		}


		public int Send(IPEndPoint target, IPacket packet, int length = 0)
		{
			return state.Socket.SendTo(packet.Buffer.GetBuffer(),
				packet.Buffer.GetBuffer().Length, SocketFlags.None, target);
		}

		public void HeartBeat()
		{
		}
	}
}
