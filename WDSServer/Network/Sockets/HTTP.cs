using System;
using System.Net;

namespace WDSServer.Network
{
	sealed public class HTTPSocket : IDisposable
	{
		HttpListener socket;
		HttpListenerContext context;

		public event HTTPDataReceivedEventHandler HTTPDataReceived;
		public event HTTPDataSendEventHandler HTTPDataSend;

		string hostname;
		int port;

		public HTTPSocket(string hostname, int port)
		{
			if (!Settings.enableHTTP)
				return;

			try
			{
				this.hostname = hostname;
				this.port = port;

				this.socket = new HttpListener();
				var endpoint = "http://{0}:{1}/".F(this.hostname, this.port);
				this.socket.Prefixes.Add(endpoint);

				this.socket.Start();

				if (this.socket.IsListening)
				{
					this.socket.BeginGetContext(new AsyncCallback(GetContext), null);
					Errorhandler.Report(Definitions.LogTypes.Info, "Website is available at: {0}".F(endpoint));
				}
				else
					Errorhandler.Report(Definitions.LogTypes.Error, "Socket is not in Listening state!");
			}
			catch (Exception ex)
			{
				Errorhandler.Report(Definitions.LogTypes.Error, "[HTTP] {0}".F(ex.Message));
				DHCP.Mode = Definitions.ServerMode.AllowAll;
			}
		}

		private void GetContext(IAsyncResult ar)
		{
			this.context = this.socket.EndGetContext(ar);
			OnHTTPDataReceived(this.context);

			this.socket.BeginGetContext(new AsyncCallback(GetContext), null);
		}

		internal void OnHTTPDataSend(HttpListenerContext context)
		{
			var evtargs = new HTTPDataSendEventArgs();

			if (HTTPDataSend != null)
				HTTPDataSend(this, evtargs);
		}

		internal void OnHTTPDataReceived(HttpListenerContext context)
		{
			var evtargs = new HTTPDataReceivedEventArgs();

			evtargs.Filename = this.context.Request.Url.LocalPath.ToLowerInvariant();
			evtargs.Arguments = this.context.Request.QueryString;
			evtargs.ContentType = this.context.Request.ContentType;
			evtargs.Headers = this.context.Request.Headers;

			if (HTTPDataReceived != null)
				HTTPDataReceived(this, evtargs);
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

						OnHTTPDataSend(this.context);
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


		public void Dispose()
		{
			Close();
		}

		public string Hostname => this.hostname;

		~HTTPSocket()
		{
			Close();
		}
	}
}
