using System;
using NUnit.Framework;

// ReSharper disable InconsistentNaming
namespace VProj.Tests
{
	[TestFixture]
	public class PathUtilsTests
	{
		[Test]
		public void Converts_with_directory()
		{
			const string original = @"C:\Directory\Directory2\File.sln";
			const string expected = @"C:\Directory\Directory2\File.Net35.sln";
			var actual = PathUtils.SuggestNewFilePath(original);
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void No_directory_name()
		{
			const string original = @"File.sln";
			const string expected = @"File.Net35.sln";
			var actual = PathUtils.SuggestNewFilePath(original);
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void No_extension()
		{
			const string original = @"..\..\bin\Debug";
			const string expected = @"..\..\bin\Debug.Net35";
			var actual = PathUtils.SuggestNewFilePath(original);
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void Unc_path_name()
		{
			const string original = @"\\Hostname\Directory\Directory2\File.sln";
			const string expected = @"\\Hostname\Directory\Directory2\File.Net35.sln";
			var actual = PathUtils.SuggestNewFilePath(original);
			Assert.AreEqual(expected, actual);
		}
	}
}
