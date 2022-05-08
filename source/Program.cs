using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using vstat.biz;
using vstat.Models;

namespace vstat
{
	class Program
	{
		static List<Application> GetApplications(string profile, int maxProjects)
		{
			VeraCodeApi gen = new VeraCodeApi(profile);
			List<string> appIds = gen.GetApplicationIds();
			List<Application> apps = new List<Application>();			
			
			Console.Write("Retrieving projects progress: ");
			int left = Console.CursorLeft;			
			int count = 1;
			object _lock = new object();

			ParallelLoopResult result = Parallel.ForEach(appIds, appId =>
			{
				VeraCodeApi localGen = new VeraCodeApi(profile);

				Application app = localGen.GetApplicationInfo(appId);
				lock (_lock)
				{
					apps.Add(app);
				}
				Interlocked.Increment(ref count);
				
				Build build = localGen.GetBuildInfo(app.Id);
				if (build != null)
				{
					app.Build = build;
				}

				int percent = count * 100 / appIds.Count;
				Console.CursorLeft = left;
				Console.Write(string.Format("{0}%", percent));

				if (count > maxProjects)
				{
					return;
				}
			});

			Console.WriteLine();
			return apps;
		}

		static void Help()
		{
			Console.WriteLine("Generate VeraCode status reports for all projects.");
			Console.WriteLine("Syntax:");
			Console.WriteLine("vstat -p VeraCodeProfile -r ReportFilename -o FolderPath");
			Console.WriteLine("-p: Specifies the profile in the VeraCode credentials file to use to authenticate");
			Console.WriteLine("-r: Specifies the name of the report file, the filename will be appended with the current date");
			Console.WriteLine("-o: Generates html file with status results in the folder specified");
			Console.WriteLine("-t: Specifies the output type, html and csv are accepted");
		}
	
		static void GenerateReport(string profile, string folder, string reportName, int maxProjects, OutputFormat format)
		{
			List<Application> apps = GetApplications(profile, maxProjects);
			string reportFile = null;
			
			if (format == OutputFormat.HTML)
			{
				reportFile = GenerateHTMLReport(apps, folder, reportName, maxProjects);
			}
			else
			{
				reportFile = GenerateCSVReport(apps, folder, reportName, maxProjects);
			}

			Console.WriteLine("Report has been saved to {0}", reportFile);
		}

		static string GenerateHTMLReport(List<Application> apps, string folder, string reportName, int maxProjects)
		{
			StringBuilder sb = new StringBuilder();

			apps.Sort((a, b) => a.Teams.CompareTo(b.Teams));

			foreach (Application app in apps)
			{
				string policyName = "Not available";
				string policyStatus = "Not available";
				string image = FAIL_IMAGE;

				if (app.Build != null)
				{
					policyName = app.Build.PolicyName;
					policyStatus = app.Build.PolicyComplianceStatus;
					if (string.Equals(app.Build.PolicyComplianceStatus, "pass",
						StringComparison.CurrentCultureIgnoreCase))
					{
						image = PASS_IMAGE;
					}
				}
				sb.AppendFormat(TABLE_ROW_TEMPLATE, app.Teams, app.Name, policyName, policyStatus, image);
			}

			string reportFile = WriteHTMLReport(folder, reportName, sb.ToString());
			return reportFile;
		}

		static string GenerateCSVReport(List<Application> apps, string folder, string reportName, int maxProjects)
		{			 
			StringBuilder sb = new StringBuilder();

			sb.AppendLine("\"team\",\"project\",\"policyname\",\"policystatus\"");
			apps.Sort((a, b) => a.Teams.CompareTo(b.Teams));

			foreach (Application app in apps)
			{
				string policyName = "Not available";
				string policyStatus = "Not available";

				if (app.Build != null)
				{
					policyName = app.Build.PolicyName;
					policyStatus = app.Build.PolicyComplianceStatus;					
				}
				
				sb.AppendLine(string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\"", app.Teams, 
					app.Name, policyName, policyStatus));
			}

			string reportFile = WriteCSVReport(folder, reportName, sb.ToString());
			return reportFile;
		}

		static string GetHTMLReportFilePath(string folder, string reportName)
		{
			string filename = string.Format("{0}-{1}.html", 
				reportName,
				DateTime.Now.ToShortDateString().Replace("/", "-"));
			return Path.Combine(folder, filename);			
		}

		static string GetCSVReportFilePath(string folder, string reportName)
		{
			string filename = string.Format("{0}-{1}.csv",
				reportName,
				DateTime.Now.ToShortDateString().Replace("/", "-"));
			return Path.Combine(folder, filename);
		}

		static string WriteHTMLReport(string folder, string reportName, string content)
		{
			string template = File.ReadAllText(TEMPLATE_HTML_FILE);
			template = template.Replace(VAR_TITLE, string.Format("SurveilX VeraCode Status as on {0}",
				DateTime.Now.ToShortDateString()));
			template = template.Replace(VAR_ROWS, content);
			
			string filePath = GetHTMLReportFilePath(folder, reportName);
			File.WriteAllText(filePath, template);			
			
			File.Copy(PASS_IMAGE, Path.Combine(folder, PASS_IMAGE), true);
			File.Copy(FAIL_IMAGE, Path.Combine(folder, FAIL_IMAGE), true);

			return filePath;
		}

		static string WriteCSVReport(string folder, string reportName, string content)
		{
			string filePath = GetCSVReportFilePath(folder, reportName);
			File.WriteAllText(filePath, content);
			return filePath;
		}

		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Help();
				return;
			}

			CmdLine cmd = new CmdLine(args);
			string profile = cmd.GetFlagValue(FLAG_PROFILE, null);
			if (string.IsNullOrEmpty(profile))
			{
				Console.WriteLine("Veracode profile not specified!");
				return;
			}

			string folder = cmd.GetFlagValue(FLAG_FOLDER, null);
			if (string.IsNullOrEmpty(folder))
			{
				Console.WriteLine("Output folder not specified!");
				return;
			}

			string outputType = cmd.GetFlagValue(FLAG_OUTPUT_TYPE, OutputFormat.CSV.ToString());
			OutputFormat format;

			if (!Enum.TryParse<OutputFormat>(outputType, true, out format))
            {
				Console.WriteLine("Invalid output format!");
				return;
			}

			if (string.Equals("csv", outputType, StringComparison.CurrentCultureIgnoreCase))
			{
				format = OutputFormat.CSV;
			}
			else if (string.Equals("html", outputType, StringComparison.CurrentCultureIgnoreCase))
			{
				format = OutputFormat.HTML;
			}
			else
			{
				Console.WriteLine("Invalid output forma.!");
				return;
			}

			string reportName = cmd.GetFlagValue(FLAG_REPORT_NAME, "veracode");
			int maxProjects = cmd.GetFlagValue<int>(FLAG_MAX_PROJECTS, int.MaxValue);

			GenerateReport(profile, folder, reportName, maxProjects, format);
		}

		private static readonly string FLAG_PROFILE = "p";
		private static readonly string FLAG_FOLDER = "o";
		private static readonly string FLAG_MAX_PROJECTS = "m";
		private static readonly string FLAG_REPORT_NAME = "r";
		private static readonly string FLAG_OUTPUT_TYPE = "t";

		private static readonly string TABLE_ROW_TEMPLATE = @"				
				<tr>
					<td>{0}</td>
					<td>{1}</td>
					<td>{2}</td>
					<td>{3}</td>
					<td><img src = ""{4}"" height=""40""></td>
				</tr>"; // team, project, policy, status, image
		
		private static readonly string VAR_TITLE = "{{title}}";
		private static readonly string VAR_ROWS = "{{rows}}";
		private static readonly string TEMPLATE_HTML_FILE = @"resources\template.html";
		private static readonly string PASS_IMAGE = @"resources\greenshield.png";
		private static readonly string FAIL_IMAGE = @"resources\redshield.png";
	}
}
