using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YO.Internals.Configuration;
using YO.Internals.Shikimori;
using YO.Internals.Shikimori.Data;
using YO.Internals.Shikimori.Parameters;

namespace YO.Internals.Schedule
{
	public class ShikimoriScheduler : IScheduler
	{
		private readonly IConfiguration _configuration;
		private readonly IShikimoriApi _shikimoriApi;
		private readonly List<ScheduledEpisode> _entries;
		private long _userId;

		public ShikimoriScheduler(IConfiguration configuration, IShikimoriApi shikimoriApi)
		{
			_configuration = configuration;
			_shikimoriApi = shikimoriApi;

			_entries = new List<ScheduledEpisode>();
		}

		public IReadOnlyCollection<ScheduledEpisode> ScheduledEntries
			=> _entries;

		public async Task ScheduleAnime(AnimeInfo anime)
		{
			var userRates = await _shikimoriApi.UserRates
											   .GetUserRates()
											   .WithUserId(_userId)
											   .WithTargetType(DataType.Anime)
											   .WithTargetId(anime.Id);

			ScheduleAnime(anime, userRates.Single());
		}

		public async Task ScheduleAnime(UserRate rate)
		{
			var anime = await _shikimoriApi.Animes.GetAnime(rate.TargetId);

			ScheduleAnime(anime, rate);
		}

		public void UpdateSchedule()
		{
			// TODO: More accurate way to clean up old episodes
			_entries.RemoveAll(e => e.ScheduledTime < DateTime.Today);
			
			for (var day = 0; day < _configuration.DaysLimit; day++)
			{
				var scheduledThatDay = GetByDayOffset(day);
				
				foreach (var scheduledEpisode in scheduledThatDay.Skip(_configuration.EpisodesPerDay))
				{
					scheduledEpisode.ScheduledTime = scheduledEpisode.ScheduledTime.AddDays(1);
				}
			}
		}

		private IEnumerable<ScheduledEpisode> GetByDayOffset(int dayOffset = 0)
		{
			var date = DateTime.Today.AddDays(dayOffset);
			return _entries.Where(e => e.ScheduledTime.Date == date);
		}
		
		private void ScheduleAnime(AnimeInfo anime, UserRate userRate)
		{
			// TODO: Implement released anime schedule
			if (anime.Status == AnimeStatus.Ongoing)
			{
				var startingEpisode = userRate.Episodes + 1;
				var totalEpisodes = anime.Episodes;
				var nextEpisodeDay = anime.NextEpisodeTime!.Value.Date;
				var daysUntilNextEpisode = (nextEpisodeDay - DateTime.Today).TotalDays;
				var dayOffset = daysUntilNextEpisode + _configuration.DelayForNewSeries;

				for (var episode = startingEpisode;
					episode < totalEpisodes && dayOffset < _configuration.DaysLimit;
					episode++, dayOffset += 7)
				{
					_entries.Add(new ScheduledEpisode(nextEpisodeDay.AddDays(dayOffset), episode));
				}
			}
			
			UpdateSchedule();
		}
	}
}