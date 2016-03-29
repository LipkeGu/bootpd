namespace bootpd
{
	using System;

	public static class Errorhandler
	{
		public static void Report(Definitions.LogTypes level, string message)
		{
			if (message == string.Empty)
				return;

			var lvl = string.Empty;

			switch (level)
			{
				case Definitions.LogTypes.Info:
					lvl = "I";
					break;
				case Definitions.LogTypes.Warning:
					lvl = "W";
					break;
				case Definitions.LogTypes.Error:
					lvl = "E";
					break;
				case Definitions.LogTypes.Other:
					lvl = "O";
					break;
				default:
					lvl = string.Empty;
					break;
			}

			var line = "[{0}]: {1}".F(lvl, message);
			Console.WriteLine(line);

			WriteLogLine("console_log.txt", line);
		}

		public static void WriteLogLine(string file, string message)
		{
			Files.Write(file, ref message);
		}
	}
}
