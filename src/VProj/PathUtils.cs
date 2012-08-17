using System;
using System.IO;

namespace VProj
{
	public static class PathUtils
	{
		/// <summary>
		/// Suggest a new name for the file
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		public static string SuggestNewFilePath(string filePath)
		{
			// TODO: get this from the command line if we ever target other frameworks.
			const string targetFrameworkText = ".Net35";
			var directory = Path.GetDirectoryName(filePath) ?? ".";
			var baseName = Path.GetFileNameWithoutExtension(filePath);
			var extension = Path.GetExtension(filePath);
			var suggestedFileName = baseName + targetFrameworkText + extension;
			return Path.Combine(directory, suggestedFileName);
		}
	}
}