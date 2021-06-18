using System.Collections.Generic;
using System.Threading.Tasks;
using YO.Internals.Shikimori.Data;

namespace YO.Internals.Schedule
{
	public interface IScheduler
	{
		public IReadOnlyCollection<ScheduledEpisode> ScheduledEntries { get; }
		public Task ScheduleAnime(AnimeInfo anime);
		public Task ScheduleAnime(UserRate rate);
		public void UpdateSchedule();
	}
}