using System.Collections.Generic;
using System.Threading.Tasks;
using YO.Internals.Shikimori.Data;
using YO.Internals.Shikimori.Parameters;

namespace YO.Internals.Shikimori.Categories.Abstractions
{
	public interface IAnimes
	{
		Task<IReadOnlyCollection<AnimeInfo>?> GetAnimes(GetAnimesParameters parameters);
		Task<AnimeInfo?> GetAnime(long id);
	}
}