namespace bootpd
{
	using System.Net;
	public class Serverentry
	{
		Definitions.BootServerTypes type;
		string hostname;
		short ident;
		string bootfile;
		IPAddress address;

		public Serverentry(short ident, string hostname, string bootfile, IPAddress address,
			Definitions.BootServerTypes type = Definitions.BootServerTypes.PXEBootstrapServer)
		{
			this.ident = ident;
			this.bootfile = bootfile;
			this.hostname = hostname;
			this.address = address;
			this.type = type;
		}

		public string Hostname
		{
			get { return this.hostname; }
		}

		public string Bootfile
		{
			get { return this.bootfile; }
		}

		public IPAddress IPAddress
		{
			get { return this.address; }
		}

		public Definitions.BootServerTypes Type
		{
			get { return this.type; }
		}

		public short Ident
		{
			get { return this.ident; }
		}
	}
}
