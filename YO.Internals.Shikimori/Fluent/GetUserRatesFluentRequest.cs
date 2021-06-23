using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YO.Internals.Shikimori.Data;
using YO.Internals.Shikimori.Parameters;

namespace YO.Internals.Shikimori.Fluent
{
	public class GetUserRatesFluentRequest 
		: FluentApiRequest<IReadOnlyCollection<UserRate>?>
	{
		public GetUserRatesFluentRequest(
			Func<string, ParametersBase, Task<IReadOnlyCollection<UserRate>?>> httpMethod,
			string requestPath)
			: base(httpMethod, requestPath)
		{ }

		public GetUserRatesFluentRequest WithUserId(long userId) 
			=> With("userId", userId);

		public GetUserRatesFluentRequest WithTargetId(long targetId)
			=> With("target_id", targetId);
		
		public GetUserRatesFluentRequest WithTargetType(DataType targetType)
			=> With("target_type", targetType);
		
		public GetUserRatesFluentRequest WithStatus(RateStatus status)
			=> With("status", status);
		
		public GetUserRatesFluentRequest WithPage(int page)
			=> With("page", page);
		
		public GetUserRatesFluentRequest WithLimit(int limit)
			=> With("limit", limit);

		private GetUserRatesFluentRequest With(string name, object value)
		{
			RequestParameters[name] = value;
			return this;
		}
	}
}