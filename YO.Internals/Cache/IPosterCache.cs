using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using YO.Internals.Shikimori.Data;

namespace YO.Internals.Cache
{
	public interface IPosterCache
	{
		Task<Bitmap> TryGetPoster(AnimeInfo anime);
	}
}