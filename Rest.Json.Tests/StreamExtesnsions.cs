using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Rest.Json.Tests
{
	public static class StreamExtesnsions
	{
		public static string ReadAsString(this Stream stream)
		{
			return ReadAsStringAsync(stream).GetAwaiter().GetResult();
		}

		public static async Task<string> ReadAsStringAsync(this Stream stream)
		{
			var bytes = await stream.ReadAsByteArrayAsync();
			var str = Encoding.UTF8.GetString(bytes);
			return str;
		}

		public static byte[] ReadAsByteArray(this Stream stream)
		{
			return ReadAsByteArrayAsync(stream).GetAwaiter().GetResult();
		}

		public static async Task<byte[]> ReadAsByteArrayAsync(this Stream stream)
		{
			using (var ms = new MemoryStream())
			{
				await stream.CopyToAsync(ms);
				var bytes = ms.ToArray();
				return bytes;
			}
		}
	}
}
