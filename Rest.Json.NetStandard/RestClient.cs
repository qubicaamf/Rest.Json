using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Dynamic;
using System.Net;

namespace Rest.Json
{
	public class RestClient : IRestClient
	{
        private readonly string _baseAddress;
        private readonly List<RestHeader> _defaultHeaders = new List<RestHeader>();
        public event Action<HttpRequestMessage> OnSendingRequest = message => { };
		private HttpMessageHandler _customHttpMessageHandler;

#if NETSTANDARD2_0
		public bool SkipSslValidation { get; set; }
#endif


		public RestClient() : this(string.Empty)
		{
		}

        public RestClient(Uri baseAddress)
        {
            _baseAddress = baseAddress.ToString();
        }

        public RestClient(string baseAddress)
		{
			_baseAddress = baseAddress;
		}

		public void UseMessageHandler(HttpMessageHandler httpMessageHandler)
		{
			_customHttpMessageHandler = httpMessageHandler;
		}

        private HttpRequestMessage CreateRequest(HttpMethod httpMethod, string address, object requestContent, RestHeader[] headers)
        {
            var request = new HttpRequestMessage(httpMethod, address);
            
            if (requestContent != null)
            {
                if (requestContent.GetType() == typeof(byte[]))
                    request.Content = new ByteArrayContent((byte[])requestContent);
                else if (requestContent.GetType() == typeof(string))
                    request.Content = new StringContent((string)requestContent, Encoding.UTF8, "text/plain");
                else
                    request.Content = new StringContent(JsonConvert.SerializeObject(requestContent, Formatting.Indented), Encoding.UTF8, "application/json");
            }

            foreach (var defaultHeader in _defaultHeaders)
                AddHeader(request, defaultHeader);

            foreach (var header in headers)
                AddHeader(request, header);

            return request;
        }

        private void AddHeader(HttpRequestMessage requestMessage, RestHeader header)
        {
            switch (header.Key)
            {
                case "Authorization":
                    requestMessage.Headers.Authorization = AuthenticationHeaderValue.Parse(header.Value);
                    break;

                case "Date":
                    requestMessage.Headers.Date = DateTimeOffset.Parse(header.Value);
                    break;

                case "Content-Type":
                    requestMessage.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(header.Value);
                    break;

                default:
                    requestMessage.Headers.Add(header.Key, header.Value);
                    break;
            }
        }

        private async Task<T> ExecuteAsync<T>(HttpRequestMessage request, bool returnValue)
		{
			ApplyBaseUrl(request);
			OnSendingRequest(request);

			using (var httpClient = new HttpClient(GetHttpMessageHandler()))
			{
				HttpResponseMessage response = await httpClient.SendAsync(request);
				return await ProcessResponseAsync<T>(request, returnValue, response);
			}
		}

		private T Execute<T>(HttpRequestMessage request, bool returnValue)
		{
			ApplyBaseUrl(request);
			OnSendingRequest(request);

			using (var httpClient = new HttpClient(GetHttpMessageHandler()))
			{
				HttpResponseMessage response = httpClient.SendAsync(request).GetAwaiter().GetResult();
				return ProcessResponseAsync<T>(request, returnValue, response).GetAwaiter().GetResult();
			}
		}

		private async Task<T> ProcessResponseAsync<T>(HttpRequestMessage request, bool returnValue, HttpResponseMessage response)
		{
			if (typeof(T) == typeof(HttpResponseMessage))
				return (T)Convert.ChangeType(response, typeof(T));

			if (!response.IsSuccessStatusCode)
			{
				throw await BuildRestExcetpion(request, response);
			}

			if (!returnValue)
				return default(T);

			if (response.StatusCode == HttpStatusCode.NoContent)
				return default(T);

			if (typeof(T) == typeof(byte[]))
			{
				var contentBytes = await response.Content.ReadAsByteArrayAsync();
				return (T)Convert.ChangeType(contentBytes, typeof(T));
			}

			if (typeof(T) == typeof(string))
			{
				var contentBytes = await response.Content.ReadAsStringAsync();
				return (T)Convert.ChangeType(contentBytes, typeof(T));
			}

			if (string.IsNullOrEmpty(response.Content.Headers.ContentType?.MediaType))
				return default(T);

			if (!response.Content.Headers.ContentType.MediaType.Equals("application/json", StringComparison.InvariantCultureIgnoreCase))
				return default(T);

			return await ReadJsonContent<T>(response);
		}

		private void ApplyBaseUrl(HttpRequestMessage request)
		{
			if (!string.IsNullOrEmpty(_baseAddress))
			{
				var baseUri = _baseAddress.EndsWith("/") ? new Uri(_baseAddress) : new Uri(_baseAddress + "/");

				var relativeUri = request.RequestUri != null ? request.RequestUri.ToString() : string.Empty;
				if (relativeUri.StartsWith("/"))
					relativeUri = relativeUri.Remove(0, 1);

				request.RequestUri = new Uri(baseUri, relativeUri);
			}
		}

		private async Task<RestException> BuildRestExcetpion(HttpRequestMessage request, HttpResponseMessage response)
		{
			string errorContentString = null;
			dynamic errorContent = null;
			try
			{
				errorContentString = await response.Content.ReadAsStringAsync();

				if (!string.IsNullOrEmpty(errorContentString))
				{
					errorContent = JsonConvert.DeserializeObject<ExpandoObject>(errorContentString);
				}
			}
			catch
			{
			}

			throw new RestException(request, response, errorContentString, errorContent);
		}

		private HttpMessageHandler GetHttpMessageHandler()
		{
			if (_customHttpMessageHandler != null)
				return _customHttpMessageHandler;

			var httpClientHandler = new HttpClientHandler
			{
				AllowAutoRedirect = false
			};


#if NETSTANDARD2_0
			if (SkipSslValidation)
				httpClientHandler.ServerCertificateCustomValidationCallback = (h, cert, x, s) => true;
#endif

			return httpClientHandler;
		}

		private async Task<T> ReadJsonContent<T>(HttpResponseMessage response)
	    {
	        var contentStr = await response.Content.ReadAsStringAsync();

            if (typeof(T) == typeof(object))
                return (dynamic)JsonConvert.DeserializeObject<ExpandoObject>(contentStr);

            return JsonConvert.DeserializeObject<T>(contentStr);
        }

        public void AddDefaultHeader(RestHeader restHeader)
        {
            _defaultHeaders.Add(restHeader);
        }



        //-- SEND -----------------------------------------------------------------------
        public T Send<T>(HttpRequestMessage request)
        {
			return Execute<T>(request, true);
		}

        public async Task<T> SendAsync<T>(HttpRequestMessage request)
        {
			return await ExecuteAsync<T>(request, true);
		}

        public void Send(HttpRequestMessage request)
        {
			Execute<object>(request, false);
		}

        public async Task SendAsync(HttpRequestMessage request)
        {
			await ExecuteAsync<object>(request, false);
		}


        //-- GET -----------------------------------------------------------------------
        public T Get<T>(string address, params RestHeader[] headers)
		{
			var request = CreateRequest(HttpMethod.Get, address, null, headers);
			return Execute<T>(request, true);
		}

		public async Task<T> GetAsync<T>(string address, params RestHeader[] headers)
		{
			var request = CreateRequest(HttpMethod.Get, address, null, headers);
			return await ExecuteAsync<T>(request, true);
		}
	    public void Get(string address, params RestHeader[] headers)
	    {
			var request = CreateRequest(HttpMethod.Get, address, null, headers);
			Execute<object>(request, false);
		}

	    public async Task GetAsync(string address, params RestHeader[] headers)
	    {
			var request = CreateRequest(HttpMethod.Get, address, null, headers);
			await ExecuteAsync<object>(request, false);
		}



        //-- POST -----------------------------------------------------------------------
        public T Post<T>(string address, object content, params RestHeader[] headers)
		{
			var request = CreateRequest(HttpMethod.Post, address, content, headers);
			return Execute<T>(request, true);
		}

        public async Task<T> PostAsync<T>(string address, object content, params RestHeader[] headers)
		{
			var request = CreateRequest(HttpMethod.Post, address, content, headers);
			return await ExecuteAsync<T>(request, true);
		}

        public void Post(string address, object content, params RestHeader[] headers)
		{
			var request = CreateRequest(HttpMethod.Post, address, content, headers);
			Execute<object>(request, false);
		}

        public async Task PostAsync(string address, object content, params RestHeader[] headers)
		{
			var request = CreateRequest(HttpMethod.Post, address, content, headers);
			await ExecuteAsync<object>(request, false);
		}



        //-- PUT -----------------------------------------------------------------------
        public T Put<T>(string address, object content, params RestHeader[] headers)
		{
			var request = CreateRequest(HttpMethod.Put, address, content, headers);
			return Execute<T>(request, true);
		}

        public async Task<T> PutAsync<T>(string address, object content, params RestHeader[] headers)
		{
			var request = CreateRequest(HttpMethod.Put, address, content, headers);
			return await ExecuteAsync<T>(request, true);
		}

        public void Put(string address, object content, params RestHeader[] headers)
		{
			var request = CreateRequest(HttpMethod.Put, address, content, headers);
			Execute<object>(request, false);
		}

		public async Task PutAsync(string address, object content, params RestHeader[] headers)
		{
			var request = CreateRequest(HttpMethod.Put, address, content, headers);
			await ExecuteAsync<object>(request, false);
		}



        //-- DELETE -----------------------------------------------------------------------
        public T Delete<T>(string address, params RestHeader[] headers)
        {
			var request = CreateRequest(HttpMethod.Delete, address, null, headers);
			return Execute<T>(request, true);
		}

        public async Task<T> DeleteAsync<T>(string address, params RestHeader[] headers)
        {
			var request = CreateRequest(HttpMethod.Delete, address, null, headers);
			return await ExecuteAsync<T>(request, true);
		}

        public void Delete(string address, params RestHeader[] headers)
        {
			var request = CreateRequest(HttpMethod.Delete, address, null, headers);
			Execute<object>(request, false);
		}

        public async Task DeleteAsync(string address, params RestHeader[] headers)
        {
			var request = CreateRequest(HttpMethod.Delete, address, null, headers);
			await ExecuteAsync<object>(request, false);
		}
    }
}
