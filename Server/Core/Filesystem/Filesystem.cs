namespace bootpd
{
	using System;
	using System.IO;

	public static class Filesystem
	{
		public static long Size(string filename)
		{
			var size = 0L;
			using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
				size = fs.Length;

			return size;
		}

		public static bool Exist(string filename) => File.Exists(filename.ToLowerInvariant());

		public static string ResolvePath(string path)
		{
			var givenPath = path.ToLowerInvariant();
			var dir = Settings.TFTPRoot;

			givenPath = ReplaceSlashes(StripRoot(givenPath, dir));

			if (givenPath.EndsWith(".html") || givenPath.EndsWith(".htm") ||
			givenPath.EndsWith(".css") || givenPath.EndsWith(".js") || givenPath.EndsWith(".xml"))
				return Path.Combine(Environment.CurrentDirectory, givenPath);

			if (givenPath.StartsWith(Path.DirectorySeparatorChar.ToString()))
				givenPath = givenPath.Remove(0, 1);

			if (givenPath.Contains("pxelinux.cfg") && Settings.FixPXELinuxConfigPath)
			{
				givenPath = givenPath.Replace(Settings.WDS_BOOT_PREFIX_X86, string.Empty);
				givenPath = givenPath.Replace(Settings.WDS_BOOT_PREFIX_X64, string.Empty);
				givenPath = givenPath.Replace(Settings.WDS_BOOT_PREFIX_EFI, string.Empty);
			}

			return Path.Combine(Settings.TFTPRoot, givenPath);
		}

		public static string StripRoot(string path, string directory) => path.Replace(directory, string.Empty);

		public static string ReplaceSlashes(string input)
		{
			var slash = "/";
			var result = string.Empty;

			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.MacOSX:
				case PlatformID.Unix:
					slash = "/";
					break;
				case PlatformID.Win32NT:
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				case PlatformID.WinCE:
				case PlatformID.Xbox:
					slash = "\\";
					break;
			}

			result = input.Replace("/", slash);
			result = result.Replace("\\", slash);

			return result;
		}
	}
}
