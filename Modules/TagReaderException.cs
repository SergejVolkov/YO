using System;

namespace YO.Modules
{
	[Serializable]
	public class TagReaderException : Exception
	{
		public TagReaderException(string message,
								  int line, 
								  string parent, 
								  string word, 
								  char separator, 
								  Tag cause = null,
								  string info = null) :
			base(message 
			   + $"\nreading file, line #{line}\n" 
			   + cause 
			   + $"\n{(info == null ? "" : info + "\r    ")}" 
			   + $"parent =\"{(parent == "" ? "none" : parent)}\"\r    word=\"{word}\"\r    separator=\'{(separator == '\r' || separator == '\n' ? "" : separator.ToString())}\'")
		{ }
	}
}