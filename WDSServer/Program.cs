namespace WDSServer
{
	using System;
	using System.Net;
	using WDSServer.Network;

	public class Server : Definitions
	{
		[STAThread]
		static void Main()
		{
			if (!Settings.EnableHTTP)
				Settings.Servermode = ServerMode.AllowAll;

			var dhcp = new DHCP(new IPEndPoint(IPAddress.Any, Settings.DHCPPort), Settings.BINLPort, Settings.Servermode);
			var tftp = new TFTP(new IPEndPoint(Settings.ServerIP, Settings.TFTPPort));
			var http = new HTTP(Settings.HTTPPort);

			var t = string.Empty;
			while (t != "exit")
				t = Console.ReadLine();

			tftp.Dispose();
			dhcp.Dispose();
			http.Dispose();
		}
	}
}
