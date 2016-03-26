namespace WDSServer
{
	using System;
	using System.Net;
	using System.Threading;

	using WDSServer.Network;

	public class Server : Definitions
	{
		[STAThread]
		static void Main()
		{
			if (!Settings.EnableHTTP)
				Settings.Servermode = ServerMode.AllowAll;

			var dhcp = new DHCP(new IPEndPoint(IPAddress.Any, Settings.DHCPPort), Settings.BINLPort, Settings.Servermode);

			var http = new HTTP(Settings.HTTPPort);

			var tftp_thread = new Thread(tftpthread);
			tftp_thread.Start();


			var t = string.Empty;
			while (t != "exit")
				t = Console.ReadLine();

			dhcp.Dispose();
			http.Dispose();
		}

		private static void tftpthread()
		{
			var tftp = new TFTP(new IPEndPoint(Settings.ServerIP, Settings.TFTPPort));
		}
	}
}
