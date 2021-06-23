using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YO.Internals.Shikimori.Data;
using YO.Internals.Shikimori.Parameters;

namespace YO.Internals.Shikimori.Fluent
{
	public class GetAnimesFluentRequest
		: FluentApiRequest<IReadOnlyCollection<AnimeInfo>?>
	{
		public GetAnimesFluentRequest(
			Func<string, ParametersBase, Task<IReadOnlyCollection<AnimeInfo>?>> httpMethod,
			string requestPath)
			: base(httpMethod, requestPath)
		{ }

		public GetAnimesFluentRequest WithIds(IEnumerable<long> ids) 
			=> With("ids", ids);

		public GetAnimesFluentRequest WithPage(int page)
			=> With("page", page);
		
		public GetAnimesFluentRequest WithLimit(int limit)
			=> With("limit", limit);

		private GetAnimesFluentRequest With(string name, object value)
		{
			RequestParameters[name] = value;
			return this;
		}
	}
}