using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace River.Test.PatchProject
{
	class Program
	{
		static void Main(string[] args)
		{
			var dir = Directory.GetCurrentDirectory();
			Console.WriteLine("CD:\r\n" + dir);

			while (Directory.GetFiles(dir, "*.sln").Length == 0)
			{
				dir = Path.GetDirectoryName(dir);
			}

			Console.WriteLine("Sln Folder:\r\n" + dir);

			var projects = Directory.GetFiles(dir, "*.csproj", SearchOption.AllDirectories);
			foreach (var file in projects)
			{
				Patch(file);
			}
		}

		private static void Patch(string file)
		{
			Console.WriteLine(file);
			restart:

			// var ns = new NameTable();
			// ns.Add("http://schemas.microsoft.com/developer/msbuild/2003");
			// var nm = new XmlNamespaceManager(ns);

			var doc = new XmlDocument();
			doc.LoadXml(File.ReadAllText(file));

			var nm = new XmlNamespaceManager(doc.NameTable);
			var msb = "http://schemas.microsoft.com/developer/msbuild/2003";
			nm.AddNamespace("x", msb);

			var nodeOldStyle = doc.SelectSingleNode("/x:Project", nm);
			var nodeNewStyle = doc.SelectSingleNode("/Project[@Sdk='Microsoft.NET.Sdk']", nm);

			if (nodeOldStyle != null)
			{
				Console.WriteLine("OLD");
				if (doc.SelectSingleNode("/x:Project/x:PropertyGroup/x:LangVersion", nm) == null)
				{
					int i = 0;
					if (nodeOldStyle.Cast<XmlElement>().Skip(1).First().Name == "Import")
					{
						// This is PropertyGroup before import!
						i++;
					}
					if (nodeOldStyle.Cast<XmlElement>().Skip(i).First().Name == "Import")
					{
						i++;
						// throw new NotSupportedException("2nd element must be import");
					}
					var pg = nodeOldStyle.Cast<XmlElement>().Skip(i).First();
					if (pg.Name != "PropertyGroup")
					{
						throw new NotSupportedException($"{i}th element must be PropertyGroup");
					}

					if (pg.SelectSingleNode("x:LangVersion", nm) == null)
					{
						var lv = doc.CreateNode(XmlNodeType.Element, "LangVersion", msb);
						lv.InnerText = "latest";
						pg.AppendChild(lv);
						doc.Save(file);
					}
				}
			}
			else if (nodeNewStyle != null)
			{
				Console.WriteLine("NEW");
				if (doc.SelectSingleNode("/Project/PropertyGroup/LangVersion") == null)
				{
					var lv = doc.CreateNode(XmlNodeType.Element, "LangVersion", "");
					lv.InnerText = "latest";
					// var lvt = doc.CreateNode(XmlNodeType.Text, "LangVersion", "");
					doc.SelectSingleNode("/Project/PropertyGroup").AppendChild(lv);
					doc.Save(file);
					// Console.WriteLine(xml);
				}
			}
			else
			{
				Console.WriteLine("UNKNOWN");
			}
		}
	}
}
