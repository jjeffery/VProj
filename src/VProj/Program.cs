using System;
using System.IO;

namespace VProj
{
	class Program
	{
		static void Main(string[] args)
		{
			var commandLine = new CommandLine(args);
			if (commandLine.Help)
			{
				commandLine.ShowHelp(Console.Out);
				Environment.ExitCode = 1;
				return;
			}
			if (commandLine.HasErrors)
			{
				commandLine.ShowErrors(Console.Error);
				Environment.ExitCode = 1;
				return;
			}
			try
			{
				if (SolutionFile.IsSolutionFileName(commandLine.ProjectFile))
				{
					ProcessSolutionFile(commandLine);
				}
				else
				{
					ProcessProjectFile(commandLine);
				}
				Log.Info("Done");
			}
			catch (Exception ex)
			{
				Log.Error("Unexpected exception: " + ex.Message);
				Environment.ExitCode = 1;
			}
		}

		private static void ProcessSolutionFile(CommandLine commandLine)
		{
			if (commandLine.Clean)
			{
				Log.Info("Removing .NET 3.5 VS solutions and project files ");
			}
			else
			{
				Log.Info("Converting VS solution and projects to target .NET 3.5");
			}
			if (commandLine.NoChanges)
			{
				Log.Info("PREVIEW MODE: NO CHANGES WILL BE MADE");
			}
			var solutionFile = new SolutionFile(commandLine.ProjectFile);
			using (CurrentDirectory.ChangeTo(Path.GetDirectoryName(commandLine.ProjectFile)))
			{
				foreach (var project in solutionFile.Projects)
				{
					if (project.ProjectType == ProjectType.SolutionFolder)
					{
						project.ShouldTransform = true;
					}
					else if (project.ProjectType == ProjectType.CSharp)
					{
						var projectFile = new ProjectFile(project.RelativePath);
						if (projectFile.ShouldTransformToNet35())
						{
							project.ShouldTransform = true;
						}
						else
						{
							project.ShouldTransform = false;
						}
					}
				}

				foreach (var project in solutionFile.Projects)
				{
					if (project.ShouldTransform && project.ProjectType == ProjectType.CSharp)
					{
						var projectFile = new ProjectFile(project.RelativePath);
						projectFile.ProjectGuid = project.NewProjectGuid;
						projectFile.TransformToNet35();
						foreach (var referenceProject in solutionFile.Projects)
						{
							if (referenceProject == project
								|| referenceProject.ProjectType != ProjectType.CSharp
								|| !referenceProject.ShouldTransform)
							{
								continue;
							}

							projectFile.ChangeProjectReference(referenceProject);
						}
						if (commandLine.Clean)
						{
							PromptToRemoveFile(projectFile.Net35FilePath);
						}
						else
						{
							Log.InfoFormat("{0} -> {1}", projectFile.FilePath, projectFile.Net35FilePath);
							if (!commandLine.NoChanges)
							{
								projectFile.SaveTo(projectFile.Net35FilePath);
							}
						}
					}
				}
			}
			if (commandLine.Clean)
			{
				PromptToRemoveFile(commandLine.OutputFile);
			}
			else
			{
				Log.InfoFormat("{0} -> {1}", commandLine.ProjectFile, commandLine.OutputFile);
				if (!commandLine.NoChanges)
				{
					solutionFile.SaveTo(commandLine.OutputFile);
				}
			}
		}

		private static void ProcessProjectFile(CommandLine commandLine)
		{
			Log.Info("Converting VS project to target .NET 3.5");
			Log.DebugFormat("Will convert {0} to {1}", commandLine.ProjectFile, commandLine.OutputFile);
			var projectFile = new ProjectFile(commandLine.ProjectFile);
			projectFile.TransformToNet35();
			Log.InfoFormat("{0} -> {1}", commandLine.ProjectFile, commandLine.OutputFile);
			projectFile.SaveTo(commandLine.OutputFile);
		}

		private static void PromptToRemoveFile(string filePath)
		{
			if (CommandLine.Instance.NoChanges)
			{
				Log.InfoFormat("Remove \"{0}", filePath);
				return;
			}

			if (CommandLine.Instance.Quiet || CommandLine.Instance.NoPrompt)
			{
				File.Delete(filePath);
				Log.InfoFormat("Removed \"{0}\"", filePath);
			}
			else
			{
				for (; ; )
				{
					Console.Write("Remove \"{0}\" (y|n)? ", filePath);
					var response = (Console.ReadLine() ?? string.Empty).ToLowerInvariant();
					if (response == "y" || response == "yes")
					{
						File.Delete(filePath);
						return;
					}
					if (response == "n" || response == "no")
					{
						return;
					}
				}
			}
		}
	}
}
