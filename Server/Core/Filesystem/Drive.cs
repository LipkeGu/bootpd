namespace bootpd
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	public static class Drives
	{
		public static List<DriveInfo> GetDrivesByType(DriveType type = DriveType.Fixed) => (from d in DriveInfo.GetDrives() where d.DriveType == DriveType.Fixed select d).ToList();

		public static long TotalSize(DriveInfo drive)
		{
			return drive.TotalSize;
		}

		public static long TotalFreeSpace(DriveInfo drive)
		{
			return drive.TotalFreeSpace;
		}

		public static long AvailableFreeSpace(DriveInfo drive)
		{
			return drive.AvailableFreeSpace;
		}

		public static Definitions.Fileystem Filesystem(DriveInfo drive)
		{
			switch (drive.DriveFormat)
			{
				case "NTFS":
					return Definitions.Fileystem.NTFS;
				case "FAT":
					return Definitions.Fileystem.FAT;
				case "FAT32":
					return Definitions.Fileystem.FAT32;
				default:
					return Definitions.Fileystem.Unknown;
			}
		}
	}
}
