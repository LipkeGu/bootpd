namespace bootpd
{
	using System;
	using System.IO;

	public class Directories
	{
		public static bool Create(string path)
		{
			try
			{
				Directory.CreateDirectory(path);

				return true;
			}
			catch (Exception ex)
			{
				Errorhandler.Report(Definitions.LogTypes.Error, ex.Message);

				return false;
			}
		}

		public static bool Delete(string path)
		{
			try
			{
				Directory.Delete(path, true);

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
