using System;

namespace Bootpd.Network.Sockets
{
	public interface ISocket : IDisposable
	{
		void Bootstrap();
		void Start();
		void Stop();
		void HeartBeat();
	}
}
