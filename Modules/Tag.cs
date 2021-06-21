using System.Collections.Generic;
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
}
