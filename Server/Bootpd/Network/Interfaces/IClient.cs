using System;

namespace Bootpd.Network.Client
{
	public interface IClient : IDisposable
	{
		void Bootstrap();
		void Start();
		void Stop();
	}
}
