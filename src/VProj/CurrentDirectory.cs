using System;

namespace VProj
{
	public static class CurrentDirectory
	{
		public static IDisposable ChangeTo(string directory)
		{
			if (string.IsNullOrWhiteSpace(directory))
			{
				return new DoNothing();
			}

			var disposable = new ChangeDirectory(Environment.CurrentDirectory);
			Environment.CurrentDirectory = directory;
			return disposable;
		}

		private class DoNothing : IDisposable
		{
			public void Dispose()
			{
			}
		}

		private class ChangeDirectory : IDisposable
		{
			private readonly string _directory;

			public ChangeDirectory(string directory)
			{
				_directory = directory;
			}

			public void Dispose()
			{
				Environment.CurrentDirectory = _directory;
			}
		}
	}
}