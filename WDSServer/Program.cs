using System;
using System.Net;

namespace WDSServer.Network
{
	public class Server : Definitions
	{
		[STAThread]
		static void Main()
		{
			if (!Settings.enableHTTP)
				Settings.Servermode = ServerMode.AllowAll;

			var DHCPServer = new DHCP(new IPEndPoint(Settings.ServerIP, Settings.DHCPPort), Settings.BINLPort, Settings.Servermode);
			var TFTPServer = new TFTP(new IPEndPoint(Settings.ServerIP, Settings.TFTPPort));
			var HTTPServer = new HTTP(Settings.HTTPPort);

			var t = string.Empty;
			while (t != "exit")
				t = Console.ReadLine();

			TFTPServer.Dispose();
			DHCPServer.Dispose();
			HTTPServer.Dispose();
		}
	}
}
