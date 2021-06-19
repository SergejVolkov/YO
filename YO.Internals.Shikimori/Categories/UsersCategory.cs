using System.Net.Http;
using System.Threading.Tasks;
using YO.Internals.Shikimori.Categories.Abstractions;
using YO.Internals.Shikimori.Data;
using YO.Internals.Shikimori.Parameters;

namespace YO.Internals.Shikimori.Categories
{
	public class UsersCategory : BaseCategory, IUsers
	{
		public UsersCategory(HttpClient httpClient, string baseUrl)
			: base(httpClient, baseUrl + "/users")
		{ }

		public Task<User?> GetById(long id)
			=> GetAsync<User>($"/{id}", ParametersBase.Empty);

		public Task<User?> GetByNickname(string nickname)
		{
			var parameters = ParametersBase.Empty;
			parameters["is_nickname"] = 1;
			return GetAsync<User>($"/{nickname}", parameters);
		}
	}
}