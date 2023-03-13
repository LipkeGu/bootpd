using Bootpd;
using Server.Extensions;
using System;
using System.Net;
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
				socket = new HttpListener();
				var endpoint = "http://{0}:{1}/".F(Environment.MachineName, port);
				socket.Prefixes.Add(endpoint);

				socket.Start();

				if (socket.IsListening)
				{
					socket.BeginGetContext(new AsyncCallback(GetContext), null);
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
			Dispose();
		}

		public event HTTPDataReceivedEventHandler HTTPDataReceived;

		public event HTTPDataSendEventHandler HTTPDataSend;

		public void Dispose()
		{
			Close();
		}

		internal void OnHTTPDataSend(HttpListenerContext context) => HTTPDataSend?.Invoke(this, new HTTPDataSendEventArgs());

		internal void OnHTTPDataReceived(HttpListenerContext context)
		{

		}

		internal void Send(byte[] buffer, int statuscode, string description)
		{
			context.Response.StatusCode = statuscode;
			context.Response.StatusDescription = description;

			using (var s = context.Response.OutputStream)
				s?.Write(buffer, 0, buffer.Length);

		}

		internal void Close() => socket?.Close();

		private void GetContext(IAsyncResult ar)
		{
			this.context = this.socket.EndGetContext(ar);

			HTTPDataReceived?.Invoke(this, new HTTPDataReceivedEventArgs
			{
				Request = this.context.Request
			});

			socket.BeginGetContext(new AsyncCallback(GetContext), null);
		}
	}
}
