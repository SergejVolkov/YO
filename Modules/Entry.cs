using System;
using System.Collections.Generic;
using YO.Windows;

namespace YO.Modules
{
	/// <summary>
	/// Anime entry that contain all the necessary info about it.
	/// </summary>
	public class Entry {
		string romaji_name, russian_name, type, href;
		int id, score, year_created;
		int week_day, period = 7, episodes_per_day = 1;
		int total_episodes, watched_episodes, added_episodes;
		bool excluded = false, override_regular = false;
		AnimeStatus status;
		EntryMode mode = EntryMode.Default;
		DateTime added_date;

		/// <summary>
		/// Construct new entry from parsed data.
		/// </summary>
		/// <param name="id">MAL anime ID.</param>
		/// <param name="romaji_name">English romaji title.</param>
		/// <param name="russian_name">Russian translated title.</param>
		/// <param name="type">Anime type.</param>
		/// <param name="href">Hyperlink.</param>
		/// <param name="total_episodes">Total anime episodes.</param>
		/// <param name="watched_episodes">Watched episodes.</param>
		/// <param name="score">User score.</param>
		/// <param name="year_created">Year of creation.</param>
		/// <param name="is_ongoing">Ongoing status.</param>
		/// <param name="week_day">Day of week for regular ongoings. Use Scheduler.WNotScheduled as not set value.</param>
		public Entry(int id, string romaji_name, string russian_name, string type, string href, int total_episodes, int watched_episodes, int score, int year_created, bool is_ongoing, int week_day) {
			this.id = id;
			this.romaji_name = romaji_name;
			this.russian_name = russian_name;
			this.type = type;
			this.href = href;
			this.total_episodes = total_episodes;
			this.added_episodes = this.watched_episodes = watched_episodes;
			this.score = score;
			this.year_created = year_created;
			this.week_day = week_day;
			if (week_day < 0) {
				mode = EntryMode.AddNew;
			}
			if (is_ongoing && week_day >= 0) {
				status = AnimeStatus.RegularOngoing;
			} else if (is_ongoing) {
				status = AnimeStatus.PendingOngoing;
			} else {
				status = AnimeStatus.Released;
			}
		}

		/// <summary>
		/// Construct from XML tag.
		/// </summary>
		/// <param name="entry">XML tag with serialized entry data.</param>
		public Entry(Tag entry) {
			id = entry.GetIntValue("ID");
			romaji_name = entry.GetValue("RomajiName");
			russian_name = entry.GetValue("RussianName");
			type = entry.GetValue("Type");
			href = entry.GetValue("Href");
			total_episodes = entry.GetIntValue("TotalEpisodes");
			watched_episodes = entry.GetIntValue("WatchedEpisodes");
			week_day = entry.GetIntValue("WeekDay");
			period = entry.GetIntValue("Period");
			episodes_per_day = entry.GetIntValue("EpisodesPerDay");
			score = entry.GetIntValue("Score");
			year_created = entry.GetIntValue("YearCreated");
			if (entry.CheckValue("Status", "Released")) {
				status = AnimeStatus.Released;
			} else if (entry.CheckValue("Status", "RegularOngoing")) {
				status = AnimeStatus.RegularOngoing;
			} else {
				status = AnimeStatus.PendingOngoing;
			}
			excluded = entry.IsValueTrue("IsExcluded");
			override_regular = entry.IsValueTrue("OverrideRegularOngoing");
			added_episodes = entry.GetIntValue("AddedEpisodes");
			added_date = DateTime.Parse(entry.GetValue("Added"));
		}

		/// <summary>
		/// Save entry as XML tag.
		/// </summary>
		/// <returns>XML tag ready for caching.</returns>
		public Tag Serialize() {
			Tag entry = new Tag("Anime");
			entry.SetValue("ID", id.ToString());
			entry.SetValue("RomajiName", romaji_name);
			entry.SetValue("RussianName", russian_name);
			entry.SetValue("Type", type);
			entry.SetValue("Href", href);
			entry.SetValue("TotalEpisodes", total_episodes.ToString());
			entry.SetValue("WatchedEpisodes", watched_episodes.ToString());
			entry.SetValue("WeekDay", week_day.ToString());
			entry.SetValue("Period", period.ToString());
			entry.SetValue("EpisodesPerDay", episodes_per_day.ToString());
			entry.SetValue("Score", score.ToString());
			entry.SetValue("YearCreated", year_created.ToString());
			if (status == AnimeStatus.Released) {
				entry.SetValue("Status", "Released");
			} else if (status == AnimeStatus.RegularOngoing) {
				entry.SetValue("Status", "RegularOngoing");
			} else {
				entry.SetValue("Status", "PendingOngoing");
			}
			entry.SetValue("IsExcluded", excluded.ToString());
			entry.SetValue("OverrideRegularOngoing", override_regular.ToString());
			entry.SetValue("AddedEpisodes", added_episodes.ToString());
			entry.SetValue("Added", added_date.ToString());
			return entry;
		}

		/// <summary>
		/// Mark entry as not scheduled.
		/// </summary>
		public void MarkReschedule() {
			if (IsRegularOngoing || mode != EntryMode.Default) {
				return;
			}
			if (!excluded && GetWeekSchedule(DateTime.Now).Contains(Convert.ToInt32(DateTime.Now.DayOfWeek)) && AreConditionsSatisfied) {
				mode = EntryMode.AlreadyWatched;
			} else {
				mode = EntryMode.AddNew;
			}
		}

		/// <summary>
		/// Mark entry for removal.
		/// </summary>
		public void MarkRemove() {
			mode = EntryMode.Remove;
		}

		/// <summary>
		/// Assign new schedule.
		/// </summary>
		/// <param name="new_added">Date indicating scheduled day.</param>
		/// <param name="satisfied">Watched episodes today flag.</param>
		public void Update(DateTime new_added, bool satisfied = false) {
			mode = EntryMode.Default;
			added_date = new_added.AddHours(-new_added.Hour).AddMinutes(-new_added.Minute).AddSeconds(-new_added.Second).AddMilliseconds(-new_added.Millisecond);
			added_episodes = watched_episodes - (satisfied && DateTime.Now.DayOfYear == added_date.DayOfYear && DateTime.Now.Year == added_date.Year ? episodes_per_day : 0);
		}

		/// <summary>
		/// Get days with planned episodes within 7 days from starting point.
		/// </summary>
		/// <param name="week_start">Starting point of schedule.</param>
		/// <returns></returns>
		public List<int> GetWeekSchedule(DateTime week_start) {
			List<int> days = new List<int>();
			if (excluded || (status != AnimeStatus.RegularOngoing && watched_episodes >= total_episodes) || mode != EntryMode.Default) {
				return days;
			}
			if (period % 7 == 0) {
				days.Add(ActualWeekDay);
				return days;
			}
			DateTime date = added_date.AddMinutes(1);
			if (week_start.Hour == 0)
				week_start = week_start.AddHours(1);
			while (week_start.Subtract(date).Days > 0) {
				date = date.AddDays(period);
			}
			while (date.Subtract(week_start).Days < 6) {
				if (added_episodes + (date.Subtract(added_date).Days / period + 1) * episodes_per_day <= total_episodes)
					days.Add(Convert.ToInt32(date.DayOfWeek));
				date = date.AddDays(period);
			}
			return days;
		}

		/// <summary>
		/// Get days with planned episodes this week.
		/// </summary>
		public List<int> CurrentWeekDays {
			get {
				DateTime week_start = DateTime.Now;
				int current_week_day = Conv.ToWeekDayRu(week_start.DayOfWeek);
				return GetWeekSchedule(week_start.AddDays(-current_week_day));
			}
		}

		/// <summary>
		/// Watched episodes.
		/// </summary>
		public int WatchedEpisodes {
			get {
				return watched_episodes;
			}
			set {
				watched_episodes = value;
				if (watched_episodes > ExpectedEpisodes + Scheduler.MaxEpisodesGap && status != AnimeStatus.RegularOngoing)
					added_episodes += watched_episodes - ExpectedEpisodes;
				if (watched_episodes == total_episodes && status == AnimeStatus.PendingOngoing && override_regular) {
					status = AnimeStatus.RegularOngoing;
					override_regular = false;
					period = 7;
					episodes_per_day = 1;
				}
				if (watched_episodes >= total_episodes && status == AnimeStatus.Released)
					MarkRemove();
			}
		}

		/// <summary>
		/// Total anime episodes.
		/// </summary>
		public int TotalEpisodes {
			get {
				return total_episodes;
			}
			set {
				if (status == AnimeStatus.PendingOngoing && value > total_episodes && watched_episodes == total_episodes)
					MarkReschedule();
				total_episodes = value;
			}
		}

		/// <summary>
		/// Planned episodes.
		/// </summary>
		public int ExpectedEpisodes {
			get {
				if (IsExcluded) {
					return WatchedEpisodes;
				}
				if (!IsRegularOngoing) {
					TimeSpan diff = DateTime.Now.Subtract(added_date);
					if (diff.Ticks < 0) {
						return added_episodes;
					}
					return Math.Min(added_episodes + (diff.Days / period + 1) * episodes_per_day, total_episodes);
				}
				if ((Convert.ToInt32(DateTime.Now.DayOfWeek) - week_day + period) % period < Scheduler.RealOngoingDelay) {
					return Math.Max(total_episodes - 1, watched_episodes);
				}
				return total_episodes;
			}
		}

		/// <summary>
		/// Scheduled day of week.
		/// </summary>
		public int WeekDay {
			get {
				if (excluded)
					return Scheduler.WNotScheduled;
				if (IsRegularOngoing)
					return week_day;
				return Convert.ToInt32(added_date.DayOfWeek);
			}
			set {
				week_day = value;
			}
		}

		/// <summary>
		/// Scheduled day of week, ongoing delay is accounted.
		/// </summary>
		public int ActualWeekDay {
			get {
				if (excluded)
					return Scheduler.WNotScheduled;
				return (WeekDay + (IsRegularOngoing ? Scheduler.RealOngoingDelay : 0)) % 7;
			}
		}

		/// <summary>
		/// Release status.
		/// </summary>
		public AnimeStatus Status {
			get {
				return status;
			}
			set {
				if (value == AnimeStatus.RegularOngoing) {
					period = 7;
				}
				status = value;
			}
		}

		/// <summary>
		/// Override regular ongoing schedule for "catching up".
		/// </summary>
		public bool OverrideRegularOngoing {
			get {
				return override_regular;
			}
			set {
				override_regular = value;
			}
		}

		/// <summary>
		/// Excluded from schedule.
		/// </summary>
		public bool IsExcluded {
			get {
				return excluded;
			}
			set {
				excluded = value;
			}
		}

		/// <summary>
		/// User score.
		/// </summary>
		public int Score {
			get {
				return score;
			}
			set {
				score = value;
			}
		}

		/// <summary>
		/// English romaji title.
		/// </summary>
		public string RomajiName {
			get {
				return romaji_name;
			}
			set {
				romaji_name = value;
			}
		}

		/// <summary>
		/// Russian translated title.
		/// </summary>
		public string RussianName {
			get {
				return russian_name;
			}
			set {
				russian_name = value;
			}
		}

		/// <summary>
		/// Hyperlink.
		/// </summary>
		public string Href {
			get {
				return href;
			}
			set {
				href = value;
			}
		}

		/// <summary>
		/// New episodes schedule period.
		/// </summary>
		public int Period {
			get {
				return period;
			}
			set {
				period = value;
			}
		}

		/// <summary>
		/// Number of episodes to watch per day.
		/// </summary>
		public int EpisodesPerDay {
			get {
				return episodes_per_day;
			}
			set {
				episodes_per_day = value;
			}
		}

		/// <summary>
		/// MAL anime ID.
		/// </summary>
		public int ID => id;
		/// <summary>
		/// Year of creation.
		/// </summary>
		public int YearCreated => year_created;
		/// <summary>
		/// Anime type.
		/// </summary>
		public string Type => type;
		/// <summary>
		/// Ongoing status.
		/// </summary>
		public bool IsOngoing => status == AnimeStatus.RegularOngoing || status == AnimeStatus.PendingOngoing;
		/// <summary>
		/// Regular ongoing status.
		/// </summary>
		public bool IsRegularOngoing => status == AnimeStatus.RegularOngoing;
		/// <summary>
		/// Entry action mode.
		/// </summary>
		public EntryMode Mode => mode;
		/// <summary>
		/// Has unwatched planned episodes flag.
		/// </summary>
		public bool AreConditionsSatisfied => ExpectedEpisodes <= WatchedEpisodes;
	}
}