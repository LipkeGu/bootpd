namespace bootpd
{
	using System.IO;

	public class Directories
	{
		public static void Create(string path)
		{
			Directory.CreateDirectory(path);
		}

		public static void Delete(string path)
		{
			Directory.Delete(path, true);
		}
	}
}
