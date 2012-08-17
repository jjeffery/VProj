using System;

namespace VProj
{
	public class SolutionProject
	{
		public string ProjectName;
		public string RelativePath;
		public string ProjectGuid;
		public string NewProjectName;
		public string NewRelativePath;
		public string NewProjectGuid;
		public string ProjectTypeGuid;
		public ProjectType ProjectType;
		public bool ShouldTransform;

		public override string ToString()
		{
			return ProjectName ?? "No Project";
		}
	}

	public enum ProjectType
	{
		Unknown,
		SolutionFolder,
		CSharp,
	}
}