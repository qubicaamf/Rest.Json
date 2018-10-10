using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

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

			foreach (var defaultHeader in _defaultHeaders)
				AddHeader(request, defaultHeader);

			foreach (var header in headers)
				AddHeader(request, header);

			if (requestContent != null)
            {
				var contentType = GetContentType(headers);

				if (requestContent.GetType() == typeof(byte[]))
				{
					request.Content = new ByteArrayContent((byte[])requestContent);
					if (contentType != null)
						request.Content.Headers.ContentType = contentType;
				}
				else if (requestContent.GetType() == typeof(string))
				{
					Encoding encoding = Encoding.UTF8;
					string mediaType = "text/plain";
					if (contentType != null)
					{
						if (!string.IsNullOrEmpty(contentType.CharSet))
							encoding = Encoding.GetEncoding(contentType.CharSet);

						if (!string.IsNullOrEmpty(contentType.MediaType))
							mediaType = contentType.MediaType;
					}

					request.Content = new StringContent((string)requestContent, encoding, mediaType);
				}
				else if (contentType != null && contentType.MediaType != null && contentType.MediaType.ToLower().Contains("xml"))
				{
					Encoding encoding = !string.IsNullOrEmpty(contentType.CharSet) ? Encoding.GetEncoding(contentType.CharSet) : Encoding.UTF8;

					string xml;
					try
					{
						xml = XmlConvert.SerializeObject(requestContent, encoding);
					}
					catch (Exception ex)
					{
						throw new ArgumentException($"Content type {requestContent.GetType()} can not be serialize in XML format", ex);
					}
					
					request.Content = new StringContent(xml, encoding, contentType.MediaType);
				}
                else
				{
					Encoding encoding = Encoding.UTF8;
					string mediaType = "application/json";
					if (contentType != null)
					{
						if (!string.IsNullOrEmpty(contentType.CharSet))
							encoding = Encoding.GetEncoding(contentType.CharSet);

						if (!string.IsNullOrEmpty(contentType.MediaType))
							mediaType = contentType.MediaType;
					}

					string json;
					try
					{
						json = JsonConvert.SerializeObject(requestContent, Formatting.Indented);
					}
					catch (Exception ex)
					{
						throw new ArgumentException($"Content type {requestContent.GetType()} can not be serialize in JSON format", ex);
					}
					
					request.Content = new StringContent(json, encoding, mediaType);
				}

				
			}

            return request;
        }

		private MediaTypeHeaderValue GetContentType(RestHeader[] headers)
		{
			var contentTypeHeader = _defaultHeaders.FirstOrDefault(h => h.Key == "Content-Type");
			if (contentTypeHeader != null)
				return MediaTypeHeaderValue.Parse(contentTypeHeader.Value);

			contentTypeHeader = headers.FirstOrDefault(h => h.Key == "Content-Type");
			if (contentTypeHeader != null)
				return MediaTypeHeaderValue.Parse(contentTypeHeader.Value);

			return null;
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

				var restResponse = new RestResponse(request, returnValue, response);

				if (typeof(T) == typeof(IRestResponse))
					return (T)(IRestResponse)restResponse;

				return await restResponse.ProcessAsync<T>();
			}
		}

		private T Execute<T>(HttpRequestMessage request, bool returnValue)
		{
			ApplyBaseUrl(request);
			OnSendingRequest(request);

			using (var httpClient = new HttpClient(GetHttpMessageHandler()))
			{
				HttpResponseMessage response = httpClient.SendAsync(request).GetAwaiter().GetResult();

				var restResponse = new RestResponse(request, returnValue, response);

				if (typeof(T) == typeof(IRestResponse))
					return (T)(IRestResponse)restResponse;

				return restResponse.Process<T>();
			}
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
		public T Post<T>(string address, params RestHeader[] headers)
		{
			return Post<T>(address, null, headers);
		}

		public T Post<T>(string address, object content, params RestHeader[] headers)
		{
			var request = CreateRequest(HttpMethod.Post, address, content, headers);
			return Execute<T>(request, true);
		}

		public async Task<T> PostAsync<T>(string address, params RestHeader[] headers)
		{
			return await PostAsync<T>(address, null, headers);
		}

		public async Task<T> PostAsync<T>(string address, object content, params RestHeader[] headers)
		{
			var request = CreateRequest(HttpMethod.Post, address, content, headers);
			return await ExecuteAsync<T>(request, true);
		}

		public void Post(string address, params RestHeader[] headers)
		{
			Post(address, null, headers);
		}

		public void Post(string address, object content, params RestHeader[] headers)
		{
			var request = CreateRequest(HttpMethod.Post, address, content, headers);
			Execute<object>(request, false);
		}

		public async Task PostAsync(string address, params RestHeader[] headers)
		{
			await PostAsync(address, null, headers);
		}

		public async Task PostAsync(string address, object content, params RestHeader[] headers)
		{
			var request = CreateRequest(HttpMethod.Post, address, content, headers);
			await ExecuteAsync<object>(request, false);
		}


		//-- PUT -----------------------------------------------------------------------
		public T Put<T>(string address, params RestHeader[] headers)
		{
			return Put<T>(address, null, headers);
		}

		public T Put<T>(string address, object content, params RestHeader[] headers)
		{
			var request = CreateRequest(HttpMethod.Put, address, content, headers);
			return Execute<T>(request, true);
		}

		public async Task<T> PutAsync<T>(string address, params RestHeader[] headers)
		{
			return await PutAsync<T>(address, null, headers);
		}

		public async Task<T> PutAsync<T>(string address, object content, params RestHeader[] headers)
		{
			var request = CreateRequest(HttpMethod.Put, address, content, headers);
			return await ExecuteAsync<T>(request, true);
		}

		public void Put(string address, params RestHeader[] headers)
		{
			Put(address, null, headers);
		}

		public void Put(string address, object content, params RestHeader[] headers)
		{
			var request = CreateRequest(HttpMethod.Put, address, content, headers);
			Execute<object>(request, false);
		}

		public async Task PutAsync(string address, params RestHeader[] headers)
		{
			await PutAsync(address, null, headers);
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
