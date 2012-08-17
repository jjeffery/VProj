using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace VProj
{
	public class ProjectFile
	{
		public readonly string FilePath;
		public XmlDocument Document;
		public readonly XmlNamespaceManager NamespaceManager;

		public ProjectFile(string filePath)
		{
				FilePath = filePath;
				var nt = new NameTable();
				Document = new XmlDocument(nt);
				Document.Load(filePath);
				NamespaceManager = new XmlNamespaceManager(nt);
				NamespaceManager.AddNamespace("p", "http://schemas.microsoft.com/developer/msbuild/2003");
		}

		public string ProjectGuid
		{
			get { 
				var node = Document.SelectSingleNode("//p:ProjectGuid", NamespaceManager);
				if (node != null)
				{
					var text = node.InnerText;
					return text.Trim();
				}
				return string.Empty;
			}
			set
			{
				var node = Document.SelectSingleNode("//p:ProjectGuid", NamespaceManager);
				if (node != null)
				{
					var projectGuid = node.InnerText;
					node.InnerText = (value ?? string.Empty).Trim();
					Log.DebugFormat("Changed <ProjectGuid>{0}</ProjectGuid> to <ProjectGuid>{1}</ProjectGuid>",
					                projectGuid, value);
				}
				else
				{
					Log.Error("Cannot find <ProjectGuid> in " + FilePath);
				}
			}
		}

		public string Net35FilePath
		{
			get
			{
				return PathUtils.SuggestNewFilePath(FilePath);
			}
		}

		public void SaveTo(string filename)
		{
			Document.Save(filename);
		}

		public bool ShouldTransformToNet35()
		{
			return HasDefinedConstant("NET40") 
			       || HasDefinedConstant("NET40_CLIENT");
		}

		public void TransformToNet35()
		{
			using (CurrentDirectory.ChangeTo(Path.GetDirectoryName(FilePath)))
			{
				ChangeTargetFrameworkVersion("v3.5");
				ChangeTargetFrameworkProfile(string.Empty);
				ChangeOutputPath(".Net35");
				ChangeHintPaths(new[] {"net45", "net45-client", "net40", "net40-client"},
				                new[] {"net35", "net30", "net20"});
				RemoveReferenceAssembly("Microsoft.CSharp");
				ChangeDefineConstants(new[] {"NET45", "NET40", "NET40_CLIENT"}, new[] {"NET35"});
			}
		}

		public void ChangeProjectReference(SolutionProject project)
		{
			var xpath1 = string.Format("//p:ProjectReference[p:Project='{0}']", project.ProjectGuid);
			var nodes = Document.SelectNodes(xpath1, NamespaceManager);
			if (nodes != null)
			{
				foreach (XmlElement element in nodes)
				{
					ChangeProjectReference(element, project);
				}
			}

			// Sometimes the guids are wonky, so try the project name as well
			var xpath2 = string.Format("//p:ProjectReference[p:Name='{0}']", project.ProjectName);
			nodes = Document.SelectNodes(xpath2, NamespaceManager);
			if (nodes != null)
			{
				foreach (XmlElement element in nodes)
				{
					ChangeProjectReference(element, project);
				}
			}
		}

		private void ChangeProjectReference(XmlElement element, SolutionProject project)
		{
			var include = element.GetAttribute("Include");
			var projectElement = element.SelectSingleNode("p:Project", NamespaceManager);
			var nameElement = element.SelectSingleNode("p:Name", NamespaceManager);

			// TODO: check for null and log error
			projectElement.InnerText = project.NewProjectGuid;
			nameElement.InnerText = project.NewProjectName;

			var fileName = Path.GetFileName(project.NewRelativePath);
			var directoryName = Path.GetDirectoryName(include) ?? ".";
			element.SetAttribute("Include", Path.Combine(directoryName, fileName));

			Log.DebugFormat("Change project reference from {0} to {1}", project.ProjectName, project.NewProjectName);
		}

		private void ChangeTargetFrameworkVersion(string targetFrameworkVersion)
		{
			var nodes = Document.SelectNodes("//p:TargetFrameworkVersion", NamespaceManager);
			if (nodes != null)
			{
				foreach (XmlElement element in nodes)
				{
					var version = element.InnerText;
					element.InnerText = targetFrameworkVersion;
					Log.DebugFormat(
						"Change <TargetFrameworkVersion>{0}</TargetFrameworkVersion> to <TargetFrameworkVersion>{1}</TargetFrameworkVersion>",
						version, targetFrameworkVersion);
				}
			}
		}

		private void ChangeTargetFrameworkProfile(string targetFrameworkProfile)
		{
			var nodes = Document.SelectNodes("//p:TargetFrameworkProfile", NamespaceManager);
			if (nodes != null)
			{
				foreach (XmlElement element in nodes)
				{
					var oldTargetFrameworkProfile = element.InnerText;
					if (oldTargetFrameworkProfile != targetFrameworkProfile)
					{
						element.InnerText = targetFrameworkProfile;
						Log.DebugFormat(
							"Change <TargetFrameworkProfile>{0}</TargetFrameworkProfile> to <TargetFrameworkProfile>{1}</TargetFrameworkProfile>",
							oldTargetFrameworkProfile, targetFrameworkProfile);
					}
				}
			}
		}


		private void ChangeOutputPath(string suffix)
		{
			var nodes = Document.SelectNodes("//p:OutputPath", NamespaceManager);
			if (nodes != null)
			{
				foreach (XmlElement element in nodes)
				{
					// get the original text and strip off any trailing path separator char
					var outputPath = element.InnerText;
					var newOutputPath = outputPath;
					while (newOutputPath[newOutputPath.Length - 1] == Path.DirectorySeparatorChar)
					{
						newOutputPath= newOutputPath.Substring(0, newOutputPath.Length - 1);
					}
					newOutputPath += suffix;
					newOutputPath += Path.DirectorySeparatorChar;
					element.InnerText = newOutputPath;
					Log.DebugFormat("Change <OutputPath>{0}</OutputPath> with <OutputPath>{1}</HintPath>",
					                outputPath, newOutputPath);
				}
			}
		}

		private void ChangeHintPaths(string[] unacceptableVersions, string[] acceptableVersions)
		{
			var nodes = Document.SelectNodes("//p:Reference/p:HintPath", NamespaceManager);
			if (nodes != null)
			{
				foreach (XmlElement element in nodes)
				{
					foreach (var acceptableVersion in acceptableVersions)
					{
						var hintPath = element.InnerText;
						var parts = hintPath.Split(Path.DirectorySeparatorChar);
						var newParts = new List<string>();

						foreach (var part in parts)
						{
							var substituted = false;

							foreach (var unacceptableVersion in unacceptableVersions)
							{
								if (part.Equals(unacceptableVersion, StringComparison.CurrentCultureIgnoreCase))
								{
									newParts.Add(acceptableVersion);
									substituted = true;
									break;
								} 
							}

							if (!substituted)
							{
								newParts.Add(part);
							}
						}

						var newHintPath = string.Join(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture), newParts);
						if (File.Exists(newHintPath))
						{
							element.InnerText = newHintPath;
							Log.DebugFormat("Change <HintPath>{0}</HintPath> to <HintPath>{1}</HintPath>",
							                hintPath, newHintPath);
							break;
						}
					}
				}
			}
		}

		private void RemoveReferenceAssembly(string assemblyName)
		{
			var nodes = Document.SelectNodes("//p:Reference", NamespaceManager);
			if (nodes != null)
			{
				foreach (XmlElement element in nodes)
				{
					var includeAttribute = element.GetAttribute("Include");
					if (includeAttribute != null && includeAttribute.Equals(assemblyName, StringComparison.InvariantCultureIgnoreCase))
					{
						var outerXml = element.OuterXml;
						var parent = (XmlElement) element.ParentNode;
						if (parent != null)
						{
							parent.RemoveChild(element);
							Log.Debug("Removed " + outerXml);
						}
					}
				}
			}
		}

		public bool HasDefinedConstant(string constant)
		{
			var nodes = Document.SelectNodes("//p:DefineConstants", NamespaceManager);
			if (nodes == null)
			{
				return false;
			}

			foreach (XmlElement element in nodes)
			{
				var defineConstants = element.InnerText;
				var array = defineConstants.Split(';');
				var c = Array.Find(array, s => s == constant);
				if (c == null)
				{
					return false;
				}
			}
			return true;
		}

		private void ChangeDefineConstants(string[] removeConstants, string[] addConstants)
		{
			var nodes = Document.SelectNodes("//p:DefineConstants", NamespaceManager);
			if (nodes != null)
			{
				foreach (XmlElement element in nodes)
				{
					var defineConstants = element.InnerText;
					var array = defineConstants.Split(';');
					var list = new List<string>();
					foreach (var c in array)
					{
						bool remove = false;

						var stripped = c.Trim();
						if (stripped.Length == 0)
						{
							remove = true;
						}
						else
						{
							foreach (var removeConstant in removeConstants)
							{
								if (stripped == removeConstant)
								{
									remove = true;
									break;
								}
							}
						}

						if (!remove)
						{
							list.Add(stripped);
						}
					}

					foreach (var addConstant in addConstants)
					{
						if (!list.Contains(addConstant))
						{
							list.Add(addConstant);
						}
					}

					var newDefineConstants = string.Join(";", list);
					if (newDefineConstants != defineConstants)
					{
						Log.DebugFormat("Change <DefineConstants>{0}</DefineConstants> to <DefineConstants>{1}</DefineConstants>",
						                defineConstants, newDefineConstants);
						element.InnerText = newDefineConstants;
					}
				}
			}
		}
	}
}
