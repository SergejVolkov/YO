using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using YO.Internals.Shikimori.Data;

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
				switch (value)
				{
					case IEnumerable enumerable:
						stringBuilder.AppendFormat(appendFormat, name, BuildEnumerable(enumerable));
						break;
					case RateStatus:
						stringBuilder.AppendFormat(appendFormat, name, value.ToString().ToLower());
						break;
					default:
						stringBuilder.AppendFormat(appendFormat, name, value);
						break;
				}

				if (first)
				{
					appendFormat = "&{0}={1}";
					first = false;
				}
			}

			return new Uri(stringBuilder.ToString());
		}

		private string BuildEnumerable(IEnumerable enumerable)
		{
			var builder = new StringBuilder();
			var first = true;
			var appendFormat = "{0}";

			foreach (var item in enumerable)
			{
				builder.AppendFormat(appendFormat, item);

				if (first)
				{
					appendFormat = ",{0}";
					first = false;
				}
			}

			return builder.ToString();
		}
	}
}