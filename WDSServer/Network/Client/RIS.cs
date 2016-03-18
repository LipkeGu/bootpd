namespace WDSServer.Network
{
	using System.Net;

	public class RISClient
	{
		IPEndPoint endpoint;

		Definitions.RISOPCodes opcode;

		public RISClient(IPEndPoint endpoint)
		{
			this.endpoint = endpoint;
		}

		public IPEndPoint Endpoint
		{
			get
			{
				return this.endpoint;
			}

			set
			{
				this.endpoint = value;
			}
		}

		public Definitions.RISOPCodes OPCode
		{
			get
			{
				return this.opcode;
			}

			set
			{
				this.opcode = value;
			}
		}
	}
}
