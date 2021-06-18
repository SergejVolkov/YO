using System.Collections.Generic;
using System.Text;

namespace YO.Internals.Shikimori.Parameters
{
	public abstract class ParametersBase
	{
		private readonly IDictionary<string, object> _parameters;

		protected ParametersBase()
		{
			_parameters = new Dictionary<string, object>();
		}

		protected T GetParameter<T>(string parameterName)
		{
			try
			{
				return (T) _parameters[parameterName];
			} catch (KeyNotFoundException) when (default(T) is null)
			{
				return default;
			}
		}

		protected void SetParameter(string parameterName, object value)
		{
			if (value != null)
			{
				_parameters[parameterName] = value;
			} else
			{
				_parameters.Remove(parameterName);
			}
		}

		internal string BuildQuery()
		{
			var stringBuilder = new StringBuilder();
			var first = true;
			var appendFormat = "&{0}={1}";
			foreach (var (name, value) in _parameters)
			{
				if (first)
				{
					appendFormat = "?{0}={1}";
					first = false;
				}
				
				stringBuilder.AppendFormat(appendFormat, name, value);
			}

			return stringBuilder.ToString();
		}
	}
}