namespace bootpd
{
	using System.IO;
	using System.Xml;

	public class Files
	{
		public static void Create(string path)
		{
			File.Create(path);
		}

		public static void Write(string path, ref byte[] data, long offset = 0)
		{
			using (var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
			{
				fs.Seek(offset, SeekOrigin.Begin);
				fs.Write(data, 0, data.Length);
			}
		}

		public static void Read(string path, ref byte[] data, out int bytesRead, int count = 0, int index = 0)
		{
			var length = 0;

			if (count == 0 || count >= data.Length)
				length = data.Length;
			else
				length = count;

			using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
			{
				fs.Seek(index, SeekOrigin.Begin);
				bytesRead = fs.Read(data, 0, length);
			}
		}

		public static void Copy(string source, string target, bool overwrite = false)
		{
			File.Copy(source, target, overwrite);
		}

		public static void Move(string source, string target, bool overwrite = false)
		{
			if (overwrite && Filesystem.Exist(target))
				File.Delete(target);

			File.Move(source, target);
		}

		public static XmlDocument ReadXML(string path)
		{
			var file = new XmlDocument();
			file.Load(Filesystem.ResolvePath(path));

			return file;
		}
	}
}
