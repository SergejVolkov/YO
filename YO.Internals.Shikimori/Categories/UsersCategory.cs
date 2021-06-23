using System.Net.Http;
using YO.Internals.Shikimori.Categories.Abstractions;
using YO.Internals.Shikimori.Data;
using YO.Internals.Shikimori.Fluent;

namespace YO.Internals.Shikimori.Categories
{
	public class UsersCategory : BaseCategory, IUsers
	{
		public UsersCategory(HttpClient httpClient, string baseUrl)
			: base(httpClient, baseUrl + "/users")
		{ }

		public FluentApiRequest<User?> GetById(long id)
			=> new (GetAsync<User>, $"/{id}");

		public FluentApiRequest<User?> GetByNickname(string nickname) 
			=> new FluentApiRequest<User?> (GetAsync<User>, $"/{nickname}")
				.With("is_nickname", 1);
	}
}