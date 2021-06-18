using System.Collections.Generic;
using System.Threading.Tasks;
using YO.Internals.Shikimori.Data;

namespace YO.Internals.Shikimori.Categories.Abstractions
{
	public interface IAnimes
	{
		Task<IReadOnlyCollection<AnimeInfo>> GetAll();
		Task<AnimeInfo> GetAnime(long id);
	}
}