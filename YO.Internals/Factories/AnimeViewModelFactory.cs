using YO.Internals.Cache;
using YO.Internals.Schedule;
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

		public ScheduledEpisodeViewModel Create(ScheduledEpisode scheduledEpisode) 
			=> new(scheduledEpisode) {Poster = _imageCache.TryGetAnimePoster(scheduledEpisode.Anime)};
	}
}