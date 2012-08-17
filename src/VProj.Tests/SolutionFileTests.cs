using System;
using System.Linq;
using NUnit.Framework;

// ReSharper disable InconsistentNaming
namespace VProj.Tests
{
	[TestFixture]
	public class SolutionFileTests
	{
		private SolutionFile _solutionFile;
		private string _solutionFilePath;

		[SetUp]
		public void SetUp()
		{
			ResourceHelper.Cleanup();
			_solutionFilePath = ResourceHelper.ExtractFile("Sample.sln");
			_solutionFile = new SolutionFile(_solutionFilePath);
		}

		[TearDown]
		public void TearDown()
		{
			ResourceHelper.Cleanup();
		}

		[Test]
		public void Loads_all_projects()
		{
			Assert.AreEqual(18, _solutionFile.Projects.Count);
		}

		[Test]
		public void Transforms_project()
		{
			var requiredProject = _solutionFile.Projects.Find(p => p.ProjectName == "Sample.Core");
			Assert.IsNotNull(requiredProject);
			requiredProject.ShouldTransform = true;

			var convertedFilePath = PathUtils.SuggestNewFilePath(_solutionFilePath);
			_solutionFile.SaveTo(convertedFilePath);

			var convertedSolutionFile = new SolutionFile(convertedFilePath);
			var convertedProjects = convertedSolutionFile.Projects.FindAll(p => p.ProjectName.EndsWith(".Net35"));
			Assert.AreEqual(1, convertedProjects.Count);
			var convertedProject = convertedProjects.First();
			Assert.AreEqual("Sample.Core.Net35", convertedProject.ProjectName);
			Assert.AreEqual(PathUtils.SuggestNewFilePath(requiredProject.RelativePath), convertedProject.RelativePath);
			Assert.AreEqual(requiredProject.ProjectTypeGuid, convertedProject.ProjectTypeGuid);
			Assert.AreNotEqual(requiredProject.ProjectGuid, convertedProject.ProjectGuid);
		}
	}
}
