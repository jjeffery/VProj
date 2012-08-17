using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace VProj
{
	public class SolutionFile
	{
		private static readonly Regex ProjectLineRegex = new Regex("^\\s*Project\\(\"(?<ProjectTypeGuid>.*)\"\\)"
		                                                           + "\\s*=\\s*\"(?<ProjectName>.*)\"\\s*,"
		                                                           + "\\s*\"(?<RelativePath>.*)\"\\s*,"
		                                                           + "\\s*\"(?<ProjectGuid>.*)\"\\s*$");
		private static readonly Regex EndProjectLineRegex = new Regex(@"^\s*EndProject\s*$");

		private const string CsProjectGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
		private const string SolutionFolderGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

		private readonly string _solutionFile;
		private int _currentLineNumber;
		private TextReader _reader;
		private TextWriter _writer;

		public readonly List<SolutionProject> Projects = new List<SolutionProject>();

		private readonly Dictionary<string, SolutionProject> _projectsByProjectGuid =
			new Dictionary<string, SolutionProject>();

		public static bool IsSolutionFileName(string fileName)
		{
			return (Path.GetExtension(fileName) ?? string.Empty).Equals(".sln", StringComparison.OrdinalIgnoreCase);
		}

		public SolutionFile(string solutionFile)
		{
			_solutionFile = solutionFile;
			ParseProjects();
		}

		public void SaveTo(string outputFile)
		{
			try
			{
				using (var inFileStream = File.OpenRead(_solutionFile))
				{
					using (_reader = new StreamReader(inFileStream))
					{
						_currentLineNumber = 0;
						using (var outFileStream = File.OpenWrite(outputFile))
						{
							using (_writer = new StreamWriter(outFileStream))
							{
								ParseFileHeader();
								CopyLines();
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				File.Delete(outputFile);
				throw;
			}
		}

		private void ParseProjects()
		{
			Projects.Clear();
			_projectsByProjectGuid.Clear();
			using (var fileStream = File.OpenRead(_solutionFile))
			{
				using (_reader = new StreamReader(fileStream))
				{
					_currentLineNumber = 0;
					ParseFileHeader();
					string line;
					while ((line = ReadLine()) != null)
					{
						var match = ProjectLineRegex.Match(line);
						if (match.Success)
						{
							// found a project line
							var proj = new SolutionProject {
							                               	ProjectTypeGuid = match.Groups["ProjectTypeGuid"].Value.Trim(),
							                               	ProjectName = match.Groups["ProjectName"].Value.Trim(),
							                               	RelativePath = match.Groups["RelativePath"].Value.Trim(),
							                               	ProjectGuid = match.Groups["ProjectGuid"].Value.Trim()
							                               };

							if (proj.ProjectTypeGuid == CsProjectGuid)
							{
								proj.ProjectType = ProjectType.CSharp;
								proj.NewProjectName = proj.ProjectName + ".Net35";
								proj.NewRelativePath = CreateNewRelativePath(proj.RelativePath);
								proj.NewProjectGuid = CreateNewProjectGuid();
							}
							else if (proj.ProjectTypeGuid == SolutionFolderGuid)
							{
								proj.ProjectType = ProjectType.SolutionFolder;
								proj.NewProjectName = proj.ProjectName;
								proj.NewRelativePath = proj.RelativePath;
								proj.NewProjectGuid = CreateNewProjectGuid();
							}
							else
							{
								proj.ProjectType = ProjectType.Unknown;
							}

							Projects.Add(proj);
							_projectsByProjectGuid.Add(proj.ProjectGuid, proj);
						}
					}
				}
			}
		}

		private void CopyLines()
		{
			string line;
			while ((line = ReadLine()) != null)
			{
				var match = ProjectLineRegex.Match(line);
				if (match.Success)
				{
					// this is a project line, get the project guid
					var projectGuid = match.Groups["ProjectGuid"].Value.Trim();
					var project = _projectsByProjectGuid[projectGuid];

					if (project.ShouldTransform)
					{


						if (project.NewProjectGuid != null)
						{
							// we are going to adjust this line, first output the required
							// number of leading spaces
							_writer.Write(GetLeadingSpaces(line));
							_writer.WriteLine("Project(\"{0}\") = \"{1}\", \"{2}\", \"{3}\"",
							                  project.ProjectTypeGuid,
							                  project.NewProjectName,
							                  project.NewRelativePath,
							                  project.NewProjectGuid);
						}
						else
						{
							// this is  project we are leaving alone, so just echo it
							_writer.WriteLine(line);
						}
					}
					else
					{
						// We do not want to include this project in the solution.
						// Gobble up lines up to and including the end project line
						while ((line = ReadLine()) != null)
						{
							if (EndProjectLineRegex.IsMatch(line))
							{
								break;
							}
						}
					}
				}
				else
				{
					bool shouldCopy = true;
					foreach (var project in Projects)
					{
						if (!project.ShouldTransform)
						{
							if (line.IndexOf(project.ProjectGuid, StringComparison.Ordinal) >= 0)
							{
								// This line contains a project guid of a project we are not including.
								// Just exclude from the output.
								shouldCopy = false;
								break;
							}
						}
					}

					if (shouldCopy)
					{
						// This is not a project line, so replace any of the project guids with its 
						// new project guid.
						foreach (var project in Projects)
						{
							if (project.ShouldTransform && project.NewProjectGuid != null)
							{
								line = line.Replace(project.ProjectGuid, project.NewProjectGuid);
							}
						}

						_writer.WriteLine(line);
					}
				}
			}
		}

		// Return leading white spaces for a string.
		private string GetLeadingSpaces(string line)
		{
			var sb = new StringBuilder();
			foreach (var ch in line)
			{
				if (!Char.IsWhiteSpace(ch))
				{
					break;
				}
				sb.Append(ch);
			}
			return sb.ToString();
		}


		private string ReadLine()
		{
			var line = _reader.ReadLine();
			++_currentLineNumber;
			return line;
		}

		private void ParseFileHeader()
		{
			const string expectedText = "Microsoft Visual Studio Solution File, Format Version ";
			var line = ReadLine();
			if (line != null && line.StartsWith(expectedText))
			{
				ValidateSolutionFileVersion(line.Substring(expectedText.Length));
				if (_writer != null)
				{
					_writer.WriteLine(line);
				}
			}
			else
			{
				throw new InvalidProjectFileException("Invalid solution file format");
			}
		}

		private void ValidateSolutionFileVersion(string versionText)
		{
			Version version;
			if (!Version.TryParse(versionText, out version))
			{
				throw new InvalidProjectFileException("Invalid solution file version: " + versionText);
			}

			if (version.Major > 12)
			{
				throw new InvalidProjectFileException("Unrecognised solution version: " + versionText);
			}
		}

		private string CreateNewRelativePath(string relativePath)
		{
			return PathUtils.SuggestNewFilePath(relativePath);
		}

		private string CreateNewProjectGuid()
		{
			return "{" + Guid.NewGuid().ToString().ToUpperInvariant() + "}";
		}
	}
}