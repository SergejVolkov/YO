using System.Threading.Tasks;
using YO.Internals.Shikimori.Data;

namespace YO.Internals.Shikimori.Categories.Abstractions
{
	public interface IAnimes
	{
		Task<AnimeInfo?> GetAnime(long id);
	}
}