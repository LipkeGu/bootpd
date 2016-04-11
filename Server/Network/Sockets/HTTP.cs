namespace bootpd
{
	using System;
	using System.Net;

	public sealed class HTTPSocket : IDisposable
	{
		HttpListener socket;

		HttpListenerContext context;

		int port;

		public HTTPSocket(int port)
		{
			if (!Settings.EnableHTTP)
				return;

			try
			{
				this.port = port;

				this.socket = new HttpListener();
				var endpoint = "http://{0}:{1}/".F(Environment.MachineName, this.port);
				this.socket.Prefixes.Add(endpoint);

				this.socket.Start();

				if (this.socket.IsListening)
				{
					this.socket.BeginGetContext(new AsyncCallback(this.GetContext), null);
					Errorhandler.Report(Definitions.LogTypes.Info, "Website is available at: {0}".F(endpoint));
				}
				else
					Errorhandler.Report(Definitions.LogTypes.Error, "Socket is not in Listening state!");
			}
			catch (Exception ex)
			{
				Errorhandler.Report(Definitions.LogTypes.Error, "[HTTP] {0}".F(ex.Message));
				Settings.Servermode = Definitions.ServerMode.AllowAll;
			}
		}

		~HTTPSocket()
		{
			this.Close();
		}

		public event HTTPDataReceivedEventHandler HTTPDataReceived;

		public event HTTPDataSendEventHandler HTTPDataSend;

		public void Dispose()
		{
			this.Close();
		}

		internal void OnHTTPDataSend(HttpListenerContext context)
		{
			var evtargs = new HTTPDataSendEventArgs();

			if (this.HTTPDataSend != null)
				this.HTTPDataSend(this, evtargs);
		}

		internal void OnHTTPDataReceived(HttpListenerContext context)
		{
			var evtargs = new HTTPDataReceivedEventArgs();
			evtargs.Request = this.context.Request;

			if (this.HTTPDataReceived != null)
				this.HTTPDataReceived(this, evtargs);
		}

		internal void Send(byte[] buffer, int statuscode, string description)
		{
			this.context.Response.StatusCode = statuscode;
			this.context.Response.StatusDescription = description;

			using (var s = this.context.Response.OutputStream)
				try
				{
					if (s.CanWrite)
					{
						s.Write(buffer, 0, buffer.Length);

						this.OnHTTPDataSend(this.context);
					}
				}
				catch (Exception)
				{
				}
		}

		internal void Close()
		{
			if (this.socket != null)
				this.socket.Close();
		}

		private void GetContext(IAsyncResult ar)
		{
			this.context = this.socket.EndGetContext(ar);
			this.OnHTTPDataReceived(this.context);

			this.socket.BeginGetContext(new AsyncCallback(this.GetContext), null);
		}
	}
}
