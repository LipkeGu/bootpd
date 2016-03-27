namespace WDSServer.Network
{
	using System;
	using System.Net;
	using System.Net.Sockets;
	using WDSServer.Providers;

	public sealed class TFTPSocket : SocketProvider
	{
		public TFTPSocket(IPEndPoint endpoint, bool broadcast = false, int buffersize = 1024, SocketType type = SocketType.TFTP)
		{
			try
			{
				if (!Settings.EnableTFTP)
					return;

				this.localEndPoint = endpoint;
				this.broadcast = broadcast;
				this.buffersize = buffersize;
				this.type = type;

				this.sendBuffer = Settings.SendBuffer;
				this.reuseAddress = Settings.ReUseAddress;

				this.socket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, ProtocolType.Udp);
				this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, this.sendBuffer);
				this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, this.broadcast);
				this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, this.reuseAddress);
				this.socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 0x3C);

				this.state = new SocketState();
				this.state.Type = this.type;
				this.state.Buffer = new byte[this.buffersize];
				this.state.Buffersize = this.state.Buffer.Length;
				this.state.Socket = this.socket;

				this.socket.Bind(this.localEndPoint);
				this.socket.BeginReceiveFrom(this.state.Buffer, 0, this.state.Buffersize, 0,
					ref this.localEndPoint, new AsyncCallback(this.Received), this.state);
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
				return this.type;
			}

			set
			{
				this.type = value;
			}
		}

		public void Send(IPEndPoint target, TFTPPacket packet)
		{
			var bytessend = this.socket.SendTo(packet.Data,
				packet.Offset, SocketFlags.None, target);

			this.OnDataSend(bytessend, target, this.type);
		}

		public void Dispose()
		{
			if (this.socket != null)
				this.socket.Dispose();
		}

		internal override void Received(IAsyncResult ar)
		{
			this.state = (SocketState)ar.AsyncState;
			var client = this.state.Socket;

			var bytesRead = client.EndReceiveFrom(ar, ref this.localEndPoint);
			if (bytesRead == 0 || bytesRead == -1)
				return;

			var data = new byte[bytesRead];
			Functions.CopyTo(ref this.state.Buffer, 0, ref data, 0, data.Length);

			this.OnDataReceived(data, (IPEndPoint)this.localEndPoint, this.type);

			client.BeginReceiveFrom(this.state.Buffer, 0, this.state.Buffersize,
				0, ref this.localEndPoint, new AsyncCallback(this.Received), this.state);
		}
	}
}
