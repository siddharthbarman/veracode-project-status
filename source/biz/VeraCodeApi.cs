using System;
using System.Collections.Generic;
using System.Text;
using com.veracode.apiwrapper;
using com.veracode.apiwrapper.services;
using vstat.Models;

namespace vstat.biz
{
	public class VeraCodeApi : VeraCodeParser
	{
		public VeraCodeApi(string profile)
		{
			this.profile = profile;
		}

		public List<string> GetApplicationIds()
		{			
			string resultXml = UploadApi.GetAppList(true.ToString());			
			return ParseApplicationResultsXml(resultXml);
		}

		public Application GetApplicationInfo(string appId)
		{
			string xml = UploadApi.GetAppInfo(appId);
			return ParseApplicationInfoXml(xml);
		}

		public Build GetBuildInfo(string appId)
		{
			string xml = UploadApi.GetBuildInfo(appId);
			Build build = ParseBuildInfoXml(xml);
			return build;
		}

		public void GetBuilds()
		{
			string xml = UploadApi.GetAppInfo("1285246");
			xml = SandboxApi.GetSandboxList("1285246");

			xml = UploadApi.GetBuildList("1285246", "4056647"); 

			xml = ResultsApi.SummaryReport("17731503");
			xml = ResultsApi.GetAppBuilds();			
			xml = UploadApi.GetAppInfo("1285246");
			xml = ResultsApi.SummaryReport("1285246");
			
		}

		protected IUploadApiWrapper UploadApi
		{
			get
			{
				if (uploadApiWrapper == null)
				{
					ICredentialsService cred = com.veracode.apiwrapper.services.impl.DefaultCredentialsService.CreateInstance();
					uploadApiWrapper = new UploadAPIWrapper();
					uploadApiWrapper.SetUpApiCredentials(profile);
				}
				return uploadApiWrapper;
			}			
		}

		protected IResultsApiWrapper ResultsApi
		{
			get
			{
				if (resultsApiWrapper == null)
				{
					ICredentialsService cred = com.veracode.apiwrapper.services.impl.DefaultCredentialsService.CreateInstance();
					resultsApiWrapper = new ResultsAPIWrapper();
					resultsApiWrapper.SetUpApiCredentials(profile);					
				}
				return resultsApiWrapper;
			}
		}

		protected ISandboxApiWrapper SandboxApi
		{
			get
			{
				if (sandboxApiWrapper == null)
				{
					ICredentialsService cred = com.veracode.apiwrapper.services.impl.DefaultCredentialsService.CreateInstance();
					sandboxApiWrapper = new SandboxAPIWrapper();
					sandboxApiWrapper.SetUpApiCredentials(profile);
				}
				return sandboxApiWrapper;
			}
		}

		private string profile;
		private UploadAPIWrapper uploadApiWrapper;
		private ResultsAPIWrapper resultsApiWrapper;
		private SandboxAPIWrapper sandboxApiWrapper;
	}
}
