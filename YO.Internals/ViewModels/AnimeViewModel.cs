using Avalonia.Media.Imaging;
using ReactiveUI.Fody.Helpers;
using YO.Internals.Shikimori.Data;

namespace YO.Internals.ViewModels
{
	public class AnimeViewModel
	{
		private readonly AnimeInfo _animeInfo;

		public AnimeViewModel(AnimeInfo animeInfo)
		{
			_animeInfo = animeInfo;
		}

		public string Name => _animeInfo.Name;

		[Reactive]
		public Bitmap? Poster { get; set; }
	}
}