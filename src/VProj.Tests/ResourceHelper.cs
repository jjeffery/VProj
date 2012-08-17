using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace VProj.Tests
{
	// Helper functions for extracting embedded resource files into a temporary directory for testing.
	internal static class ResourceHelper
	{
		public static readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "VProj.Tests");
		private static readonly Assembly ThisAssembly = typeof (ResourceHelper).Assembly;
		private static readonly Type ThisType = typeof (ResourceHelper);

		public static void Cleanup()
		{
			if (Directory.Exists(TempDirectory))
			{
				Directory.Delete(TempDirectory, recursive: true);
			}
		}

		public static string ExtractFile(string fileName)
		{
			var resourceName = fileName + ".txt";
			var extractFile = Path.Combine(TempDirectory, fileName);
			Directory.CreateDirectory(TempDirectory);
			using (var outStream = File.OpenWrite(extractFile))
			{
				using (var inStream = ThisAssembly.GetManifestResourceStream(ThisType, resourceName))
				{
					Assert.IsNotNull(inStream, "Cannot find embedded resource: " + resourceName);
					inStream.CopyTo(outStream);
				}
			}
			return extractFile;
		}
	}
}