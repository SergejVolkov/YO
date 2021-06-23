using System;
using System.Collections.Generic;
using FakeItEasy;
using NUnit.Framework;
using YO.Internals.Configuration;
using YO.Internals.Schedule;
using YO.Internals.Shikimori;
using YO.Internals.Shikimori.Data;
using YO.Internals.Shikimori.Parameters;

namespace YO.Internals.Tests
{
	[TestFixture]
	public class SchedulerTest
	{
		private IConfiguration _configuration;
		private IShikimoriApi _shikimoriApi;

		private User _testUser;
		private AnimeInfo _testOngoingAnime;
		private UserRate _testRate;
		private GetUserRatesParameters _getUserRatesParameters;

		[SetUp]
		public void SetUp()
		{
			_testUser = new User
			{
				Id = 135003, 
				Nickname = "HencoDesu"
			};
			_testOngoingAnime = new AnimeInfo
			{
				Id = 39535, 
				Episodes = 12,
				AiredEpisodes = 2,
				Status = AnimeStatus.Ongoing, 
				NextEpisodeTime = DateTime.Today.AddDays(5)
			};
			_testRate = new UserRate
			{
				UserId = _testUser.Id, 
				TargetType = DataType.Anime, 
				TargetId = _testOngoingAnime.Id,
				Episodes = 0,
			};
			_getUserRatesParameters = new GetUserRatesParameters
			{
				UserId = _testUser.Id, 
				TargetType = DataType.Anime, 
				TargetId = _testOngoingAnime.Id
			};

			_configuration = A.Fake<IConfiguration>();
			_shikimoriApi = A.Fake<IShikimoriApi>();

			A.CallTo(() => _configuration.ShikimoriUsername).Returns(_testUser.Nickname);
			A.CallTo(() => _configuration.EpisodesPerDay).Returns(1);
			A.CallTo(() => _configuration.DelayForNewSeries).Returns(1);
		}

		[TestCase(1, 0)]
		[TestCase(7, 1)]
		public void ScheduleOngoing(int daysLimit, int expectedEntries)
		{
			A.CallTo(() => _configuration.DaysLimit).Returns(daysLimit);
			var scheduler = GetScheduler();

			scheduler.ScheduleAnime(_testRate);
			
			Assert.AreEqual(expectedEntries, scheduler.ScheduledEntries.Count);
		}

		private IScheduler GetScheduler()
			=> new ShikimoriScheduler(_configuration, _shikimoriApi);
	}
}