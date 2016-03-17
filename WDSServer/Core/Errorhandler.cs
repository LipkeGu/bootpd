﻿using System;

namespace WDSServer
{
	public static class Errorhandler
	{
		public static void Report(Definitions.LogTypes level, string Message)
		{
			if (Message == string.Empty)
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

			Console.WriteLine("[{0}]: {1}", lvl, Message);
		}
	}
}