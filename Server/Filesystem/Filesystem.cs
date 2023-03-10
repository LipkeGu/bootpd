namespace Server.Extensions
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

		public static string ResolvePath(string path, string directory = "")
		{
			var givenPath =
				ReplaceSlashes(!string.IsNullOrEmpty(directory) ? StripRoot(path.ToLowerInvariant(), directory) : path.ToLowerInvariant());

			if (givenPath.EndsWith(".html") || givenPath.EndsWith(".htm") ||
			givenPath.EndsWith(".css") || givenPath.EndsWith(".js") || givenPath.EndsWith(".xml"))
				return Path.Combine(Environment.CurrentDirectory, givenPath);

			if (givenPath.StartsWith(Path.DirectorySeparatorChar.ToString()))
				givenPath = givenPath.Remove(0, 1);

			return string.IsNullOrEmpty(directory) ? Path.Combine(givenPath) : Path.Combine(directory, givenPath);
		}

		static string StripRoot(string path, string directory)
			=> path.Replace(directory, string.Empty);

		public static string ReplaceSlashes(string input)
		{
			var slash = "/";

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

			return input.Replace("/", slash);
		}
	}
}
