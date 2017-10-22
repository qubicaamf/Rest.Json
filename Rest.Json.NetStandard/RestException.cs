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
	    public HttpStatusCode StatusCode { get; }
        public HttpResponseMessage Response { get; }
        public dynamic Content { get; }

        public RestException(HttpStatusCode statusCode, HttpResponseMessage response, dynamic content) : base($"{statusCode} ({(int)statusCode}), Reason: {response.ReasonPhrase}")
        {
            StatusCode = statusCode;
            Response = response;
            Content = content;
        }
    }
}
