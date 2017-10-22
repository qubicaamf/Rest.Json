using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rest.Json
{
    public class RestAuthHeader : RestHeader
    {
        public RestAuthHeader(string value) : base("Authorization", value)
        {
        }
    }
}
