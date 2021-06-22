﻿using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using YO.Internals.Shikimori.Categories.Abstractions;
using YO.Internals.Shikimori.Data;
using YO.Internals.Shikimori.Parameters;

namespace YO.Internals.Shikimori.Categories
{
	public class AnimesCategory : BaseCategory, IAnimes
	{
		public AnimesCategory(HttpClient httpClient, string baseUrl) 
			: base(httpClient, baseUrl + "/animes")
		{ }

		public Task<IReadOnlyCollection<AnimeInfo>?> GetAnimes(GetAnimesParameters parameters)
			=> GetAsync<IReadOnlyCollection<AnimeInfo>>(string.Empty, parameters);

		public Task<AnimeInfo?> GetAnime(long id)
			=> GetAsync<AnimeInfo>($"/{id}", ParametersBase.Empty);
	}
}