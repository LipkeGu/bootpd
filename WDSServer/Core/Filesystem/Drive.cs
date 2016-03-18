namespace WDSServer
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
			try
			{
				return drive.TotalSize;
			}
			catch (Exception ex)
			{
				Errorhandler.Report(Definitions.LogTypes.Error, ex.Message);

				return 0;
			}
		}

		public static long TotalFreeSpace(DriveInfo drive)
		{
			try
			{
				return drive.TotalFreeSpace;
			}
			catch (Exception ex)
			{
				Errorhandler.Report(Definitions.LogTypes.Error, ex.Message);

				return 0;
			}
		}

		public static long AvailableFreeSpace(DriveInfo drive)
		{
			try
			{
				return drive.AvailableFreeSpace;
			}
			catch (Exception ex)
			{
				Errorhandler.Report(Definitions.LogTypes.Error, ex.Message);

				return 0;
			}
		}

		public static Definitions.Fileystem Filesystem(DriveInfo drive)
		{
			try
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
			catch (Exception ex)
			{
				Errorhandler.Report(Definitions.LogTypes.Error, ex.Message);

				return Definitions.Fileystem.Unknown;
			}
		}
	}
}
