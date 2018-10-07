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

			if (typeof(T) == typeof(string))
			{
				var contentBytes = await _response.Content.ReadAsStringAsync();
				return (T)Convert.ChangeType(contentBytes, typeof(T));
			}

			string contentType = _response.Content.Headers.ContentType?.MediaType;
			if (string.IsNullOrEmpty(contentType))
				return default(T);

			if (!contentType.Equals("application/json", StringComparison.OrdinalIgnoreCase) && !contentType.Equals("application/xml", StringComparison.OrdinalIgnoreCase))
				return default(T);

			var contentStr = await _response.Content.ReadAsStringAsync();

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
			if (typeof(T) == typeof(object))
				return (dynamic)JsonConvert.DeserializeObject<ExpandoObject>(contentStr);

			return JsonConvert.DeserializeObject<T>(contentStr);
		}
	}
}
