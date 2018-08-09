using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Rest.Json
{
	public class RestException : Exception
	{
		public HttpStatusCode StatusCode => Response.StatusCode;
		public HttpRequestMessage Request { get; }
		public HttpResponseMessage Response { get; }
        public dynamic Content { get; }

        internal RestException(HttpRequestMessage request, HttpResponseMessage response, dynamic content)
	        : base($"{request.RequestUri} => {response.StatusCode} ({(int)response.StatusCode}), Reason: {response.ReasonPhrase}")
        {
	        Request = request;
            Response = response;
            Content = content;
        }
    }
}
