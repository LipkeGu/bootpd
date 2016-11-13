namespace bootpd
{
	using System;
	using System.Net;

	public class Server : Definitions
	{
		public static SQLDatabase Database = new SQLDatabase();
		 
		[STAThread]
		static void Main()
		{
			if (!Settings.EnableHTTP)
				Settings.Servermode = ServerMode.AllowAll;

			var dhcp = new DHCP(new IPEndPoint(IPAddress.Any, Settings.DHCPPort), Settings.BINLPort, Settings.Servermode);
			var http = new HTTP(Settings.HTTPPort);
			var tftp = new TFTP(new IPEndPoint(Settings.ServerIP, Settings.TFTPPort));

			var t = string.Empty;
			while (t != "exit")
				t = Console.ReadLine();

			tftp.Dispose();
			dhcp.Dispose();
			http.Dispose();
			Database.Close();
		}
	}
}
