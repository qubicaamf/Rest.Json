using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Rest.Json
{
	internal static class XmlConvert
	{
		public static string SerializeObject(object value, Encoding encoding)
		{
			var serializer = new XmlSerializer(value.GetType());
			var sb = new StringBuilder();

			using (TextWriter writer = new ExtendedStringWriter(sb, encoding))
			{
				serializer.Serialize(writer, value);
			}

			return sb.ToString();
		}

		public static T DeserializeObject<T>(string value)
		{
			var serializer = new XmlSerializer(typeof(T));
			T result;

			using (TextReader reader = new StringReader(value))
			{
				result = (T)serializer.Deserialize(reader);
			}

			return result;
		}

		
	}

	internal class ExtendedStringWriter : StringWriter
	{
		private readonly Encoding _encoding;

		public ExtendedStringWriter(StringBuilder stringBuilder, Encoding encoding)
			: base(stringBuilder)
		{
			_encoding = encoding;
		}

		public override Encoding Encoding => _encoding;
	}
}
