namespace WDSServer
{
	using System;
	using System.IO;
	using System.Net;
	using System.Xml.Serialization;

	public static class Settings
	{
		public static bool EnableDHCP = true;
		public static bool EnableTFTP = true;
		public static bool EnableHTTP = true;

		public static bool AdvertPXEServerList = true;

		public static bool ReUseAddress = false;

		public static int DHCPPort = 67;
		public static int BINLPort = 4011;
		public static int TFTPPort = 69;
		public static int HTTPPort = 8080;
		public static int SendBuffer = 30000;

		public static int PollInterval = 4;
		public static int RetryCount = 30;

		public static int RequestID = 1;

		[XmlIgnore]
		public static IPAddress ServerIP = Exts.GetServerIP();
		public static Definitions.ServerMode Servermode = Definitions.ServerMode.KnownOnly;

		public static string TFTPRoot = Path.Combine(Filesystem.ReplaceSlashes(Environment.CurrentDirectory), "TFTPRoot");
		public static string DriverFile = Path.Combine(Filesystem.ReplaceSlashes(Environment.CurrentDirectory), "drivers.xml");
		public static string OSC_DEFAULT_FILE = "welcome.osc";
		public static string WDS_BCD_FileName = "default.bcd";
		public static string WDS_BOOT_PREFIX_X86 = "Boot/x86/";
		public static string WDS_BOOT_PREFIX_X64 = "Boot/x64/";
		public static string WDS_BOOT_PREFIX_EFI = "Boot/EFI/";
		public static string WDS_BOOT_PREFIX_IA64 = "Boot/ia64/";

		public static string WDS_BOOTFILE_X86 = "pxeboot.n12";
		public static string WDS_BOOTFILE_X64 = "pxeboot.n12";
		public static string WDS_BOOTFILE_IA64 = "Bootmgfw.efi";
		public static string WDS_BOOTFILE_EFI = "Bootmgfw.efi";

		public static string WDS_BOOTFILE_ABORT = "abortpxe.com";

		public static string DHCP_DEFAULT_BOOTFILE = "wdsnbp.0";

		[XmlIgnore]
		public static string ServerName = Environment.MachineName;
		public static string ServerDomain = "LOCALDOMAIN";
		public static string UserDNSDomain = "Localdomain.local";
		[XmlIgnore]
		public static string Charset = "utf-8";
		public static string Design = "Default";

		public static long MaxAllowedFileLength = 10485760;
	}
}
