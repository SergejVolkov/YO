using Avalonia.Media.Imaging;
using ReactiveUI.Fody.Helpers;
using YO.Internals.Schedule;

namespace YO.Internals.ViewModels
{
	public class ScheduledEpisodeViewModel
	{
		private readonly ScheduledEpisode _episode;

		public ScheduledEpisodeViewModel(ScheduledEpisode episode)
		{
			_episode = episode;
		}

		public string Name => _episode.Anime.Name;

		[Reactive]
		public Bitmap? Poster { get; set; }

		public int Episode => _episode.Episode;
	}
}