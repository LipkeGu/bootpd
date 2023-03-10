namespace Server.Extensions
{
	using Server.Network;
	using System;

	public static class Errorhandler
	{
		public static void Report(LogTypes level, string message)
		{
			if (string.IsNullOrEmpty(message))
				return;

			var lvl = string.Empty;

			switch (level)
			{
				case LogTypes.Info:
					lvl = "I";
					break;
				case LogTypes.Warning:
					lvl = "W";
					break;
				case LogTypes.Error:
					lvl = "E";
					break;
				case LogTypes.Other:
					lvl = "O";
					break;
				default:
					lvl = string.Empty;
					break;
			}

			var line = string.Format("[{0}]: {1}", lvl, message);
			Console.WriteLine(line);
		}
	}
}
