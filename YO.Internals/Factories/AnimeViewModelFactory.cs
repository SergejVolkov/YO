using YO.Internals.Cache;
using YO.Internals.Shikimori.Data;
using YO.Internals.ViewModels;

namespace YO.Internals.Factories
{
	public class AnimeViewModelFactory
	{
		private readonly IImageCache _imageCache;

		public AnimeViewModelFactory(IImageCache imageCache)
		{
			_imageCache = imageCache;
		}

		public AnimeViewModel Create(AnimeInfo anime) 
			=> new(anime) {Poster = _imageCache.TryGetAnimePoster(anime)};
	}
}