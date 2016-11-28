namespace Server.Network
{
	using System;
	using System.IO;
	using System.Net;
	using Extensions;

	public static class Settings
	{
		public static bool EnableDHCP = true;
		public static bool EnableTFTP = true;
		public static bool EnableHTTP = true;

		#region "HTTP Server"
		public static string Charset = "utf-8";
		public static string Design = "Default";
		#endregion

		#region "Windows Deployment Server"
		public static string OSC_DEFAULT_FILE = "welcome.osc";
		public static string OSC_DEFAULT_LANG = "English";
		public static string OSC_DEFAULT_TITLE = "Client Installation Wizard";

		public static string OSC_DEFAULT_USER = "Administrator";
		public static string OSC_DEFAULT_PASS = "secret";

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
		public static string DHCP_DEFAULT_BOOTFILE = "wdsnbp.com";
		public static string DHCP_MENU_PROMPT = "Select Server...";

		public static ushort PollInterval = 3;
		public static ushort RetryCount = ushort.MaxValue;
		public static int RequestID = 1;

		public static bool EnableNTLMV2 = true;
		public static bool AllowVariableWindowSize = true;
		#endregion

		#region "Server Settings"
		public static string ServerName = Environment.MachineName;
		public static string ServerDomain = "LOCALDOMAIN";
		public static string UserDNSDomain = "Localdomain.local";
		public static string TFTPRoot = Path.Combine(Filesystem.ReplaceSlashes(Environment.CurrentDirectory), "TFTPRoot");
		public static string DriverFile = Path.Combine(Filesystem.ReplaceSlashes(Environment.CurrentDirectory), "drivers.xml");

		public static ServerMode Servermode = ServerMode.KnownOnly;

		public static long MaxAllowedFileLength = 10485760;

		public static IPAddress ServerIP = Exts.GetServerIP();

		public static bool ReUseAddress = false;
		public static bool AdvertPXEServerList = false;
		public static bool FixPXELinuxConfigPath = true;

		public static ushort MaximumAllowedBlockSize = 4096;
		public static ushort MaximumAllowedWindowSize = 8;
		
		public static ushort SendBuffer = 27182;

		public static int DHCPPort = 67;
		public static int BINLPort = 4011;
		public static int TFTPPort = 69;
		public static int HTTPPort = 8080;
		public static int ReadBuffer = 2 << 64;
		public static int DiscoveryType = 7;
		#endregion
	}
}
