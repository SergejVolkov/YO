using System.IO;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using YO.Internals.Shikimori;
using YO.Internals.Shikimori.Data;

namespace YO.Internals.Extensions
{
	public static class AnimesExtension
	{
		public static async Task<Bitmap> GetAnimePoster(this AnimeInfo animeInfo, WebClient webClient)
		{
			var posterPath = ShikimoriApi.ShikimoriUrl + animeInfo.Images[ImageType.Original];
			var bytes = await webClient.DownloadDataTaskAsync(posterPath);
			var ms = new MemoryStream(bytes);
			return new Bitmap(ms);
		}
	}
}