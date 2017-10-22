using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rest.Json
{
    public class RestContentTypeHeader : RestHeader
    {
        public RestContentTypeHeader(string value) : base("Content-Type", value)
        {
        }
    }
}
