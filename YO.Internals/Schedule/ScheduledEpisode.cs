using System;
using YO.Internals.Shikimori.Data;

namespace YO.Internals.Schedule
{
	public class ScheduledEpisode
	{
		public ScheduledEpisode(AnimeInfo anime, 
								DateTime scheduledTime, 
								int episode)
		{
			Anime = anime;
			ScheduledTime = scheduledTime;
			Episode = episode;
		}

		public AnimeInfo Anime { get; }
		
		public DateTime ScheduledTime { get; set; }
		
		public int Episode { get; }
	}
}