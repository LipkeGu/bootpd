namespace bootpd
{
	using System;
	using System.Net;
	using System.Net.Sockets;

	public sealed class BINLSocket : SocketProvider
	{
		public BINLSocket(IPEndPoint endpoint, bool broadcast = false, int buffersize = 1024, SocketType type = SocketType.BINL)
		{
			try
			{
				this.localEndPoint = endpoint;
				this.broadcast = broadcast;
				this.buffersize = buffersize;
				this.type = type;
				this.reuseAddress = Settings.ReUseAddress;

				this.socket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, ProtocolType.Udp);
				this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, this.broadcast);
				this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, this.reuseAddress);

				this.state = new SocketState();
				this.state.Type = this.type;
				this.state.Buffer = new byte[this.buffersize];
				this.state.Buffersize = this.buffersize;
				this.state.Socket = this.socket;

				this.socket.Bind(this.localEndPoint);
				this.socket.BeginReceiveFrom(this.state.Buffer, 0, this.state.Buffersize, 0,
					ref this.localEndPoint, new AsyncCallback(this.Received), this.state);
			}
			catch (SocketException ex)
			{
				Errorhandler.Report(LogTypes.Error, "[BINL] Socket Error {0}: {1}".F(ex.ErrorCode, ex.Message));
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

		public void Send(IPEndPoint target, byte[] packet, int length)
		{
			if (this.broadcast && this.type == SocketType.DHCP && target.Address.ToString() == "0.0.0.0")
				target.Address = IPAddress.Broadcast;

			var bytessend = this.socket.SendTo(packet, length, SocketFlags.None, target);
			if (bytessend < 1)
				Errorhandler.Report(LogTypes.Error, "[BINL] Send(): Error!");
			else
				this.OnDataSend(bytessend, target, this.type);
		}

		internal override void Received(IAsyncResult ar)
		{
			if (this.socket == null)
				return;

			this.state = (SocketState)ar.AsyncState;
			var client = this.state.Socket;
			var length = 0;

			var bytesRead = client.EndReceiveFrom(ar, ref this.localEndPoint);
			if (bytesRead == 0 || bytesRead == -1)
				return;

			if (this.state.Buffer[0] == byte.MinValue)
			{
				length = Functions.FindEndOption(ref this.state.Buffer);
				if (length == 0)
					return;
			}
			else
				length = bytesRead;

			var data = new byte[length];

			Functions.CopyTo(ref this.state.Buffer, 0, ref data, 0, data.Length);

			this.OnDataReceived(data, (IPEndPoint)this.localEndPoint, this.type);

			this.socket.BeginReceiveFrom(this.state.Buffer, 0, this.state.Buffersize,
				0, ref this.localEndPoint, new AsyncCallback(this.Received), this.state);
		}
	}
}
