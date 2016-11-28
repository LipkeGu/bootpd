namespace Server.Network
{
	using System.Net;
	public class Serverentry<T>
	{
		protected BootServerTypes type;
		protected string hostname;
		protected string bootfile;

		protected T ident;
		protected IPAddress address;

		public Serverentry(T ident, string hostname, string bootfile, IPAddress address, BootServerTypes type = BootServerTypes.PXEBootstrapServer)
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

		public BootServerTypes Type
		{
			get { return this.type; }
		}

		public T Ident
		{
			get { return this.ident; }
		}
	}
}
