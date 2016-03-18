namespace WDSServer
{
	using System;
	using System.IO;
	using System.Xml.Serialization;

	public class XmlManager<T>
	{
		public Type Type;

		public XmlManager()
		{
			this.Type = typeof(T);
		}

		public T Load(string path)
		{
			T instance;
			using (var reader = new StreamReader(path))
			{
				var xml = new XmlSerializer(this.Type);
				instance = (T)xml.Deserialize(reader);
			}

			return instance;
		}

		public void Save(string path, object obj)
		{
			using (var writer = new StreamWriter(path))
			{
				var xml = new XmlSerializer(this.Type);
				xml.Serialize(writer, obj);
			}
		}
	}
}
