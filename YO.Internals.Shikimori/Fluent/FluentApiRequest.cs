using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using YO.Internals.Shikimori.Parameters;

namespace YO.Internals.Shikimori.Fluent
{
	public class FluentApiRequest<TResult> 
	{
		private readonly Func<string, ParametersBase, Task<TResult>> _httpMethod;
		private readonly string _requestPath;
		protected ParametersBase RequestParameters { get; }

		public FluentApiRequest(Func<string, ParametersBase, Task<TResult>> httpMethod,
								   string requestPath) 
			: this(httpMethod, requestPath, ParametersBase.Empty)
		{ }

		public FluentApiRequest(Func<string, ParametersBase, Task<TResult>> httpMethod,
								 string requestPath,
								 ParametersBase defaultParameters)
		{
			_httpMethod = httpMethod;
			_requestPath = requestPath;
			RequestParameters = defaultParameters;
		}

		internal FluentApiRequest<TResult> With(string parameterName, object parameterValue)
		{
			RequestParameters[parameterName] = parameterValue;
			return this;
		}
		
		public TaskAwaiter<TResult> GetAwaiter() 
			=> _httpMethod(_requestPath, RequestParameters).GetAwaiter();
	}
}