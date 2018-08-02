using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Dynamic;
using System.Net;
using System.Globalization;

namespace Rest.Json
{
	public class RestClient : IRestClient
	{
        private readonly string _baseAddress;
        private readonly List<RestHeader> _defaultHeaders = new List<RestHeader>();
        public event Action<HttpRequestMessage> OnSendingRequest = message => { };
		private HttpMessageHandler _customHttpMessageHandler;

		public bool SkipSslValidation { get; set; }

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

        private async Task ExecuteAsync(HttpMethod httpMethod, string address, object requestContent, params RestHeader[] headers)
        {
            await ProcessAsync<object>(httpMethod, address, requestContent, headers, false);
        }

        private async Task<T> ExecuteAsync<T>(HttpMethod httpMethod, string address, object requestContent, params RestHeader[] headers)
        {
            return await ProcessAsync<T>(httpMethod, address, requestContent, headers, true);
        }

        private async Task ExecuteAsync(HttpRequestMessage request)
        {
            await ProcessAsync<object>(request, false);
        }

        private async Task<T> ExecuteAsync<T>(HttpRequestMessage request)
        {
            return await ProcessAsync<T>(request, true);
        }

        private async Task<T> ProcessAsync<T>(HttpMethod httpMethod, string address, object requestContent, RestHeader[] headers, bool returnValue)
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

            return await ProcessAsync<T>(request, returnValue);
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

        private async Task<T> ProcessAsync<T>(HttpRequestMessage request, bool returnValue)
		{
			if (!string.IsNullOrEmpty(_baseAddress))
			{
				var baseUri = _baseAddress.EndsWith("/") ? new Uri(_baseAddress) : new Uri(_baseAddress + "/");

				var relativeUri = request.RequestUri != null ? request.RequestUri.ToString() : string.Empty;
				if (relativeUri.StartsWith("/"))
					relativeUri = relativeUri.Remove(0, 1);

				request.RequestUri = new Uri(baseUri, relativeUri);
			}

			OnSendingRequest(request);

			using (var httpClient = new HttpClient(GetHttpMessageHandler()))
			{
				HttpResponseMessage response = await httpClient.SendAsync(request);

				if (typeof(T) == typeof(HttpResponseMessage))
					return (T)Convert.ChangeType(response, typeof(T));

				if ((int)response.StatusCode >= 300)
				{
					dynamic errorContent = null;
					try
					{
						errorContent = await ReadJsonContent<dynamic>(response);
					}
					catch
					{
						// ignored
					}

					throw new RestException(response.StatusCode, response, errorContent);
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
		}

		private HttpMessageHandler GetHttpMessageHandler()
		{
			if (_customHttpMessageHandler != null)
				return _customHttpMessageHandler;

			var httpClientHandler = new HttpClientHandler
			{
				AllowAutoRedirect = false
			};

			if (SkipSslValidation)
				httpClientHandler.ServerCertificateCustomValidationCallback = (h, cert, x, s) => true;

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
            return ExecuteAsync<T>(request).GetAwaiter().GetResult();
        }

        public async Task<T> SendAsync<T>(HttpRequestMessage request)
        {
            return await ExecuteAsync<T>(request);
        }

        public void Send(HttpRequestMessage request)
        {
            SendAsync(request).GetAwaiter().GetResult();
        }

        public async Task SendAsync(HttpRequestMessage request)
        {
            await ExecuteAsync(request);
        }


        //-- GET -----------------------------------------------------------------------
        public T Get<T>(string address, params RestHeader[] headers)
		{
			return GetAsync<T>(address, headers).GetAwaiter().GetResult();
		}

		public async Task<T> GetAsync<T>(string address, params RestHeader[] headers)
		{
			return await ExecuteAsync<T>(HttpMethod.Get, address, null, headers);
		}
	    public void Get(string address, params RestHeader[] headers)
	    {
	        GetAsync(address, headers).GetAwaiter().GetResult();
	    }

	    public async Task GetAsync(string address, params RestHeader[] headers)
	    {
	        await ExecuteAsync(HttpMethod.Get, address, null, headers);
	    }



        //-- POST -----------------------------------------------------------------------
        public T Post<T>(string address, object content, params RestHeader[] headers)
		{
			return PostAsync<T>(address, content, headers).GetAwaiter().GetResult();
		}

        public async Task<T> PostAsync<T>(string address, object content, params RestHeader[] headers)
		{
			return await ExecuteAsync<T>(HttpMethod.Post, address, content, headers);
		}

        public void Post(string address, object content, params RestHeader[] headers)
		{
			PostAsync(address, content, headers).GetAwaiter().GetResult();
		}

        public async Task PostAsync(string address, object content, params RestHeader[] headers)
		{
			await ExecuteAsync(HttpMethod.Post, address, content, headers);
		}



        //-- PUT -----------------------------------------------------------------------
        public T Put<T>(string address, object content, params RestHeader[] headers)
		{
			return PutAsync<T>(address, content, headers).GetAwaiter().GetResult();
		}

        public async Task<T> PutAsync<T>(string address, object content, params RestHeader[] headers)
		{
			return await ExecuteAsync<T>(HttpMethod.Put, address, content, headers);
		}

        public void Put(string address, object content, params RestHeader[] headers)
		{
			PutAsync(address, content, headers).GetAwaiter().GetResult();
		}

		public async Task PutAsync(string address, object content, params RestHeader[] headers)
		{
			await ExecuteAsync(HttpMethod.Put, address, content, headers);
		}



        //-- DELETE -----------------------------------------------------------------------
        public T Delete<T>(string address, params RestHeader[] headers)
        {
            return DeleteAsync<T>(address, headers).GetAwaiter().GetResult();
        }

        public async Task<T> DeleteAsync<T>(string address, params RestHeader[] headers)
        {
            return await ExecuteAsync<T>(HttpMethod.Delete, address, null, headers);
        }

        public void Delete(string address, params RestHeader[] headers)
        {
            DeleteAsync(address, headers).GetAwaiter().GetResult();
        }

        public async Task DeleteAsync(string address, params RestHeader[] headers)
        {
            await ExecuteAsync(HttpMethod.Delete, address, null, headers);
        }
    }
}
