using System;
using System.IO;
using System.Linq;

namespace YO.Modules
{
	/// <summary>
	/// TagReader helper class.
	/// </summary>
	public class TagStreamReader : StreamReader
	{
		private static char[] separators = { '<', '/', '>', '=', '\"', '?' };
		private static char[] gaps = { ' ', '\r', '\n' };

		private int line = 0;
		private bool last = false;

		public TagStreamReader(Stream stream) : base(stream) { }
		public TagStreamReader(string path) : base(path) { }

		/// <summary>
		/// Read word surrounded by whitespace or special symbols.
		/// </summary>
		/// <param name="separator">Save separator after word here.</param>
		/// <returns>String representing word.</returns>
		public string ReadWord(out char separator)
		{
			string word = "";
			char read = ' ';
			while (gaps.Contains(read))
			{
				try { read = Convert.ToChar(Read()); }
				catch
				{
					separator = '\0';
					return "";
				}
			}
			while (!separators.Contains(read) && !gaps.Contains(read))
			{
				word += read;
				try { read = Convert.ToChar(Read()); }
				catch { throw new TagStreamReaderException("Unexpected end of file!", Line, word); }
			}
			separator = read;
			return word;
		}

		/// <summary>
		/// Read XML attribute value.
		/// </summary>
		/// <returns>XML attribute value.</returns>
		public string ReadValue()
		{
			string word = "";
			char read = ' ';
			while (gaps.Contains(read))
			{
				try { read = Convert.ToChar(Read()); }
				catch { throw new TagStreamReaderException("Unexpected end of file!", Line, word); }
			}
			if (read != '\"') throw new TagStreamReaderException("Missing value!", Line, word, "Check for correct placement of quotes");
			read = '\0';
			while ((read) != '\"')
			{
				if (read != '\0') word += read;
				try { read = Convert.ToChar(Read()); }
				catch { throw new TagStreamReaderException("Unexpected end of file!", Line, word); }
				//if (read == '\r' || read == '\n') throw new TagStreamReaderException("Value with a new line advanced within!", Line, word);
			}
			return word;
		}

		/// <summary>
		/// Read XML inner content.
		/// </summary>
		/// <returns>String representing XML inner content.</returns>
		public string ReadInnerValue()
		{
			string value = "";
			char read = '\0';
			while (read != '<')
			{
				if (read != '\0') value += read;
				try { read = Convert.ToChar(Read()); }
				catch { throw new TagStreamReaderException("Unexpected end of file!", Line, value); }
			}
			return value;
		}

		/// <summary>
		/// Read next char and count lines.
		/// </summary>
		/// <returns>Char read from stream.</returns>
		public override int Read()
		{
			int read = base.Read();
			if (read == '\r' || read == '\n')
			{
				if (last) last = false;
				else
				{
					line++;
					last = true;
				}
			}
			return read;
		}

		/// <summary>
		/// Current line number.
		/// </summary>
		public int Line => line + 1;
	}
}