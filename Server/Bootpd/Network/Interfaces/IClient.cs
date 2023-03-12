using System;

namespace Bootpd.Network.Client
{
	public interface IClient : IDisposable
	{
		Guid Id { get; }

		void Bootstrap();
		void Start();
		void Stop();
		void HeartBeat();
	}
}
