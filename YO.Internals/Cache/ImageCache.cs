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
	public class ImageCache : IImageCache
	{
		private const string PostersDirectory = "Posters";
		private readonly WebClient _webClient;
		private readonly ConcurrentDictionary<long, Bitmap> _posters = new();
		private readonly string _postersFolder;

		public ImageCache(WebClient webClient)
		{
			_webClient = webClient;

			var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
			_postersFolder = Path.Combine(appPath, PostersDirectory);

			if (!Directory.Exists(_postersFolder))
			{
				Directory.CreateDirectory(_postersFolder);
			}
		}

		public Task<Bitmap> TryGetAnimePoster(AnimeInfo anime) 
			=> TryGetPicture(anime.Id, ShikimoriApi.ShikimoriUrl + anime.Images[ImageType.Original]);

		public Task<Bitmap> TryGetUserPicture(User user) 
			=> TryGetPicture(user.Id, user.Avatar);

		private async Task<Bitmap> TryGetPicture(long id, string webUrl)
		{
			if (_posters.TryGetValue(id, out var poster))
			{
				return poster;
			}

			var filePath = Path.Combine(_postersFolder, $"{id}.png");
			if (File.Exists(filePath))
			{
				poster = LoadFromDisk(filePath);
			} else
			{
				poster = await LoadFromWeb(webUrl);
				poster.Save(filePath);
			}

			_posters.TryAdd(id, poster);

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