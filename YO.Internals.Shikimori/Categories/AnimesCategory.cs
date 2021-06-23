using System.Collections.Generic;
using System.Net.Http;
using YO.Internals.Shikimori.Categories.Abstractions;
using YO.Internals.Shikimori.Data;
using YO.Internals.Shikimori.Fluent;

namespace YO.Internals.Shikimori.Categories
{
	public class AnimesCategory : BaseCategory, IAnimes
	{
		public AnimesCategory(HttpClient httpClient, string baseUrl) 
			: base(httpClient, baseUrl + "/animes")
		{ }

		public FluentApiRequest<AnimeInfo?> GetAnime(long id)
			=> new(GetAsync<AnimeInfo>, $"/{id}");
		
		public GetAnimesFluentRequest GetAnimes()
			=> new GetAnimesFluentRequest(GetAsync<IReadOnlyCollection<AnimeInfo>>, string.Empty);
	}
}