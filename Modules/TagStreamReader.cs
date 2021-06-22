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
		// TODO: Move it so separate enum
		private static readonly char[] Separators = {'<', '/', '>', '=', '\"', '?'};

		// TODO: Move it so separate enum
		private static readonly char[] Gaps = {' ', '\r', '\n'};

		private int _line;
		private bool _last;

		public TagStreamReader(string path) : base(path)
		{ }

		/// <summary>
		/// Read word surrounded by whitespace or special symbols.
		/// </summary>
		/// <param name="separator">Save separator after word here.</param>
		/// <returns>String representing word.</returns>
		public string ReadWord(out char separator)
		{
			var word = "";
			var read = ' ';
			while (Gaps.Contains(read))
			{
				try
				{
					read = Convert.ToChar(Read());
				} catch
				{
					separator = '\0';
					return "";
				}
			}

			while (!Separators.Contains(read) 
				&& !Gaps.Contains(read))
			{
				word += read;
				try
				{
					read = Convert.ToChar(Read());
				} catch
				{
					throw new TagStreamReaderException("Unexpected end of file!", Line, word);
				}
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
			var word = "";
			var read = ' ';
			while (Gaps.Contains(read))
			{
				try
				{
					read = Convert.ToChar(Read());
				} catch
				{
					throw new TagStreamReaderException("Unexpected end of file!", Line, word);
				}
			}

			if (read != '\"')
			{
				throw new TagStreamReaderException("Missing value!", Line, word,
												   "Check for correct placement of quotes");
			}

			read = '\0';
			while ((read) != '\"')
			{
				if (read != '\0') word += read;
				try
				{
					read = Convert.ToChar(Read());
				} catch
				{
					throw new TagStreamReaderException("Unexpected end of file!", Line, word);
				}
			}

			return word;
		}

		/// <summary>
		/// Read XML inner content.
		/// </summary>
		/// <returns>String representing XML inner content.</returns>
		public string ReadInnerValue()
		{
			var value = "";
			var read = '\0';
			while (read != '<')
			{
				if (read != '\0') value += read;
				try
				{
					read = Convert.ToChar(Read());
				} catch
				{
					throw new TagStreamReaderException("Unexpected end of file!", Line, value);
				}
			}

			return value;
		}

		/// <summary>
		/// Read next char and count lines.
		/// </summary>
		/// <returns>Char read from stream.</returns>
		public override int Read()
		{
			var read = base.Read();
			if (read == '\r' || read == '\n')
			{
				if (_last) _last = false;
				else
				{
					_line++;
					_last = true;
				}
			}

			return read;
		}

		/// <summary>
		/// Current line number.
		/// </summary>
		public int Line => _line + 1;
	}
}