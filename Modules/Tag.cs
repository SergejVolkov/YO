using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YO.Modules {
    /// <summary>
    /// XML tag class.
    /// </summary>
    public class Tag
    {
        public string Name = null;
        public List<Tag> Content;
        private Dictionary<string, string> properties;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Tag()
        {
            properties = new Dictionary<string, string>();
            Content = new List<Tag>();
            SetValue("__content__", "");
        }

        /// <summary>
        /// Construct from XML tag name.
        /// </summary>
        /// <param name="name">Tag name.</param>
        public Tag(string name)
        {
            properties = new Dictionary<string, string>();
            Content = new List<Tag>();
            Name = name;
            SetValue("__content__", "");
        }

        /// <summary>
        /// Set XML attribute.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public void SetValue(string key, string value)
        {
            if (properties.ContainsKey(key)) properties[key] = value;
            else properties.Add(key, value);
        }

        /// <summary>
        /// Get XML attribute. Returns inner content by default.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Value.</returns>
        public string GetValue(string key = "__content__")
        {
            if (!properties.ContainsKey(key)) throw new KeyNotFoundException($"Property with key \"{key}\" does not exist!");
            return properties[key];
        }

        /// <summary>
        /// Check if XML attribute is equivalent to boolean true.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Bool attribute value.</returns>
        public bool IsValueTrue(string key = "__content__")
        {
            try { return bool.Parse(GetValue(key)); }
            catch { return false; };
        }

        /// <summary>
        /// Get child XML tag.
        /// </summary>
        /// <param name="name">Child tag name.</param>
        /// <returns>Child tag.</returns>
        public Tag GetContent(string name)
        {
            Tag occurrence = Content.Find(p => p.Name == name);
            if (occurrence != null) return occurrence;
            else throw new KeyNotFoundException($"Content with name \"{name}\" does not exist!");
        }

        /// <summary>
        /// Set child XML tag.
        /// </summary>
        /// <param name="content">New child tag object.</param>
        public void SetContent(Tag content)
        {
            RemoveContent(content.Name);
            Content.Add(content);
        }

        /// <summary>
        /// Recursive deep clone.
        /// </summary>
        /// <returns>Cloned tag.</returns>
        public Tag Clone()
        {
            Tag copy = new Tag(Name);
            foreach (var key in Keys) copy.SetValue(key, properties[key]);
            foreach (var child in Content) copy.Content.Add(child.Clone());
            return copy;
        }

        public string[] Keys => properties.Keys.ToArray();
        public bool ContainsKey(string key) => properties.ContainsKey(key);
        public void SetValue(string value) => properties["__content__"] = value;
        public void AppendValue(string key, string value) => SetValue(key, GetValue(key) + value);
        public void AppendValue(string value) => SetValue(GetValue() + value);
        public int GetIntValue(string key = "__content__") => int.Parse(GetValue(key));
        public double GetDoubleValue(string key = "__content__") => double.Parse(GetValue(key));
        public bool CheckValue(string key, string value) => GetValue(key) == value;
        public bool CheckValue(string key, int value) => GetIntValue(key) == value;
        public bool CheckValue(string key, double value) => GetDoubleValue(key) == value;
        public bool CheckValue(string value) => GetValue() == value;
        public bool CheckValue(int value) => GetIntValue() == value;
        public bool CheckValue(double value) => GetDoubleValue() == value;
        public void RemoveValue(string key) => properties.Remove(key);
        public void RemoveValue() => properties["__content__"] = "";
        public List<Tag> GetAllContent(string name) => Content.FindAll(p => p.Name == name);
        public void RemoveContent(string name) => Content.RemoveAll(p => p.Name == name);
        public bool ContainsContent(string name) => Content.Exists(p => p.Name == name);
    }

    /// <summary>
    /// XML tag reader class.
    /// </summary>
    public class TagReader : IDisposable
    {
        private TagStreamReader reader;

        public TagReader(Stream stream) => reader = new TagStreamReader(stream);
        public TagReader(string path) => reader = new TagStreamReader(path);

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
                        if (separator == '>') tag.Content.AddRange(Read(tag));
                        else if ((separator == '?' || separator == '/') && ((word = reader.ReadWord(out separator)) != "" || separator != '>'))
                            throw new TagReaderException("Unclosed tag!", reader.Line, parent.Name, word, separator, tag,
                                "Tag with no content should be finished with \"/>\" construction");
                    }
                    else throw new TagReaderException("Some weird symbols!", reader.Line, parent.Name, word, separator, tag,
                        "Tag properties should be separated with space");
                }
                else if (separator == '>' && word != "")
                {
                    tag.Name = word;
                    tag.Content.AddRange(Read(tag));
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

    /// <summary>
    /// Various string array processing extension methods.
    /// </summary>
    public static class StringExtensions
    { 
        public static int[] ToIntArray(this string argument, char splitter)
        {
            string[] array = argument.Split(splitter);
            int[] output = new int[array.Length];
            for (int i = 0; i < array.Length; i++)
                output[i] = int.Parse(array[i]);
            return output;
        }

        public static int[] ToIntArray(this string[] argument)
        {
            int[] output = new int[argument.Length];
            for (int i = 0; i < argument.Length; i++)
                output[i] = int.Parse(argument[i]);
            return output;
        }

        public static double[] ToDoubleArray(this string[] argument)
        {
            double[] output = new double[argument.Length];
            for (int i = 0; i < argument.Length; i++)
                output[i] = double.Parse(argument[i]);
            return output;
        }

        public static double[] ToDoubleArray(this string argument, char splitter)
        {
            string[] array = argument.Split(splitter);
            double[] output = new double[array.Length];
            for (int i = 0; i < array.Length; i++)
                output[i] = double.Parse(array[i]);
            return output;
        }

        public static string[] DotChange(this string[] input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = input[i].Replace(',', '.');
            }
            return input;
        }

        public static string Merge(this string[] input, char splitter, char wrapper = '\0')
        {
            string output = "";
            foreach (var elem in input)
            {
                if (wrapper == '\0') output += elem + splitter;
                else output += wrapper + elem + wrapper + splitter;
            }
            return output.Remove(output.Length - 1, 1);
        }
    }
}
