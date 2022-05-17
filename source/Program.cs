using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using vstat.biz;
using vstat.Models;
using System.Linq;

namespace vstat
{
	class Program
	{
		static List<Application> GetApplications(string profile, int maxProjects, IEnumerable<string> ignoredAppNames)
		{
			VeraCodeApi gen = new VeraCodeApi(profile);
			List<ApplicationInfo> appInfos = gen.GetApplicationsInfo();

			if (ignoredAppNames != null)
			{
				foreach (string ignoredAppName in ignoredAppNames)
				{
					ApplicationInfo toRemove = appInfos.Where(appInfo => appInfo.Name == ignoredAppName).FirstOrDefault();
					if (toRemove != null)
					{
						Console.WriteLine("Ignoring: {0}", toRemove.Name);
						appInfos.Remove(toRemove);
					}
				}
			}
			
			List<Application> apps = new List<Application>();			
			Console.Write("Retrieving projects progress: ");
			int left = Console.CursorLeft;			
			int count = 0;
			object _lock = new object();

			ParallelLoopResult result = Parallel.ForEach(appInfos, appInfo =>
			{
				VeraCodeApi localGen = new VeraCodeApi(profile);
				Application app = localGen.GetApplicationInfo(appInfo.Id);
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

				int percent = count * 100 / appInfos.Count;
				Console.CursorLeft = left;
				Utils.ConsoleWrite(ConsoleColor.Green, "{0}%", percent);

				if (count > maxProjects)
				{
					return;
				}
			});

			Console.WriteLine();
			return apps;
		}

		private static void Help()
		{
			Console.WriteLine("Generate VeraCode status reports for all projects.");
			Console.WriteLine("Syntax:");
			Console.WriteLine("vstat -p VeraCodeProfile -r ReportFilename -o FolderPath -i ignore-projects");
			Console.WriteLine("-p: Specifies the profile in the VeraCode credentials file to use to authenticate");
			Console.WriteLine("-r: Specifies the name of the report file, the filename will be appended with the current date");
			Console.WriteLine("-o: Generates html file with status results in the folder specified");
			Console.WriteLine("-t: Specifies the output type, html and csv are accepted");
			Console.WriteLine("-i: optional, command separated list of projects to ignore");
		}

		
	
		static void GenerateReport(string profile, string folder, string reportName, int maxProjects, 
			OutputFormat format, IEnumerable<string> ignoredProjects)
		{
			string reportFile = (format == OutputFormat.CSV ? GetCSVReportFilePath(folder, reportName) : GetHTMLReportFilePath(folder, reportName));
			if (!Utils.CheckFileIsWritable(reportFile))
			{
				throw new ApplicationException("Reportfile specified is not writable, it could be already in use.");
			}

			List<Application> apps = GetApplications(profile, maxProjects, ignoredProjects);
			
			if (format == OutputFormat.HTML)
			{				
				GenerateHTMLReport(apps, reportFile, maxProjects);
			}
			else
			{
				GenerateCSVReport(apps, reportFile, maxProjects);
			}

			Console.WriteLine("Report has been saved to {0}", reportFile);
		}

		static void GenerateHTMLReport(List<Application> apps, string reportFile, int maxProjects)
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

			WriteHTMLReport(reportFile, sb.ToString());			
		}

		static void GenerateCSVReport(List<Application> apps, string reportFile, int maxProjects)
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
			
			WriteCSVReport(reportFile, sb.ToString());			
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

		static string WriteHTMLReport(string reportFile, string content)
		{
			string template = File.ReadAllText(TEMPLATE_HTML_FILE);
			
			template = template.Replace(VAR_TITLE, string.Format("SurveilX VeraCode Status as on {0}",
				DateTime.Now.ToShortDateString()));			
			template = template.Replace(VAR_ROWS, content);			
			string filePath = reportFile;

			File.WriteAllText(filePath, template);						
			File.Copy(PASS_IMAGE, Path.Combine(Path.GetDirectoryName(filePath), PASS_IMAGE), true);
			File.Copy(FAIL_IMAGE, Path.Combine(Path.GetDirectoryName(filePath), FAIL_IMAGE), true);

			return filePath;
		}

		static void WriteCSVReport(string reportFile, string content)
		{		
			File.WriteAllText(reportFile, content);		
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
				Utils.WriteLineError("Veracode profile not specified!");				
				return;
			}

			string folder = cmd.GetFlagValue(FLAG_FOLDER, null);
			if (string.IsNullOrEmpty(folder))
			{
				Utils.WriteLineError("Output folder not specified!");
				return;
			}

			string outputType = cmd.GetFlagValue(FLAG_OUTPUT_TYPE, OutputFormat.CSV.ToString());
			OutputFormat format;

			if (!Enum.TryParse<OutputFormat>(outputType, true, out format))
            {
				Utils.WriteLineError("Invalid output format!");
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
				Utils.WriteLineError("Invalid output format!");
				return;
			}
						
			string[] ignoredProjectList = ParseIgnoredProjects(cmd.GetFlagValue(FLAG_IGNORED_PROJECTS, null));			
			string reportName = cmd.GetFlagValue(FLAG_REPORT_NAME, "veracode");
			int maxProjects = cmd.GetFlagValue<int>(FLAG_MAX_PROJECTS, int.MaxValue);

			try
			{
				GenerateReport(profile, folder, reportName, maxProjects, format, ignoredProjectList);
			}
			catch (Exception e)
			{				
				Utils.WriteLineError("Something went terribly wrong! Error: {0}", e.Message);				
			}
		}

		private static string[] ParseIgnoredProjects(string ignoredProjects)
		{
			if (ignoredProjects == null)
			{
				return null;
			}
			else
			{
				return ignoredProjects.Split(',');
			}			
		}

		private static readonly string FLAG_PROFILE = "p";
		private static readonly string FLAG_FOLDER = "o";
		private static readonly string FLAG_MAX_PROJECTS = "m";
		private static readonly string FLAG_REPORT_NAME = "r";
		private static readonly string FLAG_OUTPUT_TYPE = "t";
		private static readonly string FLAG_IGNORED_PROJECTS = "i";

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
