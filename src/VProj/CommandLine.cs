using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NDesk.Options;

namespace VProj
{
	public class CommandLine
	{
		public static CommandLine Instance { get; private set; }

		public string ProgramName { get; private set; }
		public bool Help { get; private set; }
		public bool NoChanges { get; private set; }
		public bool Clean { get; private set; }
		public bool NoPrompt { get; private set; }
		public bool Verbose { get; private set; }
		public bool Quiet { get; private set; }
		public string ProjectFile { get; private set; }
		public string OutputFile { get; private set; }

		public bool HasErrors { get { return _errors.Count > 0; }}

		private readonly List<string> _errors = new List<string>();
		private readonly OptionSet _optionSet;

		public CommandLine(IEnumerable<string> args)
		{
			if (args == null)
			{
				throw new ArgumentNullException("args");
			}

			ProgramName = Assembly.GetEntryAssembly().GetName().Name;

			_optionSet = new OptionSet {
			                              	{"p|project=", "* Project file", v => ProjectFile = v},
			                              	{"o|output=", "  Output file", v => OutputFile = v},
											{"n|nochange", "  Show proposed changes, but do not write files", v => NoChanges = v != null},
											{"clean", "  Remove files created from previous operation", v => Clean = v != null},
											{"noprompt", "  Do not prompt to remove files, just do it", v => NoPrompt = v != null},
											{"v|verbose", "  Log additional debug messages", v => Verbose = v != null},
											{"q|quiet", "  Quiet operation, log errors and warnings only", v => Quiet = v != null},
											{"?|help", "  Show this help message", v => Help = v != null }
			                              };
			var extras = _optionSet.Parse(args);
			Validate(extras);
			Instance = this;
		}

		private void Validate(IEnumerable<string> extras)
		{
			foreach (var extra in extras)
			{
				_errors.Add("Unexpected argument: " + extra);
			}

			if (ProjectFile == null)
			{
				_errors.Add("Please specify a project file");
			}
			else
			{
				if (File.Exists(ProjectFile))
				{
					if (OutputFile == null)
					{
						OutputFile = PathUtils.SuggestNewFilePath(ProjectFile);
					}
				}
				else
				{
					_errors.Add("Cannot find project file: " + ProjectFile);
				}
			}
		}

		public void ShowErrors(TextWriter writer)
		{
			if (_errors.Count > 0)
			{
				writer.WriteLine("{0}: {1}", ProgramName, _errors[0]);
				for (int index = 1; index < _errors.Count; ++index)
				{
					writer.WriteLine("    " + _errors[index]);
				}
				writer.WriteLine("For usage information type {0} --help", ProgramName);
			}
		}

		public void ShowHelp(TextWriter writer)
		{
			writer.WriteLine("Usage: {0} [ options ]", ProgramName);
			writer.WriteLine("Convert a VS project from .NET 4.0 to .NET 3.5.");
			writer.WriteLine();
			writer.WriteLine("Options:");
			_optionSet.WriteOptionDescriptions(writer);
			writer.WriteLine("(Items marked * are mandatory)");
		}
	}
}
