using System;
using System.IO;
using System.Linq;

namespace YO.Modules
{
	/// <summary>
	/// XML tag writer class.
	/// </summary>
	public class TagWriter : IDisposable
	{
		private StreamWriter writer;

		public TagWriter(string path)
		{
			var stream = new FileStream(path, FileMode.Create);
			writer = new StreamWriter(stream);
		}

		public TagWriter(Stream stream) => writer = new StreamWriter(stream);

		/// <summary>
		/// Write XML tag and its children to the output stream.
		/// </summary>
		/// <param name="tag">XML tag object.</param>
		/// <param name="reclev">Do not set this parameter, it is ised by recursion.</param>
		public void WriteTag(Tag tag, int reclev = 0)
		{
			for (int i = 0; i < reclev * 4; i++) writer.Write(' ');
			writer.Write("<" + tag.Name);
			foreach (var key in tag.Keys)
			{
				if (key != "__content__")
				{
					writer.Write(' ');
					writer.Write(key);
					writer.Write("=\"");
					writer.Write(tag.GetValue(key));
					writer.Write("\"");
				}
			}
			if (tag.Content.Count == 0 && tag.CheckValue("")) writer.Write("/>\r\n");
			else
			{
				writer.Write(">");
				if (tag.GetValue().Count(p => p == '\n') > 0 || tag.Content.Count > 0)
				{
					writer.Write("\r\n");
					if (!tag.CheckValue(""))
					{
						for (int i = 0; i < (reclev + 1) * 4; i++) writer.Write(' ');
						if (tag.GetValue().EndsWith("\n"))
						{
							writer.Write(tag.GetValue());
						}
						else
						{
							writer.Write(tag.GetValue() + "\r\n");
						}
					}
					for (int i = 0; i < tag.Content.Count; i++) WriteTag(tag.Content[i], reclev + 1);
					for (int i = 0; i < reclev * 4; i++) writer.Write(' ');
				}
				else
				{
					writer.Write(tag.GetValue());
				}
				writer.Write($"</{tag.Name}>\r\n");
			}
			writer.Flush();
		}

		/// <summary>
		/// Close the stream associated with TagWriter.
		/// </summary>
		/// <param name="disposing">Dispose flag.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing) writer.Close();
		}

		/// <summary>
		/// Close the stream associated with TagWriter and release all the resources used by it. 
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}