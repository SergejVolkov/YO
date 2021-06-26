using System.Collections.Generic;
using System.Threading.Tasks;
using YO.Internals.Shikimori.Data;

namespace YO.Internals.Schedule
{
	public interface IScheduler
	{
		IReadOnlyCollection<ScheduledEpisode> ScheduledEntries { get; }
		void ScheduleAnime(AnimeInfo anime, int watchedEpisodes);
	}
}