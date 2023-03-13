namespace Server.Network
{
	using Bootpd;
	using Extensions;
	using System;
	using System.Net;
	using System.Net.Sockets;
	using static Bootpd.Functions;
	public sealed class TFTPSocket : SocketProvider
	{
		public TFTPSocket(IPEndPoint endpoint, bool broadcast = false, int buffersize = 1024, SocketType type = SocketType.TFTP)
		{
			try
			{
				if (!Settings.EnableTFTP)
					return;

				localEndPoint = endpoint;
				this.broadcast = broadcast;
				this.buffersize = buffersize;
				this.type = type;

				sendBuffer = Settings.SendBuffer;
				reuseAddress = Settings.ReUseAddress;

				socket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, ProtocolType.Udp);
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, sendBuffer);
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, this.broadcast);
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, reuseAddress);
				socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 0x3C);

				state = new SocketState
				{
					Type = this.type,
					Buffer = new byte[this.buffersize]
				};
				state.Buffersize = state.Buffer.Length;
				state.Socket = socket;

				socket.Bind(localEndPoint);
				socket.BeginReceiveFrom(state.Buffer, 0, state.Buffersize, 0,
					ref localEndPoint, new AsyncCallback(Received), state);
			}
			catch (SocketException ex)
			{
				Errorhandler.Report(LogTypes.Error, "[TFTP] Socket Error {0}: {1}".F(ex.ErrorCode, ex.Message));
			}
		}

		public override SocketType Type
		{
			get
			{
				return type;
			}

			set
			{
				type = value;
			}
		}

		public void Send(IPEndPoint target, TFTPPacket packet)
		{
			var bytessend = socket.SendTo(packet.Data, packet.Offset, SocketFlags.None, target);
			OnDataSend(bytessend, target, type);
		}

		public void Dispose() => socket?.Dispose();

		internal override void Received(IAsyncResult ar)
		{
			state = (SocketState)ar.AsyncState;
			var client = state.Socket;

			var bytesRead = client.EndReceiveFrom(ar, ref localEndPoint);
			if (bytesRead == 0 || bytesRead == -1)
				return;

			var data = new byte[bytesRead];
			CopyTo(state.Buffer, 0, data, 0, data.Length);

			OnDataReceived(data, (IPEndPoint)localEndPoint, type);

			client.BeginReceiveFrom(state.Buffer, 0, state.Buffersize,
				0, ref localEndPoint, new AsyncCallback(Received), state);
		}
	}
}
