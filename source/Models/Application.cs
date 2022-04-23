using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vstat.Models
{
	public class Application
	{
		public string Name { get; set; }
		public string Id { get; set; }

		public DateTime LastUpdatedOn { get; set; }
		
		public string Policy { get; set; }

		public string Teams { get; set; }

		public string BusinessUnit { get; set; }
		
		public string BusinessOwnerEmail { get; set; }

		public string LatestBuildNumber { get; set; }

		public Build Build { get; set; }
	}
}
