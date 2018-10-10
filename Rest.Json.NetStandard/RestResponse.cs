using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Rest.Json
{
	public interface IRestResponse
	{
		bool IsSuccessStatusCode { get; }
		HttpStatusCode StatusCode { get; }
		HttpResponseHeaders Headers { get; }
		HttpResponseMessage HttpResponse { get; }

		Task<T> ContentAsync<T>();
		T Content<T>();
	}

	internal class RestResponse : IRestResponse
	{
		private readonly HttpRequestMessage _request;
		private readonly bool _returnValue;
		private readonly HttpResponseMessage _response;

		public RestResponse(HttpRequestMessage request, bool returnValue, HttpResponseMessage response)
		{
			_request = request;
			_returnValue = returnValue;
			_response = response;
		}

		public bool IsSuccessStatusCode => _response.IsSuccessStatusCode;
		public HttpStatusCode StatusCode => _response.StatusCode;
		public HttpResponseHeaders Headers => _response.Headers;
		public HttpResponseMessage HttpResponse => _response;

		public T Process<T>()
		{
			return ProcessAsync<T>().GetAwaiter().GetResult();
		}

		public async Task<T> ProcessAsync<T>()
		{
			if (typeof(T) == typeof(HttpResponseMessage))
				return (T)Convert.ChangeType(_response, typeof(T));

			if (!_response.IsSuccessStatusCode)
			{
				throw await BuildRestExcetpion();
			}

			if (!_returnValue)
				return default(T);

			if (_response.StatusCode == HttpStatusCode.NoContent)
				return default(T);

			return await ContentAsync<T>();
		}

		public T Content<T>()
		{
			return ContentAsync<T>().GetAwaiter().GetResult();
		}

		public async Task<T> ContentAsync<T>()
		{
			if (typeof(T) == typeof(byte[]))
			{
				var contentBytes = await _response.Content.ReadAsByteArrayAsync();
				return (T)Convert.ChangeType(contentBytes, typeof(T));
			}

			var contentStr = await _response.Content.ReadAsStringAsync();

			if (typeof(T) == typeof(string))
				return (T)Convert.ChangeType(contentStr, typeof(T));

			return DeserializeContent<T>(contentStr);
		}

		private async Task<RestException> BuildRestExcetpion()
		{
			string errorContentString = null;
			dynamic errorContent = null;
			try
			{
				errorContentString = await _response.Content.ReadAsStringAsync();

				if (!string.IsNullOrEmpty(errorContentString))
				{
					errorContent = DeserializeContent<dynamic>(errorContentString);
				}
			}
			catch
			{
			}

			throw new RestException(_request, _response, errorContentString, errorContent);
		}

		private T DeserializeContent<T>(string contentStr)
		{
			string mediaType = _response.Content.Headers.ContentType?.MediaType;
			if (string.IsNullOrEmpty(mediaType))
				return default(T);

			if (mediaType.IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0)
				return DeserializeJsonContent<T>(contentStr);

			if (mediaType.IndexOf("xml", StringComparison.OrdinalIgnoreCase) >= 0)
				return DeserializeXmlContent<T>(contentStr);

			return default(T);
		}

		private T DeserializeJsonContent<T>(string contentStr)
		{
			try
			{
				if (typeof(T) == typeof(object))
				return (dynamic)JsonConvert.DeserializeObject<ExpandoObject>(contentStr);
			
				return JsonConvert.DeserializeObject<T>(contentStr);
			}
			catch (Exception ex)
			{
				throw new ArgumentException($"Return type {typeof(T)} can not be deserialize in JSON format", ex);
			}
		}

		private T DeserializeXmlContent<T>(string contentStr)
		{
			try
			{
				return XmlConvert.DeserializeObject<T>(contentStr);
			}
			catch (Exception ex)
			{
				throw new ArgumentException($"Return type {typeof(T)} can not be deserialize in XML format", ex);
			}
		}
	}
}
