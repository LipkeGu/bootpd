namespace WDSServer
{
	using System;
	using System.Text;

	public class DistributionShare
	{
		public static bool CreateDS(string name, string description, string path) => true;

		public static bool Test()
		{
			try
			{
				foreach (var drive in Drives.GetDrivesByType())
					if (drive.Name == "C:")
						Files.Write("{0}\\deploy.tag".F(drive.Name), Encoding.ASCII.GetBytes(string.Empty));

				return true;
			}
			catch (Exception ex)
			{
				Errorhandler.Report(Definitions.LogTypes.Error, ex.Message);

				return false;
			}
		}
	}
}
