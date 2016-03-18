namespace WDSServer.Core.Providers
{
	using System;
	using System.Net;
	using static WDSServer.Definitions;

	public abstract class Computer
	{
		protected Guid guid;
		protected SystemType type;
		protected Architecture arch;
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

		public abstract SystemType Type
		{
			get; set;
		}

		public abstract Architecture Architecture
		{
			get; set;
		}

		public abstract IPAddress IPAddress
		{
			get; set;
		}
	}
}
