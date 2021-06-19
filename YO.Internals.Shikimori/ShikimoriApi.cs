using System.Net.Http;
using YO.Internals.Shikimori.Categories;
using YO.Internals.Shikimori.Categories.Abstractions;

namespace YO.Internals.Shikimori
{
	public class ShikimoriApi : IShikimoriApi
	{
		public const string ShikimoriUrl = "https://shikimori.one";
		public const string ShikimoriApiUrl = ShikimoriUrl + "/api";
		
		public IAnimes Animes { get; }
		public IUsers Users { get; }
		public IUserRates UserRates { get; }

		public ShikimoriApi(HttpClient httpClient)
		{
			Animes = new AnimesCategory(httpClient, ShikimoriApiUrl);
			Users = new UsersCategory(httpClient, ShikimoriApiUrl);
			UserRates = new UserRatesCategory(httpClient, ShikimoriApiUrl);
		}
	}
}