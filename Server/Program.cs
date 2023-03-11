﻿namespace server
{
	using Bootpd;
	using Server.Network;
	using System;
	using System.Net;

	public class Program
	{
		[STAThread]
		static void Main()
		{
			var instance = new BootpdCommon(Environment.GetCommandLineArgs());

			var dhcp = new DHCP(new IPEndPoint(IPAddress.Any, Settings.DHCPPort), Settings.BINLPort, Settings.Servermode);
			var http = new HTTP(Settings.HTTPPort);
			var tftp = new TFTP(new IPEndPoint(Settings.ServerIP, Settings.TFTPPort));





			var t = string.Empty;
			while (t != "exit")
				t = Console.ReadLine();

			tftp.Dispose();
			dhcp.Dispose();
			http.Dispose();
		}
	}
}
