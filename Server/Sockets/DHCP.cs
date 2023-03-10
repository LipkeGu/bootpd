﻿namespace Server.Network
{
	using Extensions;
	using System;
	using System.Net;
	using System.Net.Sockets;
	using static Functions;

	public sealed class DHCPSocket : SocketProvider
	{
		public DHCPSocket(IPEndPoint endpoint, bool broadcast = false, int buffersize = 1024, SocketType type = SocketType.DHCP)
		{
			try
			{
				localEndPoint = endpoint;
				this.broadcast = broadcast;
				this.buffersize = buffersize;
				this.type = type;
				reuseAddress = Settings.ReUseAddress;

				socket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, ProtocolType.Udp);
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, this.broadcast);
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, reuseAddress);

				state = new SocketState
				{
					Type = this.type,
					Buffer = new byte[this.buffersize],
					Buffersize = this.buffersize,
					Socket = socket
				};

				socket.Bind(localEndPoint);

				socket.BeginReceiveFrom(state.Buffer, 0, state.Buffersize, 0,
					ref localEndPoint, new AsyncCallback(Received), state);
			}
			catch (SocketException ex)
			{
				Errorhandler.Report(LogTypes.Error, "[DHCP] Socket Error {0}: {1}".F(ex.ErrorCode, ex.Message));
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

		public void Send(IPEndPoint target, byte[] packet, int length)
		{
			if (broadcast && type == SocketType.DHCP && target.Address.ToString() == "0.0.0.0")
				target.Address = IPAddress.Broadcast;

			var bytessend = socket.SendTo(packet, length, SocketFlags.None, target);
			if (bytessend < 1)
				Errorhandler.Report(LogTypes.Error, "[DHCP] Send(): Error!");
			else
				OnDataSend(bytessend, target, type);
		}

		internal override void Received(IAsyncResult ar)
		{
			if (socket == null)
				return;

			state = (SocketState)ar.AsyncState;
			var client = state.Socket;

			var bytesRead = client.EndReceiveFrom(ar, ref localEndPoint);
			if (bytesRead == 0 || bytesRead == -1)
				return;

			var data = new byte[bytesRead];

			CopyTo(state.Buffer, 0, data, 0, data.Length);

			OnDataReceived(data, (IPEndPoint)localEndPoint, type);

			socket.BeginReceiveFrom(state.Buffer, 0, state.Buffersize,
				0, ref localEndPoint, new AsyncCallback(Received), state);
		}
	}
}