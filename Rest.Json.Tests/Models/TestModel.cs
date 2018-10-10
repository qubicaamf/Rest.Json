using System;
using System.Collections.Generic;
using System.Text;

namespace Rest.Json.Tests.Models
{
	public class TestModel
	{
		public int Id { get; set; }
		public string Name { get; set; }

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
			{
				return false;
			}

			var self = (TestModel)obj;
			return Name == self.Name && Id == self.Id;
		}

		public override int GetHashCode()
		{
			return -1;
		}
	}
}
