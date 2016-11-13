namespace bootpd
{
	public abstract class User
	{
		protected string username;
		protected string email;
		protected string fullname;
		protected string password;
		protected string location;

		protected Definitions.UserGroup userGroup;

		protected bool active;
		protected bool autoLogon;

		public abstract string UserName
		{
			get; set;
		}

		public abstract string Password
		{
			get; set;
		}

		public abstract string FullName
		{
			get; set;
		}

		public abstract string EMail
		{
			get; set;
		}

		public abstract Definitions.UserGroup UserGroup
		{
			get; set;
		}

		public abstract bool Active
		{
			get; set;
		}

		public abstract bool Autologin
		{
			get; set;
		}

		public abstract string Location
		{
			get; set;
		}
	}
}
