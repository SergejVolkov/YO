using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using YO.Internals.Shikimori.Categories.Abstractions;
using YO.Internals.Shikimori.Data;
using YO.Internals.Shikimori.Parameters;

namespace YO.Internals.Shikimori.Categories
{
	public class UserRatesCategory : BaseCategory, IUserRates
	{
		public UserRatesCategory(HttpClient httpClient, string baseUrl) 
			: base(httpClient, baseUrl + "/v2/user_rates")
		{ }

		public Task<IReadOnlyCollection<UserRate>?> GetUserRates(GetUserRatesParameters parameters) 
			=> GetAsync<IReadOnlyCollection<UserRate>?>("", parameters);
	}
}