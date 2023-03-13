using Server.Network;
using System.Net;

namespace Bootpd.Network.Client
{
	public class TFTPClient : BaseClient
	{

		public TFTPClient(ServerType serverType, IPEndPoint endpoint, bool local)
			: base(serverType, endpoint, local)
		{

		}
	}
}
