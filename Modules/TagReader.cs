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
		static string[] html_single_tags = {
			"br",
			"hr",
			"img",
			"meta",
			"input",
			"doctype"
		};

		private TagStreamReader reader;
		bool html_mode = false;

		/// <summary>
		/// Construct TagReader from file stream.
		/// </summary>
		/// <param name="stream">File stream.</param>
		/// <param name="html_mode">False for default XML behaviour, true for HTML mode.</param>
		public TagReader(Stream stream, bool html_mode = false) {
			reader = new TagStreamReader(stream);
			this.html_mode = html_mode;
		}

		/// <summary>
		/// Construct TagReader from file path.
		/// </summary>
		/// <param name="path">Path to file.</param>
		/// <param name="html_mode">False for default XML behaviour, true for HTML mode.</param>
		public TagReader(string path, bool html_mode = false) {
			reader = new TagStreamReader(path);
			this.html_mode = html_mode;
		}

		/// <summary>
		/// Read all XML tags in stream.
		/// </summary>
		/// <param name="parent">Do not set this parameter, it is ised by recursion.</param>
		/// <returns>Collection of tags.</returns>
		public List<Tag> Read(Tag parent = default(Tag))
		{
			if (parent == null)
				parent = new Tag("");
			List<Tag> content = new List<Tag>();
			while (true)
			{
				string word = reader.ReadWord(out char separator);
				if (separator == '\0') return content;
				else if (separator != '<' || word != "")
				{
					//if (content.Count != 0) throw new TagReaderException("Mixed inner content and tags!", reader.Line, parent.Name, word, separator, parent, "May be caused by some weird off-tag expressions");
					parent.SetValue(word);
					if (separator != '<')
						parent.AppendValue(separator + reader.ReadInnerValue());
				}
				Tag tag = new Tag();
				if (reader.Peek() == '?')
					reader.Read();
				word = reader.ReadWord(out separator);
				if (separator == ' ' && word != "")
				{
					tag.Name = word;
					word = reader.ReadWord(out separator);
					while (separator == '=' && word != "")
					{
						tag.SetValue(word, reader.ReadValue());
						word = reader.ReadWord(out separator);
					}
					if (word == "")
					{
						if (separator == '>' && !(html_mode && html_single_tags.Contains(tag.Name))) {
							tag.Content.AddRange(Read(tag));
						} else if ((separator == '?' || separator == '/') && ((word = reader.ReadWord(out separator)) != "" || separator != '>')) {
							throw new TagReaderException("Unclosed tag!", reader.Line, parent.Name, word, separator, tag,
														 "Tag with no content should be finished with \"/>\" construction");
						}
					}
					else throw new TagReaderException("Some weird symbols!", reader.Line, parent.Name, word, separator, tag,
													  "Tag properties should be separated with space");
				}
				else if (separator == '>' && word != "")
				{
					tag.Name = word;
					if (!(html_mode && html_single_tags.Contains(tag.Name))) {
						tag.Content.AddRange(Read(tag));
					}
				}
				else if (separator == '?' || separator == '/')
				{
					if (word == "")
					{
						tag.Name = reader.ReadWord(out separator);
						if (tag.Name == parent.Name)
							return content;
						else throw new TagReaderException("Tag name mismatch!", reader.Line, parent.Name, word, separator, tag);
					}
					else
					{
						tag.Name = word;
						if ((word = reader.ReadWord(out separator)) != "" || separator != '>')
							throw new TagReaderException("Unclosed tag!", reader.Line, parent.Name, word, separator, tag,
														 "Tag with no content should be finished with \"/>\" construction");
					}
				}
				else throw new TagReaderException("Empty or unclosed tag!", reader.Line, parent.Name, word, separator, tag);
				//if (!parent.CheckValue("")) throw new TagReaderException("Mixed inner content and tags!", reader.Line, parent.Name, word, separator, parent, "May be caused by some weird off-tag expressions");
				content.Add(tag);
			}
		}

		/// <summary>
		/// Close the stream associated with TagReader.
		/// </summary>
		/// <param name="disposing">Dispose flag.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing) reader.Close();
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