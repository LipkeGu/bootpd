namespace server
{
	using Bootpd;
	using System;
	using System.Threading;

	public class Program
	{
		static BootpdCommon instance;

		[STAThread]
		static void Main()
		{
			instance = new BootpdCommon(Environment.GetCommandLineArgs());
			instance.Bootstrap();
			instance.Start();
			var heartbeatThread = new Thread(HeartBeat);
			heartbeatThread.Start();
			Console.WriteLine("[D] HeartBeat every 60 Seconds !");

			var t = string.Empty;
			while (t != "exit")
				t = Console.ReadLine();

			heartbeatThread.Abort();
			instance.Stop();
			instance.Dispose();

		}

		static void HeartBeat()
		{
			while (true)
			{
				Thread.Sleep(6000);
				instance.Heartbeat();
			}
		}
	}
}
