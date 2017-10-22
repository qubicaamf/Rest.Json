using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Rest.Json.Tests.Models;
using System.Net.Http;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.IO;
using Rest.Json.Tests;

namespace WebApplicationCore.Controllers
{
    [Route("api/[controller]")]
    public class TestController : Controller
    {
        private List<TestModel> _models = new List<TestModel> {
                new TestModel { Id = 1, Name = "Gino" },
                new TestModel { Id = 2, Name = "Pino" }
            };

        // GET api/values
        [HttpGet]
        public IEnumerable<TestModel> Get()
        {
            return _models;
        }

        [HttpGet("{id}")]
        public TestModel Get(int id)
        {
            return _models.First(m => m.Id == id);
        }

        [HttpPost]
        public TestModel Post([FromBody]TestModel model)
        {
            return model;
        }

        [HttpPut("{id}")]
        public TestModel Put(int id, [FromBody]TestModel model)
        {
            return model;
        }

        [HttpDelete("{id}")]
        public void Delete(int id, int? errorCode = null)
        {
            if (errorCode.HasValue)
                throw new ApplicationException();

            //Return OK instead of NoContent of OWIN implementation ?
        }

        [HttpGet]
        [Route("error")]
        public IActionResult Error()
        {
            return BadRequest(new
            {
                Error = new
                {
                    Code = "MyErrorCode",
                    Message = "MyErrorMessage"
                }
            });
        }

        [HttpPost, HttpPut]
        [Route("textecho")]
        public IActionResult TextEcho()
        {
            return Content(Request.Body.ReadAsString(), "text/plain");
        }

        [HttpGet("mykey")]
        public string MyKey()
        {
            var str = Request.Headers["MyKey"].FirstOrDefault();

            return str;
        }

        [HttpGet("authorization")]
        public string Authorization()
        {
            var str = Request.Headers["Authorization"].FirstOrDefault();

            return str;
        }

        [HttpGet]
        [Route("returnnocontent")]
        public IActionResult ReturnNoContent()
        {
            return NoContent();
        }

        [HttpGet]
        [Route("html")]
        public IActionResult Html()
        {
            return Content("<html><body>Ciao</body></html>", "text/html");
        }

        [HttpGet]
        [Route("redirect")]
        public IActionResult Redirect()
        {
            return Redirect("http://www.google.it");
        }
    }
}
