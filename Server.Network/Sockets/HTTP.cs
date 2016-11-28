using System;
using System.Net;
using Server.Extensions;

namespace Server.Network
{
	public sealed class HTTPSocket : IDisposable
	{
		HttpListener socket;

		HttpListenerContext context;
		public HTTPSocket(int port)
		{
			if (!Settings.EnableHTTP)
				return;

			try
			{
				this.socket = new HttpListener();
				var endpoint = "http://{0}:{1}/".F(Environment.MachineName, port);
				this.socket.Prefixes.Add(endpoint);

				this.socket.Start();

				if (this.socket.IsListening)
				{
					this.socket.BeginGetContext(new AsyncCallback(this.GetContext), null);
					Errorhandler.Report(LogTypes.Info, "Website is available at: {0}".F(endpoint));
				}
				else
					Errorhandler.Report(LogTypes.Error, "Socket is not in Listening state!");
			}
			catch (Exception ex)
			{
				Errorhandler.Report(LogTypes.Error, "[HTTP] {0}".F(ex.Message));
				Settings.Servermode = ServerMode.AllowAll;
			}
		}

		~HTTPSocket()
		{
			this.Dispose();
		}

		public event HTTPDataReceivedEventHandler HTTPDataReceived;

		public event HTTPDataSendEventHandler HTTPDataSend;

		public void Dispose()
		{
			this.Close();
		}

		internal void OnHTTPDataSend(HttpListenerContext context)
		{
			this.HTTPDataSend?.Invoke(this, new HTTPDataSendEventArgs());
		}

		internal void OnHTTPDataReceived(HttpListenerContext context)
		{
			var evtargs = new HTTPDataReceivedEventArgs();
			evtargs.Request = this.context.Request;

			this.HTTPDataReceived?.Invoke(this, evtargs);
		}

		internal void Send(byte[] buffer, int statuscode, string description)
		{
			this.context.Response.StatusCode = statuscode;
			this.context.Response.StatusDescription = description;

			using (var s = this.context.Response.OutputStream)
				s?.Write(buffer, 0, buffer.Length);

		}

		internal void Close()
		{
			this.socket?.Close();
		}

		private void GetContext(IAsyncResult ar)
		{
			this.context = this.socket.EndGetContext(ar);
			this.OnHTTPDataReceived(this.context);

			this.socket.BeginGetContext(new AsyncCallback(this.GetContext), null);
		}
	}
}
