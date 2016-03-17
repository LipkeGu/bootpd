using System;
using System.Net;

namespace WDSServer
{
	public class Settings
	{
		public static string TFTPRoot = Filesystem.ReplaceSlashes(Environment.CurrentDirectory);

		public static bool enableDHCP = true;
		public static bool enableTFTP = true;
		public static bool enableHTTP = true;
		public static bool ReUseAddress = false;

		public static int DHCPPort = 67;
		public static int BINLPort = 4011;
		public static int TFTPPort = 69;
		public static int HTTPPort = 8080;
		public static int SendBuffer = 30000;

		public static int PollInterval = 4;
		public static int RetryCount = 30;
		public static int RequestID = 1;

		public static IPAddress ServerIP = Exts.GetServerIP();
		public static Definitions.ServerMode Servermode = Definitions.ServerMode.KnownOnly;

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

		public static string DHCP_DEFAULT_BOOTFILE = "wdsnbp.0";

		public static string ServerName = Environment.MachineName;
		public static string ServerDomain = Environment.UserDomainName == Environment.MachineName
			? "LOCALDOMAIN" : Environment.UserDomainName;

		public static string UserDNSDomain = "{0}.local".F(ServerDomain);

		public static string Charset = "utf-8";


		public static long MaxAllowedFileLength = 10485760;

	}
}
