using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using YO.Internals.Shikimori;
using YO.Internals.Shikimori.Data;

namespace YO.Internals.Cache
{
	public class PosterCache : IPosterCache
	{
		private const string PostersDirectory = "Posters";
		private readonly WebClient _webClient;
		private readonly ConcurrentDictionary<long, Bitmap> _posters = new();
		private readonly string _postersFolder;

		public PosterCache(WebClient webClient)
		{
			_webClient = webClient;

			var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
			_postersFolder = Path.Combine(appPath, PostersDirectory);

			if (!Directory.Exists(_postersFolder))
			{
				Directory.CreateDirectory(_postersFolder);
			}
		}

		public async Task<Bitmap> TryGetPoster(AnimeInfo anime)
		{
			if (_posters.TryGetValue(anime.Id, out var poster))
			{
				return poster;
			}

			var filePath = Path.Combine(_postersFolder, $"{anime.Id}.png");
			if (File.Exists(filePath))
			{
				poster = LoadFromDisk(filePath);
			} else
			{
				poster = await LoadFromWeb(ShikimoriApi.ShikimoriUrl + anime.Images[ImageType.Original]);
				poster.Save(filePath);
			}

			_posters.TryAdd(anime.Id, poster);

			return poster;
		}

		private async Task<Bitmap> LoadFromWeb(string url)
		{
			var bytes = await _webClient.DownloadDataTaskAsync(url);
			await using var ms = new MemoryStream(bytes);
			return new Bitmap(ms);
		}

		private static Bitmap LoadFromDisk(string filePath)
		{
			using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
			return new Bitmap(fileStream);
		}
	}
}