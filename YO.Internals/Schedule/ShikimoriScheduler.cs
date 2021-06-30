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
	public class ShikimoriScheduler
	{
		private readonly IConfiguration _configuration;
		private readonly List<ScheduledEpisode> _entries;

		public ShikimoriScheduler(IConfiguration configuration)
		{
			_configuration = configuration;

			_entries = new List<ScheduledEpisode>();
		}

		public IReadOnlyCollection<ScheduledEpisode> ScheduledEntries
			=> _entries;

		public void ScheduleAnime(AnimeInfo anime, UserRate rate)
		{
			var startingEpisode = rate.Episodes + 1;
			var totalEpisodes = anime.Episodes;

			if (anime.Status == AnimeStatus.Ongoing)
			{
				var airedEpisodes = anime.AiredEpisodes;
				var episode = startingEpisode;

				while (episode < airedEpisodes)
				{
					_entries.Add(new ScheduledEpisode(anime, DateTime.Today, episode++));
				}

				var nextEpisodeDay = anime.NextEpisodeTime.Value.Date;
				var daysUntilNextEpisode = (nextEpisodeDay - DateTime.Today).TotalDays;
				var dayOffset = daysUntilNextEpisode + _configuration.DelayForNewSeries;

				while (episode < totalEpisodes && dayOffset < _configuration.DaysLimit)
				{
					_entries.Add(new ScheduledEpisode(anime, nextEpisodeDay.AddDays(dayOffset), episode++));
					dayOffset += 7;
				}
			} else if (anime.Status == AnimeStatus.Released)
			{
				var episode = startingEpisode;
				var dayOffset = 0;

				while (dayOffset < _configuration.DaysLimit)
				{
					var plannedThatDay = 0;

					while (episode < totalEpisodes && plannedThatDay < _configuration.EpisodesPerDay)
					{
						_entries.Add(new ScheduledEpisode(anime, DateTime.Today.AddDays(dayOffset), episode++));
						plannedThatDay++;
					}

					dayOffset++;
				}
			}
		}

		public void UpdateSchedule()
		{
			for (var day = 0; day < _configuration.DaysLimit; day++)
			{
				var scheduledThatDay = GetByDayOffset(day);

				foreach (var scheduledEpisode in scheduledThatDay.Skip(_configuration.EpisodesPerDay))
				{
					scheduledEpisode.ScheduledTime = scheduledEpisode.ScheduledTime.AddDays(1);
				}
			}

			var lastDay = DateTime.Today.AddDays(_configuration.DaysLimit);
			_entries.RemoveAll( e => e.ScheduledTime == lastDay);
		}

		public void Clear()
		{
			_entries.Clear();
		}

		private IEnumerable<ScheduledEpisode> GetByDayOffset(int dayOffset = 0)
		{
			var date = DateTime.Today.AddDays(dayOffset);
			return _entries.Where(e => e.ScheduledTime.Date == date);
		}
	}
}