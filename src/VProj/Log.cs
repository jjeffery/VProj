using System;

namespace VProj
{
	public static class Log
	{
		public static void Debug(string message)
		{
			if (CommandLine.Instance.Verbose)
			{
				Console.WriteLine(message);
			}
		}

		public static void DebugFormat(string format, params object[] args)
		{
			Debug(string.Format(format, args));
		}

		public static void Info(string message)
		{
			if (!CommandLine.Instance.Quiet || CommandLine.Instance.Verbose)
			{
				Console.WriteLine(message);
			}
		}

		public static void InfoFormat(string format, params object[] args)
		{
			Info(string.Format(format, args));
		}

		public static void Warn(string message)
		{
			var prevColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("WARNING: " + message);
			Console.ForegroundColor = prevColor;
		}

		public static void WarnFormat(string format, params object[] args)
		{
			Warn(string.Format(format, args));
		}

		public static void Error(string message)
		{
			var prevColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("ERROR: " + message);
			Console.ForegroundColor = prevColor;
		}

		public static void ErrorFormat(string format, params object[] args)
		{
			Error(string.Format(format, args));
		}
	}
}
