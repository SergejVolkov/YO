using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YO.Modules
{
	/// <summary>
	/// XML tag reader class.
	/// </summary>
	public class TagReader : IDisposable
	{
		// TODO: Move it so separate enum
		private static readonly string[] HtmlSingleTags =
		{
			"br",
			"hr",
			"img",
			"meta",
			"input",
			"doctype"
		};

		private readonly TagStreamReader _reader;
		private readonly bool _htmlMode;

		/// <summary>
		/// Construct TagReader from file path.
		/// </summary>
		/// <param name="path">Path to file.</param>
		/// <param name="html_mode">False for default XML behaviour, true for HTML mode.</param>
		public TagReader(string path, bool html_mode = false)
		{
			_reader = new TagStreamReader(path);
			_htmlMode = html_mode;
		}

		/// <summary>
		/// Read all XML tags in stream.
		/// </summary>
		/// <param name="parent">Do not set this parameter, it is ised by recursion.</param>
		/// <returns>Collection of tags.</returns>
		// TODO: Refactor it to reduce nesting
		public List<Tag> Read(Tag parent = default)
		{
			if (parent == null)
			{
				parent = new Tag("");
			}

			var content = new List<Tag>();
			while (true)
			{
				var word = _reader.ReadWord(out var separator);
				if (separator == '\0')
				{
					return content;
				}

				if (separator != '<' || word != "")
				{
					parent.SetValue(word);

					if (separator != '<')
					{
						parent.AppendValue(separator + _reader.ReadInnerValue());
					}
				}

				var tag = new Tag();
				if (_reader.Peek() == '?')
				{
					_reader.Read();
				}

				word = _reader.ReadWord(out separator);
				switch (separator)
				{
					case ' ' when word != "":
					{
						tag.Name = word;
						word = _reader.ReadWord(out separator);
						while (separator == '=' && word != "")
						{
							tag.SetValue(word, _reader.ReadValue());
							word = _reader.ReadWord(out separator);
						}

						if (word == "")
						{
							if (separator == '>'
							 && !(_htmlMode && HtmlSingleTags.Contains(tag.Name)))
							{
								tag.Content.AddRange(Read(tag));
							} else if ((separator == '?' || separator == '/')
									&& ((word = _reader.ReadWord(out separator)) != "" || separator != '>'))
							{
								throw new TagReaderException("Unclosed tag!", 
															 _reader.Line, 
															 parent.Name, 
															 word,
															 separator, 
															 tag, 
															 "Tag with no content should be finished with \"/>\" construction");
							}
						} else
							throw new TagReaderException("Some weird symbols!", 
														 _reader.Line, 
														 parent.Name, 
														 word,
														 separator, 
														 tag, 
														 "Tag properties should be separated with space");

						break;
					}
					case '>' when word != "":
					{
						tag.Name = word;
						if (!(_htmlMode && HtmlSingleTags.Contains(tag.Name)))
						{
							tag.Content.AddRange(Read(tag));
						}

						break;
					}
					case '?':
					case '/':
					{
						if (word == "")
						{
							tag.Name = _reader.ReadWord(out separator);
							return tag.Name == parent.Name
								? content
								: throw new TagReaderException("Tag name mismatch!", 
															   _reader.Line, 
															   parent.Name, 
															   word,
															   separator, 
															   tag);
						}

						tag.Name = word;
						if ((word = _reader.ReadWord(out separator)) != "" 
						 || separator != '>')
						{
							throw new TagReaderException("Unclosed tag!", 
														 _reader.Line, 
														 parent.Name, 
														 word,
														 separator, 
														 tag,
														 "Tag with no content should be finished with \"/>\" construction");
						}

						break;
					}
					default:
						throw new TagReaderException("Empty or unclosed tag!", 
													 _reader.Line, 
													 parent.Name, 
													 word,
													 separator, 
													 tag);
				}

				content.Add(tag);
			}
		}

		/// <summary>
		/// Close the stream associated with TagReader.
		/// </summary>
		/// <param name="disposing">Dispose flag.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_reader.Close();
			}
		}

		/// <summary>
		/// Close the stream associated with TagReader and release all the resources used by it. 
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}