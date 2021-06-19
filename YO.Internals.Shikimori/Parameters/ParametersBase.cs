using System;
using System.Collections.Generic;
using System.Text;

namespace YO.Internals.Shikimori.Parameters
{
	public abstract class ParametersBase
	{
		private readonly Dictionary<string, object?> _parameters = new();

		public object? this[string parameter]
		{
			get => _parameters[parameter];
			set => _parameters[parameter] = value;
		}

		internal static ParametersBase Empty => new GetUserRatesParameters();
		
		internal Uri BuildQuery(string baseUrl)
		{
			var stringBuilder = new StringBuilder(baseUrl);
			var first = true;
			var appendFormat = "?{0}={1}";
			foreach (var (name, value) in _parameters)
			{
				stringBuilder.AppendFormat(appendFormat, name, value);
				
				if (first)
				{
					appendFormat = "&{0}={1}";
					first = false;
				}
			}

			return new Uri(stringBuilder.ToString());
		}
	}
}