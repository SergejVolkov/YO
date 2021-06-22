using System;

namespace YO.Modules
{
	[Serializable]
	public class TagStreamReaderException : Exception
	{
		public TagStreamReaderException(string message, int line, string word, string info = null) :
			base(message + $"reading file, line #{line}\n{(info == null ? "" : info + "\r    ")}word=\"{(word == "" ? "unavailable" : word)}\"")
		{ }
	}
}