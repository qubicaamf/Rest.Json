using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Rest.Json.Tests.Models;
using System.Globalization;
using System.Linq;

namespace Rest.Json.Tests
{
    [TestFixture]
    public class RestClientFixture
    {
        private const string BaseAddress = "http://localhost:5000/";
        private IRestClient _restClient;
        private IWebHost _webHost;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _webHost = WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .Build();

            _webHost.Start();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _webHost.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            _restClient = new RestClient(BaseAddress);
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public async Task GetAsync()
        {
            var model = await _restClient.GetAsync<TestModel>("api/test/1");

            Assert.That(model, Is.EqualTo(new TestModel { Id = 1, Name = "Gino" }));
        }

        [Test]
        public void Get()
        {
            var model = _restClient.Get<TestModel>("api/test/1");

            Assert.That(model, Is.EqualTo(new TestModel { Id = 1, Name = "Gino" }));
        }

        [Test]
        public async Task GetAsyncArray()
        {
            var models = await _restClient.GetAsync<TestModel[]>("api/test");

            Assert.That(models.Length, Is.EqualTo(2));
            Assert.That(models[0], Is.EqualTo(new TestModel { Id = 1, Name = "Gino" }));
            Assert.That(models[1], Is.EqualTo(new TestModel { Id = 2, Name = "Pino" }));
        }

        [Test]
        public void ReturnBytes()
        {
            var bytes = _restClient.Get<byte[]>("api/test/1");

            var model = JsonConvert.DeserializeObject<TestModel>(Encoding.UTF8.GetString(bytes));
            Assert.That(model, Is.EqualTo(new TestModel { Id = 1, Name = "Gino" }));
        }

        [Test]
        public void ReturnExceptionOnUnexistingUrl()
        {
            var restClient = new RestClient("http://sadsadsadsadsa.com");

            var ex = Assert.Throws<HttpRequestException>(() => restClient.Get<string>("ooooooooooo/test/1"));

            Console.WriteLine("HttpRequestException: " + ex.Message);
        }

        [Test]
        public void ReturnExceptionOnResponseNotFound()
        {
            var ex = Assert.Throws<RestException>(() => _restClient.Get<string>("ooooooooooo/test/1"));

            Console.WriteLine("RestException: " + ex.Message);
            Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(ex.Content, Is.Null);
	        Assert.That(ex.ContentAsString, Is.EqualTo(string.Empty));
			Assert.That(ex.Response, Is.Not.Null);
        }

        [Test]
        public void ReturnExceptionOnCustomError()
        {
            var ex = Assert.Throws<RestException>(() => _restClient.Get("api/test/error"));

            Console.WriteLine("RestException: " + ex.Message);
            Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(ex.Content.Error.Code, Is.EqualTo("MyErrorCode"));
            Assert.That(ex.Content.Error.Message, Is.EqualTo("MyErrorMessage"));
			Assert.That(ex.ContentAsString, Is.EqualTo("{\"Error\":{\"Code\":\"MyErrorCode\",\"Message\":\"MyErrorMessage\"}}"));
        }

	    [Test]
	    public void ReturnExceptionOnHtmlError()
	    {
		    var ex = Assert.Throws<RestException>(() => _restClient.Get("api/test/errorashtml"));

		    Console.WriteLine("RestException: " + ex.Message);
		    Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
			Assert.That(ex.Content, Is.Null);
			Assert.That(ex.ContentAsString, Is.EqualTo("<html><body>bad request</body></html>"));
	    }

		[Test]
        public async Task PostAsync()
        {
            await _restClient.PostAsync("api/test", new TestModel { Id = 3, Name = "Paperino" });
        }

		[Test]
		public async Task PostNoContentAsync()
		{
			await _restClient.PostAsync("api/test/nocontent");
		}

		[Test]
        public void Post()
        {
            _restClient.Post("api/test", new TestModel { Id = 3, Name = "Paperino" });
        }

		[Test]
		public void PostNoContent()
		{
			_restClient.Post("api/test/nocontent");
		}

		[Test]
        public void PostAndReturnRawBytes()
        {
            var bytesToSend = Encoding.UTF8.GetBytes("ciaone");

            var bytesReceived = _restClient.Post<byte[]>("api/test/textecho", bytesToSend, new RestContentTypeHeader("text/plain"));

            Assert.That(Encoding.UTF8.GetString(bytesReceived), Is.EqualTo("ciaone"));
        }

        [Test]
        public async Task PostAsyncWithReturn()
        {
            var response = await _restClient.PostAsync<TestModel>("api/test", new TestModel { Id = 3, Name = "Paperino" });

            Assert.That(response, Is.EqualTo(new TestModel { Id = 3, Name = "Paperino" }));
        }

		[Test]
		public async Task PostNoContentAsyncWithReturn()
		{
			var response = await _restClient.PostAsync<TestModel>("api/test/nocontent");

			Assert.That(response, Is.EqualTo(new TestModel { Id = 1, Name = "Gino" }));
		}

		[Test]
        public void PostWithReturn()
        {
            var response = _restClient.Post<TestModel>("api/test", new TestModel { Id = 3, Name = "Paperino" });

            Assert.That(response, Is.EqualTo(new TestModel { Id = 3, Name = "Paperino" }));
        }

		[Test]
		public void PostNoContentWithReturn()
		{
			var response = _restClient.Post<TestModel>("api/test/nocontent");

			Assert.That(response, Is.EqualTo(new TestModel { Id = 1, Name = "Gino" }));
		}

		[Test]
        public async Task PutAsync()
        {
            await _restClient.PutAsync("api/test/1", new TestModel { Id = 1, Name = "Gino" });
        }

		[Test]
		public async Task PutNoContentAsync()
		{
			await _restClient.PutAsync("api/test/1/nocontent");
		}

		[Test]
        public void Put()
        {
            _restClient.Put("api/test/1", new TestModel { Id = 1, Name = "Gino" });
        }

		[Test]
		public void PutNoContent()
		{
			_restClient.Put("api/test/1/nocontent");
		}

		[Test]
        public async Task PutAsyncWithReturn()
        {
            var response = await _restClient.PutAsync<TestModel>("api/test/1", new TestModel { Id = 1, Name = "Gino" });

            Assert.That(response, Is.EqualTo(new TestModel { Id = 1, Name = "Gino" }));
        }

		[Test]
		public async Task PutNoContentAsyncWithReturn()
		{
			var response = await _restClient.PutAsync<TestModel>("api/test/1/nocontent");

			Assert.That(response, Is.EqualTo(new TestModel { Id = 1, Name = "Gino" }));
		}

		[Test]
        public void PutWithReturn()
        {
            var response = _restClient.Put<TestModel>("api/test/1", new TestModel { Id = 1, Name = "Gino" });

            Assert.That(response, Is.EqualTo(new TestModel { Id = 1, Name = "Gino" }));
        }

		[Test]
		public void PutNoContentWithReturn()
		{
			var response = _restClient.Put<TestModel>("api/test/1/nocontent");

			Assert.That(response, Is.EqualTo(new TestModel { Id = 1, Name = "Gino" }));
		}

		[Test]
        public void PutBytes()
        {
            var bytes = Encoding.UTF8.GetBytes("ciaone");

            _restClient.Put("api/test/textecho", bytes, new RestContentTypeHeader("text/plain"));
        }

        [Test]
        public void SendString()
        {
            _restClient.Put("api/test/textecho", "ciaone", new RestContentTypeHeader("text/plain"));
        }

        [Test]
        public async Task SetHeader()
        {
            var value = await _restClient.GetAsync<string>("api/test/mykey", new RestHeader("MyKey", "MyValue"));

            Assert.That(value, Is.EqualTo("MyValue"));
        }

        [Test]
        public async Task SetAuthHeader()
        {
            var value = await _restClient.GetAsync<string>("api/test/authorization", new RestAuthHeader("MySchema MyUser:MyPassword"));

            Assert.That(value, Is.EqualTo("MySchema MyUser:MyPassword"));
        }

        [Test]
        public async Task SetDateHeaderWithDateTime()
        {
            var now = DateTime.Now;

            var value = await _restClient.GetAsync<string>("api/test/date", new RestDateHeader(now));
            
            Assert.That(DateTime.Parse(value), Is.EqualTo(new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second)));
        }

        [Test]
        public async Task SetDateHeaderWithDateTimeOffset()
        {
            var now = DateTime.Now;
            var nowPlus4 = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, TimeSpan.FromHours(4));

            var value = await _restClient.GetAsync<string>("api/test/date", new RestDateHeader(nowPlus4));

            Assert.That(DateTimeOffset.Parse(value), Is.EqualTo(nowPlus4));
        }

        [Test]
        public async Task SetDefaultHeader()
        {
            _restClient.AddDefaultHeader(new RestHeader("MyKey", "MyValue"));

            var value = await _restClient.GetAsync<string>("api/test/mykey");

            Assert.That(value, Is.EqualTo("MyValue"));
        }

        [Test]
        public async Task ReturnHttpResponseMessage()
        {
            var response = await _restClient.GetAsync<HttpResponseMessage>("api/test/1");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var resturnedJson = await response.Content.ReadAsStringAsync();
            var expectedJson = JsonConvert.SerializeObject(new TestModel { Id = 1, Name = "Gino" });

            Assert.That(resturnedJson, Is.EqualTo(expectedJson));
        }

        [Test]
        public async Task ReturnHttpResponseMessageDoesntThrowRestException()
        {
            var response = await _restClient.GetAsync<HttpResponseMessage>("api1234/test/1");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task SendAsyncHttpRequestMessage()
        {
            var sentJson = JsonConvert.SerializeObject(new TestModel { Id = 3, Name = "Paperino" });

            var request = new HttpRequestMessage(HttpMethod.Post, "api/test") { Content = new StringContent(sentJson, Encoding.UTF8, "application/json") };

            await _restClient.SendAsync(request);
        }

        [Test]
        public void SendHttpRequestMessage()
        {
            var sentJson = JsonConvert.SerializeObject(new TestModel { Id = 3, Name = "Paperino" });

            var request = new HttpRequestMessage(HttpMethod.Post, "api/test") { Content = new StringContent(sentJson, Encoding.UTF8, "application/json") };

            _restClient.Send(request);
        }

        [Test]
        public async Task SendAsyncHttpRequestMessageWithReturn()
        {
            var sentJson = JsonConvert.SerializeObject(new TestModel { Id = 3, Name = "Paperino" });

            var request = new HttpRequestMessage(HttpMethod.Post, "api/test") { Content = new StringContent(sentJson, Encoding.UTF8, "application/json") };

            var response = await _restClient.SendAsync<TestModel>(request);

            Assert.That(response, Is.EqualTo(new TestModel { Id = 3, Name = "Paperino" }));
        }

        [Test]
        public async Task NormalizeAddress()
        {
            var restClient = new RestClient("http://localhost:5000/api");

            await restClient.GetAsync<TestModel>("test/1");
        }

        [Test]
        public async Task NormalizeAddress2()
        {
            var restClient = new RestClient("http://localhost:5000/api");

            await restClient.GetAsync<TestModel>("/test/1");
        }

        [Test]
        public async Task NormalizeAddress3()
        {
            var restClient = new RestClient("http://localhost:5000/api/test/1");

            await restClient.GetAsync<TestModel>("");
        }

        [Test]
        public async Task EmptyBaseAddress()
        {
            var restClient = new RestClient();

            await restClient.GetAsync<TestModel>("http://localhost:5000/api/test/1");
        }

        [Test]
        public async Task GetDynamic()
        {
            var model = await _restClient.GetAsync<dynamic>("api/test/1");

            Console.WriteLine(model.Id);
            Console.WriteLine(model.Name);

            Assert.That(model.Id, Is.EqualTo(1));
            Assert.That(model.Name, Is.EqualTo("Gino"));
        }

        [Test]
        public async Task DeleteAsync()
        {
            await _restClient.DeleteAsync("api/test/1");
        }

        [Test]
        public void Delete()
        {
            _restClient.Delete("api/test/1");
        }

        [Test]
        public async Task DeleteAsyncReturnsHttpResponseMessage()
        {
            var response = await _restClient.DeleteAsync<HttpResponseMessage>("api/test/1");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public void DeleteReturnsHttpResponseMessage()
        {
            var response = _restClient.Delete<HttpResponseMessage>("api/test/1");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public void OnSendingRequest()
        {
            bool eventFired = false;
            _restClient.OnSendingRequest += request =>
            {
                eventFired = true;
                Assert.That(request.RequestUri, Is.EqualTo(new Uri(BaseAddress + "api/test/1")));
            };

            _restClient.Get<TestModel>("api/test/1");

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void DeleteReturnError()
        {
            var restClient = new RestClient(BaseAddress);

            var ex = Assert.Throws<RestException>(() => restClient.Delete("api/test/1?errorCode=500"));

            Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public async Task ManageNoContentStatusCode()
        {
            var response = await _restClient.GetAsync<HttpResponseMessage>("api/test/returnnocontent");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task ReturnNullOnNoContent()
        {
            var response = await _restClient.GetAsync<dynamic>("api/test/returnnocontent");

            Assert.That(response, Is.Null);
        }

        [Test]
        public async Task ReturnNullOnContentNotJson()
        {
            var response = await _restClient.GetAsync<dynamic>("api/test/html");

            Assert.That(response, Is.Null);
        }

        [Test]
        public void ReturnExceptionOnResponseRedirect3XX()
        {
            var ex = Assert.Throws<RestException>(() => _restClient.Get<dynamic>("api/test/redirect"));

            Console.WriteLine("RestException: " + ex.Message);
            Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
            Assert.That(ex.Content, Is.Null);
            Assert.That(ex.Response, Is.Not.Null);
        }

        [Test]
        public async Task SendAndReceivePlainText()
        {
            var value = await _restClient.PostAsync<string>("api/test/text", "ciao");

            Assert.That(value, Is.EqualTo("ciao"));
        }

		[Test]
		public async Task GetRestResponseAsync()
		{
			var response = await _restClient.GetAsync<IRestResponse>("api/test/1");

			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That(response.Headers.GetValues("x-name").First(), Is.EqualTo("gino"));

			var model = await response.ContentAsync<TestModel>();

			Assert.That(model, Is.EqualTo(new TestModel { Id = 1, Name = "Gino" }));
		}

		[Test]
		public void GetRestResponse()
		{
			var response = _restClient.Get<IRestResponse>("api/test/1");

			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That(response.Headers.GetValues("x-name").First(), Is.EqualTo("gino"));

			var model = response.Content<TestModel>();

			Assert.That(model, Is.EqualTo(new TestModel { Id = 1, Name = "Gino" }));
		}
	}
}
