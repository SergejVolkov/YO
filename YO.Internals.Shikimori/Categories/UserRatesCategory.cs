using System.Collections.Generic;
using System.Net.Http;
using YO.Internals.Shikimori.Categories.Abstractions;
using YO.Internals.Shikimori.Data;
using YO.Internals.Shikimori.Fluent;

namespace YO.Internals.Shikimori.Categories
{
	public class UserRatesCategory : BaseCategory, IUserRates
	{
		public UserRatesCategory(HttpClient httpClient, string baseUrl) 
			: base(httpClient, baseUrl + "/v2/user_rates")
		{ }

		public GetUserRatesFluentRequest GetUserRates() 
			=> new (GetAsync<IReadOnlyCollection<UserRate>?>, string.Empty);
	}
}