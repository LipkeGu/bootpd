namespace Server.Extensions
{
	using Server.Network;
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

		public static Fileystem Filesystem(DriveInfo drive)
		{
			switch (drive.DriveFormat)
			{
				case "NTFS":
					return Fileystem.NTFS;
				case "FAT":
					return Fileystem.FAT;
				case "FAT32":
					return Fileystem.FAT32;
				default:
					return Fileystem.Unknown;
			}
		}
	}
}
