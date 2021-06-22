using System;

namespace YO.Internals.Schedule
{
	public class ScheduledEpisode
	{
		public ScheduledEpisode(DateTime scheduledTime, int episode)
		{
			ScheduledTime = scheduledTime;
			Episode = episode;
		}
		
		public DateTime ScheduledTime { get; set; }
		
		public int Episode { get; }
	}
}