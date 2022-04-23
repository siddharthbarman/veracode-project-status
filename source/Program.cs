using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

			foreach (string appId in appIds)
			{
				Application app = gen.GetApplicationInfo(appId);
				apps.Add(app);				
				count++;				

				Build build = gen.GetBuildInfo(app.Id);
				if (build != null)
				{
					app.Build = build;
				}

				int percent = count * 100 / appIds.Count;
				Console.CursorLeft = left;
				Console.Write(string.Format("{0}%", percent));

				if (count > maxProjects)
				{
					break;
				}
			}

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
		}
	
		static void GenerateReport(string profile, string folder, string reportName, int maxProjects)
		{
			List<Application> apps = GetApplications(profile, maxProjects);
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

			string reportFile = WriteReport(folder, reportName, sb.ToString());
			Console.WriteLine("Report has been saved to {0}", reportFile);
		}

		static string GetReportFilePath(string folder, string reportName)
		{
			string filename = string.Format("{0}-{1}.html", 
				reportName,
				DateTime.Now.ToShortDateString().Replace("/", "-"));
			return Path.Combine(folder, filename);			
		}

		static string WriteReport(string folder, string reportName, string content)
		{
			string template = File.ReadAllText(TEMPLATE_HTML_FILE);
			template = template.Replace(VAR_TITLE, string.Format("SurveilX VeraCode Status as on {0}",
				DateTime.Now.ToShortDateString()));
			template = template.Replace(VAR_ROWS, content);
			
			string filePath = GetReportFilePath(folder, reportName);
			File.WriteAllText(filePath, template);			
			
			File.Copy(PASS_IMAGE, Path.Combine(folder, PASS_IMAGE), true);
			File.Copy(FAIL_IMAGE, Path.Combine(folder, FAIL_IMAGE), true);

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

			string reportName = cmd.GetFlagValue(FLAG_REPORT_NAME, "veracode");
			int maxProjects = cmd.GetFlagValue<int>(FLAG_MAX_PROJECTS, int.MaxValue);

			GenerateReport(profile, folder, reportName, maxProjects);
		}

		private static readonly string FLAG_PROFILE = "p";
		private static readonly string FLAG_FOLDER = "o";
		private static readonly string FLAG_MAX_PROJECTS = "m";
		private static readonly string FLAG_REPORT_NAME = "r";

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
