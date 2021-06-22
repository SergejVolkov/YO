using System.Collections.Generic;

namespace YO.Modules
{
	/// <summary>
	/// XML tag class.
	/// </summary>
	public class Tag
	{
		private readonly Dictionary<string, string> _properties;

		

		/// <summary>
		/// Default constructor.
		/// </summary>
		public Tag()
		{
			_properties = new Dictionary<string, string>();
			Content = new List<Tag>();
			SetValue("__content__", "");
		}

		/// <summary>
		/// Construct from XML tag name.
		/// </summary>
		/// <param name="name">Tag name.</param>
		public Tag(string name) 
			: this()
		{
			Name = name;
		}
		
		public string Name { get; set; }
		public List<Tag> Content { get; }
		public IEnumerable<string> Keys => _properties.Keys;

		/// <summary>
		/// Set XML attribute.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
		public void SetValue(string key, string value)
		{
			if (!_properties.ContainsKey(key))
			{
				_properties.Add(key, value);
			} else
			{
				_properties[key] = value;
			}
		}

		/// <summary>
		/// Get XML attribute. Returns inner content by default.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <returns>Value.</returns>
		public string GetValue(string key = "__content__")
		{
			if (!_properties.ContainsKey(key))
			{
				throw new KeyNotFoundException($"Property with key \"{key}\" does not exist!");
			}

			return _properties[key];
		}

		/// <summary>
		/// Check if XML attribute is equivalent to boolean true.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <returns>Bool attribute value.</returns>
		public bool IsValueTrue(string key = "__content__")
		{
			return bool.TryParse(GetValue(key), out var value) && value;
		}

		/// <summary>
		/// Get child XML tag.
		/// </summary>
		/// <param name="name">Child tag name.</param>
		/// <returns>Child tag.</returns>
		public Tag GetContent(string name)
		{
			var occurrence = Content.Find(p => p.Name == name);
			return occurrence ?? throw new KeyNotFoundException($"Content with name \"{name}\" does not exist!");
		}

		public bool ContainsKey(string key) => _properties.ContainsKey(key);
		public void SetValue(string value) => _properties["__content__"] = value;
		public void AppendValue(string value) => SetValue(GetValue() + value);
		public int GetIntValue(string key = "__content__") => int.Parse(GetValue(key));
		public bool CheckValue(string key, string value) => GetValue(key) == value;
		public bool CheckValue(string value) => GetValue() == value;
		public List<Tag> GetAllContent(string name) => Content.FindAll(p => p.Name == name);
		public bool ContainsContent(string name) => Content.Exists(p => p.Name == name);
	}
}