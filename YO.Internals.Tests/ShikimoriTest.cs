using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using YO.Internals.Shikimori;
using YO.Internals.Shikimori.Data;
using YO.Internals.Shikimori.Parameters;

namespace YO.Internals.Tests
{
	[TestFixture]
	public class ShikimoriTest
	{
		private IShikimoriApi _shikimoriApi;

		[SetUp]
		public void SetUp()
		{
			_shikimoriApi = new ShikimoriApi(new HttpClient());
		}

		[TestCase(367866, "SergejVolkov")]
		[TestCase(135003, "HencoDesu")]
		public async Task GetUserTest(int userId, string userNickname)
		{
			var user = await _shikimoriApi.Users.GetById(userId);
			
			Assert.NotNull(user);
			Assert.AreEqual(userId, user.Id);
			Assert.AreEqual(userNickname, user.Nickname);

			user = await _shikimoriApi.Users.GetById(userId);
			
			Assert.NotNull(user);
			Assert.AreEqual(userId, user.Id);
			Assert.AreEqual(userNickname, user.Nickname);
		}
		
		[TestCase(39535, "Mushoku Tensei: Isekai Ittara Honki Dasu", true)]
		[TestCase(21, "One Piece", false)]
		public async Task GetAnimeTest(int animeId, string animeName, bool isReleased)
		{
			var anime = await _shikimoriApi.Animes.GetAnime(animeId);
			var expectedStatus = isReleased ? AnimeStatus.Released : AnimeStatus.Ongoing;

			Assert.NotNull(anime);
			Assert.AreEqual(animeId, anime.Id);
			Assert.AreEqual(animeName, anime.Name);
			Assert.AreEqual(expectedStatus, anime.Status);
			if (!isReleased)
			{
				Assert.NotNull(anime.NextEpisodeTime);
			}
		}

		[TestCase(135003, 39535)]
		public async Task GetUserRateTest(int userId, int animeId)
		{
			var requestParameters = new GetUserRatesParameters()
			{
				UserId = userId,
				TargetType = DataType.Anime,
				TargetId = animeId
			};
			var userRates = await _shikimoriApi.UserRates.GetUserRates(requestParameters);
			
			Assert.NotNull(userRates);
			Assert.IsNotEmpty(userRates);
			Assert.AreEqual(1, userRates.Count);
		}
	}
}