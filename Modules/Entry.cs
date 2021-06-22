using System;
using System.Collections.Generic;
using System.Globalization;
using YO.Windows;

namespace YO.Modules
{
	/// <summary>
	/// Anime entry that contain all the necessary info about it.
	/// </summary>
	public class Entry
	{
		#region Fields

		private int _weekDay;
		private int _totalEpisodes;
		private int _watchedEpisodes;
		private int _addedEpisodes;
		private DateTime _addedDate;
		private AnimeStatus _status;

		#endregion

		#region Constructors

		/// <summary>
		/// Construct new entry from parsed data.
		/// </summary>
		/// <param name="id">MAL anime ID.</param>
		/// <param name="romajiName">English romaji title.</param>
		/// <param name="russianName">Russian translated title.</param>
		/// <param name="type">Anime type.</param>
		/// <param name="href">Hyperlink.</param>
		/// <param name="totalEpisodes">Total anime episodes.</param>
		/// <param name="watchedEpisodes">Watched episodes.</param>
		/// <param name="score">User score.</param>
		/// <param name="yearCreated">Year of creation.</param>
		/// <param name="isOngoing">Ongoing status.</param>
		/// <param name="weekDay">Day of week for regular ongoings. Use Scheduler.WNotScheduled as not set value.</param>
		public Entry(int id,
					 string romajiName,
					 string russianName,
					 string type,
					 string href,
					 int totalEpisodes,
					 int watchedEpisodes,
					 int score,
					 int yearCreated,
					 bool isOngoing,
					 int weekDay)
		{
			Id = id;
			RomajiName = romajiName;
			RussianName = russianName;
			Type = type;
			Href = href;
			_totalEpisodes = totalEpisodes;
			_addedEpisodes = _watchedEpisodes = watchedEpisodes;
			Score = score;
			YearCreated = yearCreated;
			_weekDay = weekDay;
			if (weekDay < 0)
			{
				Mode = EntryMode.AddNew;
			}

			switch (isOngoing)
			{
				case true when weekDay >= 0:
					_status = AnimeStatus.RegularOngoing;
					break;
				case true:
					_status = AnimeStatus.PendingOngoing;
					break;
				default:
					_status = AnimeStatus.Released;
					break;
			}
		}

		/// <summary>
		/// Construct from XML tag.
		/// </summary>
		/// <param name="entry">XML tag with serialized entry data.</param>
		// TODO: Remove all xml related staff from that class
		public Entry(Tag entry)
		{
			Id = entry.GetIntValue("ID");
			RomajiName = entry.GetValue("RomajiName");
			RussianName = entry.GetValue("RussianName");
			Type = entry.GetValue("Type");
			Href = entry.GetValue("Href");
			_totalEpisodes = entry.GetIntValue("TotalEpisodes");
			_watchedEpisodes = entry.GetIntValue("WatchedEpisodes");
			_weekDay = entry.GetIntValue("WeekDay");
			Period = entry.GetIntValue("Period");
			EpisodesPerDay = entry.GetIntValue("EpisodesPerDay");
			Score = entry.GetIntValue("Score");
			YearCreated = entry.GetIntValue("YearCreated");
			
			var status = entry.GetValue("Status");
			switch (status)
			{
				case "Released": 
					_status = AnimeStatus.Released;
					break;
				case "RegularOngoing": 
					_status = AnimeStatus.RegularOngoing;
					break;
				default: 
					_status = AnimeStatus.PendingOngoing;
					break;
			}

			IsExcluded = entry.IsValueTrue("IsExcluded");
			OverrideRegularOngoing = entry.IsValueTrue("OverrideRegularOngoing");
			_addedEpisodes = entry.GetIntValue("AddedEpisodes");
			_addedDate = DateTime.Parse(entry.GetValue("Added"));
		}

		#endregion

		#region Properties

		/// <summary>
		/// MAL anime ID.
		/// </summary>
		public int Id { get; }

		/// <summary>
		/// Russian translated title.
		/// </summary>
		public string RussianName { get; set; }

		/// <summary>
		/// English romaji title.
		/// </summary>
		public string RomajiName { get; set; }

		/// <summary>
		/// Anime type.
		/// </summary>
		public string Type { get; }

		/// <summary>
		/// Get days with planned episodes this week.
		/// </summary>
		public List<int> CurrentWeekDays
		{
			get
			{
				var weekStart = DateTime.Now;
				var currentWeekDay = WeekDayConverter.ToWeekDayRu(weekStart.DayOfWeek);
				return GetWeekSchedule(weekStart.AddDays(-currentWeekDay));
			}
		}

		/// <summary>
		/// Watched episodes.
		/// </summary>
		public int WatchedEpisodes
		{
			get => _watchedEpisodes;
			// TODO: Move any logic from setter to more appropriate place
			set
			{
				_watchedEpisodes = value;

				if (_watchedEpisodes > ExpectedEpisodes + Scheduler.MaxEpisodesGap
				 && _status != AnimeStatus.RegularOngoing)
				{
					_addedEpisodes += _watchedEpisodes - ExpectedEpisodes;
				}

				if (_watchedEpisodes == _totalEpisodes
				 && _status == AnimeStatus.PendingOngoing
				 && OverrideRegularOngoing)
				{
					_status = AnimeStatus.RegularOngoing;
					OverrideRegularOngoing = false;
					Period = 7;
					EpisodesPerDay = 1;
				}

				if (_watchedEpisodes >= _totalEpisodes
				 && _status == AnimeStatus.Released)
					MarkRemove();
			}
		}

		/// <summary>
		/// Total anime episodes.
		/// </summary>
		public int TotalEpisodes
		{
			get => _totalEpisodes;
			// TODO: Move any logic from setter to more appropriate place
			set
			{
				if (_status == AnimeStatus.PendingOngoing
				 && value > _totalEpisodes
				 && _watchedEpisodes == _totalEpisodes)
				{
					MarkReschedule();
				}

				_totalEpisodes = value;
			}
		}

		/// <summary>
		/// Planned episodes.
		/// </summary>
		public int ExpectedEpisodes
		{
			get
			{
				if (IsExcluded)
				{
					return WatchedEpisodes;
				}

				if (!IsRegularOngoing)
				{
					var diff = DateTime.Now.Subtract(_addedDate);
					return diff.Ticks < 0
						? _addedEpisodes
						: Math.Min(_addedEpisodes + (diff.Days / Period + 1) * EpisodesPerDay, _totalEpisodes);
				}

				// TODO: Separate condition to variable
				return ((int) DateTime.Now.DayOfWeek - _weekDay + Period) % Period < Scheduler.RealOngoingDelay
					? Math.Max(_totalEpisodes - 1, _watchedEpisodes)
					: _totalEpisodes;
			}
		}

		/// <summary>
		/// Scheduled day of week.
		/// </summary>
		public int WeekDay
		{
			get => IsExcluded
				? Scheduler.WNotScheduled
				: IsRegularOngoing
					? _weekDay
					: (int) _addedDate.DayOfWeek;
			set => _weekDay = value;
		}

		/// <summary>
		/// Scheduled day of week, ongoing delay is accounted.
		/// </summary>
		public int ActualWeekDay
			=> IsExcluded
				? Scheduler.WNotScheduled
				: (WeekDay + (IsRegularOngoing ? Scheduler.RealOngoingDelay : 0)) % 7;

		/// <summary>
		/// Release status.
		/// </summary>
		public AnimeStatus Status
		{
			get => _status;
			// TODO: Move any logic from setter to more appropriate place
			set
			{
				if (value == AnimeStatus.RegularOngoing)
				{
					Period = 7;
				}

				_status = value;
			}
		}

		/// <summary>
		/// Override regular ongoing schedule for "catching up".
		/// </summary>
		public bool OverrideRegularOngoing { get; set; }

		/// <summary>
		/// Excluded from schedule.
		/// </summary>
		public bool IsExcluded { get; set; }

		/// <summary>
		/// User score.
		/// </summary>
		public int Score { get; set; }

		/// <summary>
		/// Hyperlink.
		/// </summary>
		public string Href { get; set; }

		/// <summary>
		/// New episodes schedule period.
		/// </summary>
		public int Period { get; set; } = 7;

		/// <summary>
		/// Number of episodes to watch per day.
		/// </summary>
		public int EpisodesPerDay { get; set; } = 1;

		/// <summary>
		/// Year of creation.
		/// </summary>
		public int YearCreated { get; }

		/// <summary>
		/// Ongoing status.
		/// </summary>
		public bool IsOngoing
			=> _status == AnimeStatus.RegularOngoing
			|| _status == AnimeStatus.PendingOngoing;

		/// <summary>
		/// Regular ongoing status.
		/// </summary>
		public bool IsRegularOngoing
			=> _status == AnimeStatus.RegularOngoing;

		/// <summary>
		/// Entry action mode.
		/// </summary>
		public EntryMode Mode { get; private set; } = EntryMode.Default;

		/// <summary>
		/// Has unwatched planned episodes flag.
		/// </summary>
		public bool AreConditionsSatisfied
			=> ExpectedEpisodes <= WatchedEpisodes;

		#endregion

		#region Public Methods

		/// <summary>
		/// Save entry as XML tag.
		/// </summary>
		/// <returns>XML tag ready for caching.</returns>
		// TODO: Remove all xml related staff from that class
		public Tag Serialize()
		{
			var entry = new Tag("Anime");
			entry.SetValue("ID", Id.ToString());
			entry.SetValue("RomajiName", RomajiName);
			entry.SetValue("RussianName", RussianName);
			entry.SetValue("Type", Type);
			entry.SetValue("Href", Href);
			entry.SetValue("TotalEpisodes", _totalEpisodes.ToString());
			entry.SetValue("WatchedEpisodes", _watchedEpisodes.ToString());
			entry.SetValue("WeekDay", _weekDay.ToString());
			entry.SetValue("Period", Period.ToString());
			entry.SetValue("EpisodesPerDay", EpisodesPerDay.ToString());
			entry.SetValue("Score", Score.ToString());
			entry.SetValue("YearCreated", YearCreated.ToString());
			switch (_status)
			{
				case AnimeStatus.Released:
					entry.SetValue("Status", "Released");
					break;
				case AnimeStatus.RegularOngoing:
					entry.SetValue("Status", "RegularOngoing");
					break;
				default:
					entry.SetValue("Status", "PendingOngoing");
					break;
			}

			entry.SetValue("IsExcluded", IsExcluded.ToString());
			entry.SetValue("OverrideRegularOngoing", OverrideRegularOngoing.ToString());
			entry.SetValue("AddedEpisodes", _addedEpisodes.ToString());
			entry.SetValue("Added", _addedDate.ToString(CultureInfo.InvariantCulture));
			return entry;
		}

		/// <summary>
		/// Mark entry as not scheduled.
		/// </summary>
		public void MarkReschedule()
		{
			if (IsRegularOngoing 
			 || Mode != EntryMode.Default)
			{
				return;
			}

			var isWatched = !IsExcluded
						 && GetWeekSchedule(DateTime.Now).Contains(Convert.ToInt32(DateTime.Now.DayOfWeek))
						 && AreConditionsSatisfied;
			Mode = isWatched 
				? EntryMode.AlreadyWatched 
				: EntryMode.AddNew;
		}

		/// <summary>
		/// Mark entry for removal.
		/// </summary>
		public void MarkRemove()
		{
			Mode = EntryMode.Remove;
		}

		/// <summary>
		/// Assign new schedule.
		/// </summary>
		/// <param name="newAdded">Date indicating scheduled day.</param>
		/// <param name="satisfied">Watched episodes today flag.</param>
		public void Update(DateTime newAdded, bool satisfied = false)
		{
			Mode = EntryMode.Default;
			_addedDate = newAdded.AddHours(-newAdded.Hour)
								 .AddMinutes(-newAdded.Minute)
								 .AddSeconds(-newAdded.Second)
								 .AddMilliseconds(-newAdded.Millisecond);
			
			// TODO: Separate condition to variable
			_addedEpisodes = _watchedEpisodes -
							 (satisfied 
						   && DateTime.Now.DayOfYear == _addedDate.DayOfYear 
						   && DateTime.Now.Year == _addedDate.Year
								 ? EpisodesPerDay
								 : 0);
		}

		/// <summary>
		/// Get days with planned episodes within 7 days from starting point.
		/// </summary>
		/// <param name="weekStart">Starting point of schedule.</param>
		/// <returns></returns>
		public List<int> GetWeekSchedule(DateTime weekStart)
		{
			var days = new List<int>();
			if (IsExcluded 
			 || _status != AnimeStatus.RegularOngoing 
			 && _watchedEpisodes >= _totalEpisodes 
			 || Mode != EntryMode.Default)
			{
				return days;
			}

			if (Period % 7 == 0)
			{
				days.Add(ActualWeekDay);
				return days;
			}

			var date = _addedDate.AddMinutes(1);
			if (weekStart.Hour == 0)
			{
				weekStart = weekStart.AddHours(1);

			}
			while (weekStart.Subtract(date).Days > 0)
			{
				date = date.AddDays(Period);
			}

			while (date.Subtract(weekStart).Days < 6)
			{
				// TODO: Separate condition to variable
				if (_addedEpisodes + (date.Subtract(_addedDate).Days / Period + 1) * EpisodesPerDay <= _totalEpisodes)
				{
					days.Add((int) date.DayOfWeek);
				}
				date = date.AddDays(Period);
			}

			return days;
		}

		#endregion
	}
}