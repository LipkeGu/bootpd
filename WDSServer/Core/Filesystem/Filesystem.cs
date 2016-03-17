﻿using System;
using System.IO;

namespace WDSServer
{
	public static class Filesystem
	{
		public static long Size(string filename)
		{
			var size = 0L;
			using (var fs = new FileStream(filename, FileMode.Open))
				size = fs.Length;

			return size;
		}

		public static bool Exist(string filename) => File.Exists(filename);

		public static string ResolvePath(string path)
		{
			var givenPath = path;
			givenPath = ReplaceSlashes(StripRoot(givenPath));

			if (givenPath.StartsWith(Path.DirectorySeparatorChar.ToString()))
				givenPath = givenPath.Remove(0, 1);

			var newPath = Path.Combine(Settings.TFTPRoot, givenPath);

			if (Exist(newPath))
				return newPath;
			else
				return path;
		}

		public static string StripRoot(string path) => path.Replace(ReplaceSlashes(Settings.TFTPRoot), string.Empty);

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