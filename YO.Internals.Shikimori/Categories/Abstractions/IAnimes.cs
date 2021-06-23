using YO.Internals.Shikimori.Data;
using YO.Internals.Shikimori.Fluent;

namespace YO.Internals.Shikimori.Categories.Abstractions
{
	public interface IAnimes
	{
		FluentApiRequest<AnimeInfo?> GetAnime(long id);
		GetAnimesFluentRequest GetAnimes();
	}
}