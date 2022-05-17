using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using vstat.Models;

namespace vstat.biz
{
	public class VeraCodeParser
	{	
		public List<ApplicationInfo> ParseApplicationResultsXml(string xml)
		{
			XmlDocument doc = GetXmlDocument(xml);
			List<ApplicationInfo> result = new List<ApplicationInfo>();
			foreach (XmlNode node in SelectNodes(doc, "//a:applist/a:app"))
			{				
				result.Add(new ApplicationInfo(node.Attributes["app_id"].Value, node.Attributes["app_name"].Value));
			}
			return result;
		}

		public Application ParseApplicationInfoXml(string xml)
		{
			XmlDocument doc = GetXmlDocument(xml);			 
			XmlNode node = SelectSingleNode(doc, "//a:appinfo/a:application");
			Application result = new Application
			{
				BusinessOwnerEmail = GetAttributeValue(node, "business_owner_email", string.Empty),
				BusinessUnit = GetAttributeValue(node, "business_unit", string.Empty),
				Id = GetAttributeValue(node, "app_id", string.Empty),
				LastUpdatedOn = GetAttributeValue<DateTime>(node, "modified_date", DateTime.MinValue),
				Name = GetAttributeValue(node, "app_name", string.Empty),
				Policy = GetAttributeValue(node, "policy", string.Empty),
				Teams = GetAttributeValue(node, "teams", string.Empty)
			};
			return result;
		}

		public Build ParseBuildInfoXml(string xml)
		{
			XmlDocument doc = GetXmlDocument(xml);
			
			if (!IsDataAvailable(doc))
			{
				return null;
			}

			XmlNode node = SelectSingleNode(doc, "//a:buildinfo/a:build");

			if (node == null)
			{
				return null;
			}
			
			Build result = new Build
			{				
				PolicyComplianceStatus = GetAttributeValue(node, "policy_compliance_status", string.Empty),
				PolicyName = GetAttributeValue(node, "policy_name", string.Empty),
				Submitter = GetAttributeValue(node, "submitter", string.Empty),
				Version = GetAttributeValue(node, "version", string.Empty),
			};

			return result;
		}

		protected XmlDocument GetXmlDocument(string xml)
		{
			XmlDocument doc = new XmlDocument();			
			doc.LoadXml(xml);			
			return doc;
		}

		protected XmlNodeList SelectNodes(XmlDocument doc, string xPath)
		{
			XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
			XmlAttribute nsAtt = doc.ChildNodes[1].Attributes["xmlns"];
			if (nsAtt != null)
			{
				nsmgr.AddNamespace("a", nsAtt.Value);
			}
			XmlNodeList result = doc.SelectNodes(xPath, nsmgr);
			return result;
		}

		protected XmlNode SelectSingleNode(XmlDocument doc, string xPath)
		{
			XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
			XmlAttribute nsAtt = doc.ChildNodes[1].Attributes["xmlns"];
			if (nsAtt != null)
			{
				nsmgr.AddNamespace("a", nsAtt.Value);
			}
			return doc.SelectSingleNode(xPath, nsmgr);
		}

		protected T GetAttributeValue<T>(XmlNode node, string attribute, T defaultValue)
		{
			XmlAttribute att = node.Attributes[attribute];			
			if (att == null)
			{
				return defaultValue;
			}
			else
			{
				return (T)Convert.ChangeType(att.Value, typeof(T));
			}
		}

		protected bool IsDataAvailable(XmlDocument doc)
		{
			return doc.SelectSingleNode("error") == null;
		}
		
	}
}
