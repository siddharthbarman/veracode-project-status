using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vstat.Models
{
	public class ApplicationInfo
	{
		public ApplicationInfo()
		{
		}

		public ApplicationInfo(string id, string name)
		{
			Id = id;
			Name = name;
		}

		public string Id { get; set;  }
		public string Name { get; set; }
	}
}
