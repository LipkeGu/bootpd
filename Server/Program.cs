namespace bootpd
{
	using System;
	using System.Net;
	using System.Text;
	using Server.Network;
	using Server.Extensions;

	public class Bootpd
	{
		public static SQLDatabase Database = new SQLDatabase();
		 
		[STAThread]
		static void Main()
		{
			var dhcp = new DHCP(new IPEndPoint(IPAddress.Any, Settings.DHCPPort), Settings.BINLPort, Settings.Servermode);
			var http = new HTTP(Settings.HTTPPort);
			var tftp = new TFTP(new IPEndPoint(Settings.ServerIP, Settings.TFTPPort));

			Console.WriteLine("AscII length: {0}", Exts.StringToByte(Settings.ServerDomain, Encoding.ASCII).Length);
			Console.WriteLine("Unicode length: {0}", Exts.StringToByte(Settings.ServerDomain, Encoding.Unicode).Length);
			
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
