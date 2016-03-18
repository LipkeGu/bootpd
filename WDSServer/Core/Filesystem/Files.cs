using System;
using System.IO;
using System.Xml;

namespace WDSServer
{
	public class Files
	{
		public static bool Create<T>(string path)
		{
			try
			{
				File.Create(path);

				return true;
			}
			catch (Exception ex)
			{
				Errorhandler.Report(Definitions.LogTypes.Error, ex.Message);

				return false;
			}
		}

		public static void Write(string path, byte[] data, long offset = 0)
		{
			try
			{
				using (var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
				{
					fs.Seek(offset, SeekOrigin.Begin);
					fs.Write(data, 0, data.Length);
				}
			}
			catch (Exception ex)
			{
				Errorhandler.Report(Definitions.LogTypes.Error, ex.Message);
			}
		}

		public static void Read(string path, ref byte[] data, out int bytesRead, int count = 0, int index = 0)
		{
			var readedbytes = 0;
			var length = 0;

			if (count == 0 || count >= data.Length)
				length = data.Length;
			else
				length = count;

			try
			{
				using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
				{
					fs.Seek(index, SeekOrigin.Begin);
					readedbytes = fs.Read(data, 0, length);
					bytesRead = readedbytes;
				}
			}
			catch (Exception ex)
			{
				Errorhandler.Report(Definitions.LogTypes.Error, ex.Message);
				bytesRead = 0;
			}
		}

		public static bool Copy(string source, string target, bool overwrite = false)
		{
			try
			{
				if (!overwrite && Filesystem.Exist(target))
					return false;
				else
				{
					File.Copy(source, target, overwrite);

					return true;
				}
			}
			catch (Exception ex)
			{
				Errorhandler.Report(Definitions.LogTypes.Error, ex.Message);

				return false;
			}
		}

		public static bool Move(string source, string target, bool overwrite = false)
		{
			try
			{
				if (overwrite && Filesystem.Exist(target))
					File.Delete(target);

				File.Move(source, target);

				return true;
			}
			catch (Exception ex)
			{
				Errorhandler.Report(Definitions.LogTypes.Error, ex.Message);

				return false;
			}
		}

		public static XmlDocument ReadXML(string path)
		{
			var file = new XmlDocument();

			if (Filesystem.Exist(path))
				file.Load(path);

			return file;
		}
	}
}
