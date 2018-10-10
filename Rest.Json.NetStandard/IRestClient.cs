using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Rest.Json
{
	public interface IRestClient
	{
		void AddDefaultHeader(RestHeader restHeader);
		event Action<HttpRequestMessage> OnSendingRequest;

		//-- SEND -----------------------------------------------------------------------
		T Send<T>(HttpRequestMessage request);
		Task<T> SendAsync<T>(HttpRequestMessage request);
		void Send(HttpRequestMessage request);
		Task SendAsync(HttpRequestMessage request);


		//-- GET -----------------------------------------------------------------------
		T Get<T>(string address, params RestHeader[] headers);
		Task<T> GetAsync<T>(string address, params RestHeader[] headers);
		void Get(string address, params RestHeader[] headers);
		Task GetAsync(string address, params RestHeader[] headers);


		//-- POST -----------------------------------------------------------------------
		T Post<T>(string address, params RestHeader[] headers);
		T Post<T>(string address, object content, params RestHeader[] headers);
		Task<T> PostAsync<T>(string address, params RestHeader[] headers);
		Task<T> PostAsync<T>(string address, object content, params RestHeader[] headers);
		void Post(string address, params RestHeader[] headers);
		void Post(string address, object content, params RestHeader[] headers);
		Task PostAsync(string address, params RestHeader[] headers);
		Task PostAsync(string address, object content, params RestHeader[] headers);


		//-- PUT -----------------------------------------------------------------------
		T Put<T>(string address, params RestHeader[] headers);
		T Put<T>(string address, object content, params RestHeader[] headers);
		Task<T> PutAsync<T>(string address, params RestHeader[] headers);
		Task<T> PutAsync<T>(string address, object content, params RestHeader[] headers);
		void Put(string address, params RestHeader[] headers);
		void Put(string address, object content, params RestHeader[] headers);
		Task PutAsync(string address, params RestHeader[] headers);
		Task PutAsync(string address, object content, params RestHeader[] headers);


		//-- DELETE -----------------------------------------------------------------------
		void Delete(string address, params RestHeader[] headers);
		Task DeleteAsync(string address, params RestHeader[] headers);
		T Delete<T>(string address, params RestHeader[] headers);
		Task<T> DeleteAsync<T>(string address, params RestHeader[] headers);
	}
}
