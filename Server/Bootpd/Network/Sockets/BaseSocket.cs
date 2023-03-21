using Bootpd.Common;
using Bootpd.Network.Packet;
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
		public EndPoint RemoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
		public EndPoint LocalEndpoint = new IPEndPoint(IPAddress.Any, 0);

		public string Id { get; }

		public IPAddress IPAddress { get; private set; }

		public bool Bound { get; private set; }

		public int Buffersize { get; private set; } = 1500;

		SocketState state;
		public BaseSocket(System.Net.Sockets.SocketType socketType, ServerType serverType, IPEndPoint endPoint, ushort size = 1500)
		{
			Id = Guid.NewGuid().ToString();
			LocalEndpoint = endPoint;
			IPAddress = endPoint.Address;
			state = new SocketState(size, endPoint.AddressFamily, serverType, socketType);
			Buffersize = size;
		}

		public void Bootstrap()
		{
			try
			{
				state.Socket.Bind(LocalEndpoint);
				Bound = true;
			}
			catch (SocketException ex)
			{
				Console.WriteLine(ex);
			}
		}

		public void Dispose()
		{
			if (Bound)
				Stop();

			state.Socket.Dispose();
		}

		void Received(IAsyncResult ar)
		{
			state = (SocketState)ar.AsyncState;

			var bytesRead = state.Socket.EndReceiveFrom(ar, ref RemoteEndpoint);
			if (bytesRead == 0 || bytesRead == -1)
				return;

			var data = new byte[bytesRead];

			Array.Copy(state.Buffer, 0, data, 0, data.Length);
			BasePacket packet = null;

			switch (state.ServerType)
			{
				case ServerType.DHCP:
				case ServerType.BOOTP:
					packet = new DHCPPacket(data);
					break;
				case ServerType.TFTP:
					packet = new TFTPPacket(data);
					break;
			}

			SocketDataReceived?.Invoke(this,
				new SocketDataReceivedEventArgs(Id,
					(IPEndPoint)RemoteEndpoint, packet));

			state.Socket.BeginReceiveFrom(state.Buffer, 0, state.Buffer.Length,
				0, ref RemoteEndpoint, new AsyncCallback(Received), state);
		}

		public void Start()
		{
			if (!Bound)
				return;

			Buffersize = state.Socket.ReceiveBufferSize;

			Console.WriteLine("[D] Starting socket on: {0}", LocalEndpoint);

			state.Socket.BeginReceiveFrom(state.Buffer, 0, state.Buffer.Length,
				0, ref RemoteEndpoint, new AsyncCallback(Received), state);
		}

		public void Stop()
		{
			if (!Bound)
				return;

			state.Socket.Shutdown(SocketShutdown.Both);
			state.Socket.Close();
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
			IPAddress = ((IPEndPoint)LocalEndpoint).Address;
			Buffersize = state.Socket.ReceiveBufferSize;
		}
	}
}
