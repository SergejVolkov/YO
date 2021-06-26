using Avalonia.Media.Imaging;
using YO.Internals.Shikimori.Data;

namespace YO.Internals.Cache
{
	public interface IImageCache
	{
		Bitmap TryGetAnimePoster(AnimeInfo anime);

		Bitmap TryGetUserPicture(User user);
	}
}