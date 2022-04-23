using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vstat.Models
{
	public class Build
	{
		public string Version { get; set; }

		public string Submitter { get; set; }

		public string PolicyName { get; set; }

		public string PolicyComplianceStatus { get; set; }

	}
}
