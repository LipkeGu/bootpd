using Bootpd.Network.Packet;
using Server.Network;
using System;
using System.Net;
using System.Net.Sockets;

namespace Bootpd.Network.Sockets
{
	public class SocketState
	{
		public byte[] Buffer;
		public Socket Socket;
		public ServerType ServerType;

		internal SocketState(int buffersize, AddressFamily addressFamily, ServerType serverType, System.Net.Sockets.SocketType socketType)
		{
			var proto = ProtocolType.Unknown;
			var sockType = System.Net.Sockets.SocketType.Raw;
			ServerType = serverType;

			switch (socketType)
			{
				case System.Net.Sockets.SocketType.Stream:
					proto = ProtocolType.Tcp;
					sockType = System.Net.Sockets.SocketType.Stream;
					break;
				case System.Net.Sockets.SocketType.Dgram:

					proto = ProtocolType.Udp;
					sockType = System.Net.Sockets.SocketType.Dgram;
					break;
				default:
				case System.Net.Sockets.SocketType.Raw:
					proto = ProtocolType.Raw;
					sockType = System.Net.Sockets.SocketType.Unknown;
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
		public EndPoint RemoteEndpoint;

		public Guid Id { get; }

		public bool Bound { get; private set; }
		public IPAddress ServerIP { get; }

		public int Buffersize { get; private set; } = 1500;

		SocketState state;
		public BaseSocket(System.Net.Sockets.SocketType socketType, ServerType serverType, IPEndPoint endPoint, ushort size = 1500)
		{
			Id = Guid.NewGuid();
			ServerIP = endPoint.Address;
			LocalEndPoint = endPoint;
			state = new SocketState(size, endPoint.AddressFamily, serverType, socketType);
			Buffersize = size;
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
			BasePacket packet = null;

			switch (state.ServerType)
			{
				case ServerType.DHCP:
				case ServerType.BOOTP:
					packet = new Packet.DHCPPacket(data);
					break;
				case ServerType.TFTP:
					packet = new Packet.TFTPPacket(data);
					break;
			}

			SocketDataReceived?.Invoke(this, new SocketDataReceivedEventArgs(Id, (IPEndPoint)LocalEndPoint, packet));

			state.Socket.BeginReceiveFrom(state.Buffer, 0, state.Buffer.Length,
				0, ref LocalEndPoint, new AsyncCallback(Received), state);
		}

		public void Start()
		{
			if (!Bound)
				return;
			Buffersize = state.Socket.ReceiveBufferSize;

			state.Socket.BeginReceiveFrom(state.Buffer, 0, state.Buffer.Length,
				0, ref LocalEndPoint, new AsyncCallback(Received), state);
		}

		public void Stop()
		{
			if (!Bound)
				return;

			state.Socket.Shutdown(SocketShutdown.Both);
		}

		public int Send(IPEndPoint target, IPacket packet)
		{
			if (!Bound)
				return -1;

			return state.Socket.SendTo(packet.Buffer.GetBuffer(),
				packet.Buffer.GetBuffer().Length, SocketFlags.None, target);
		}

		public void HeartBeat()
		{
			Buffersize = state.Socket.ReceiveBufferSize;
		}
	}
}
