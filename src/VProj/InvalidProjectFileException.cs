using System;

namespace VProj
{
	public class InvalidProjectFileException : Exception
	{
		public InvalidProjectFileException(string message) : base(message) {}
	}
}
