using System;

namespace Bootpd.Network.Client
{
	public interface IClient : IDisposable
	{
		string Id { get; }

		void Bootstrap();
		void Start();
		void Stop();
		void HeartBeat();
	}
}
