namespace bootpd
{
	using System;
	using System.Net;

	public abstract class Computer
	{
		protected Guid guid;
		protected Definitions.SystemType type;
		protected Definitions.Architecture arch;
		protected IPAddress ipaddress;

		protected string manufacturer;
		protected string model;
		protected string hostname;

		public abstract Guid UUID
		{
			get; set;
		}

		public abstract string Manufacturer
		{
			get; set;
		}

		public abstract string Hostname
		{
			get; set;
		}

		public abstract string Model
		{
			get; set;
		}

		public abstract Definitions.SystemType Type
		{
			get; set;
		}

		public abstract Definitions.Architecture Architecture
		{
			get; set;
		}

		public abstract IPAddress IPAddress
		{
			get; set;
		}
	}
}
