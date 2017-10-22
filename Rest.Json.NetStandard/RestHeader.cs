using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rest.Json
{
    public class RestHeader
    {
        public string Key { get; }
        public string Value { get; }

        public RestHeader(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var self = (RestHeader)obj;
            return Key == self.Key && Value == self.Value;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return -1;
        }
    }
}
