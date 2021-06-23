using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using YO.Internals.Shikimori.Data;

namespace YO.Internals.Cache
{
	public interface IImageCache
	{
		Task<Bitmap> TryGetAnimePoster(AnimeInfo anime);

		Task<Bitmap> TryGetUserPicture(User user);
	}
}