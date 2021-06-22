using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace YO.Modules
{
	/// <summary>
	/// Anime scheduler class.
	/// </summary>
	public class Scheduler
	{
		// TODO: Do something with that constant
		public const int WNotScheduled = -1;

		/// <summary>
		/// Maximum episodes ahead gap to not update scheduler.
		/// </summary>
		// TODO: Do something with that constant
		public const int MaxEpisodesGap = 0;

		/// <summary>
		/// Ongoing delay parameter.
		/// </summary>
		// TODO: Make it not static
		public static int RealOngoingDelay { get; set; }

		// TODO: Move it so separate enum
		private static readonly string[] SupportedTypes = {"TV Series", "Сериал", "OVA", "ONA", "Special", "Спешл"};
		
		private readonly Dictionary<int, Entry> _data = new Dictionary<int, Entry>();
		private readonly Random _rand = new Random();
		private readonly int[] _stats = new int[7];
		private bool _recalculateStats = true;

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="realOngoingDelay">Ongoing delay parameter.</param>
		public Scheduler(int realOngoingDelay = 1)
		{
			RealOngoingDelay = realOngoingDelay;
		}

		/// <summary>
		/// Construct from XML tag.
		/// </summary>
		/// <param name="data">XML tag with serialized scheduler data.</param>
		/// <param name="realOngoingDelay">Ongoing delay parameter.</param>
		// TODO: Remove all xml related staff from that class
		public Scheduler(Tag data, int realOngoingDelay = 1) 
			: this(realOngoingDelay)
		{
			foreach (var entry in data.Content)
			{
				Add(new Entry(entry));
			}
		}

		/// <summary>
		/// Has unwatched planned episodes flag.
		/// </summary>
		public bool AreConditionsSatisfied
			=> !AnimesToWatchToday.Any();

		public int Count
			=> _data.Count;

		/// <summary>
		/// List of animes with unwatched episodes planned for today.
		/// </summary>
		public IEnumerable<Entry> AnimesToWatchToday
			=> Entries.Where(e => !e.IsExcluded
							   && !e.AreConditionsSatisfied
							   && e.CurrentWeekDays.Contains((int) DateTime.Now.DayOfWeek));

		/// <summary>
		/// List of animes with episodes planned for today.
		/// </summary>
		public IEnumerable<Entry> AnimesToday
			=> Entries.Where(e => !e.IsExcluded
							   && e.CurrentWeekDays.Contains((int) DateTime.Now.DayOfWeek));
		
		public IEnumerable<int> Ids => _data.Keys;

		public IEnumerable<Entry> Entries => _data.Values;
		
		public Entry this[int id] => _data[id];

		public static bool IsSupported(string type)
			=> SupportedTypes.Contains(type);
		
		/// <summary>
		/// Add new anime entry.
		/// </summary>
		/// <param name="entry">Anime entry.</param>
		public void Add(Entry entry)
		{
			_data.Add(entry.Id, entry);
			_recalculateStats = true;
		}

		/// <summary>
		/// Mark entry as not scheduled.
		/// </summary>
		/// <param name="id">Entry ID.</param>
		public void MarkReschedule(int id)
		{
			this[id].MarkReschedule();
			_recalculateStats = true;
		}

		/// <summary>
		/// Mark entry for removal.
		/// </summary>
		/// <param name="id">Entry ID.</param>
		public void MarkRemove(int id)
		{
			this[id].MarkRemove();
			_recalculateStats = true;
		}

		/// <summary>
		/// Schedule not scheduled entries.
		/// </summary>
		public void Schedule()
		{
			WaitIfMidnight();
			var now = DateTime.Now;
			RemoveEntries();
			foreach (var item in _data)
			{
				var entry = item.Value;
				if (entry.Mode != EntryMode.Default && !entry.IsRegularOngoing && !entry.IsExcluded)
				{
					var satisfied = entry.Mode == EntryMode.AlreadyWatched;
					var stats = GetStats();
					var min = stats.Min();
					var max = stats.Max();
					var minIds = new List<int>();
					var maxIds = new List<int>();

					for (var i = 0; i < 7; ++i)
					{
						if (stats[i] == min)
						{
							minIds.Add(i);
						}

						if (stats[i] == max)
						{
							maxIds.Add(i);
						}
					}

					var startRandomDay = _rand.Next(7);
					var info = new MinMaxInfo[entry.Period];
					for (var i = 0; i < entry.Period; ++i)
					{
						var weekDay = (startRandomDay + i) % 7;
						var addedDate = now;
						var realWeekDay = (int) addedDate.DayOfWeek;
						if (weekDay < realWeekDay)
						{
							weekDay += 7;
						}

						while (weekDay >= realWeekDay)
						{
							weekDay -= entry.Period;
						}

						weekDay += entry.Period;
						addedDate = addedDate.AddDays(weekDay - realWeekDay);
						entry.Update(addedDate, satisfied);

						info[i] = new MinMaxInfo(entry.GetWeekSchedule(now).Count(p => minIds.Contains(p)),
												 entry.GetWeekSchedule(now).Count(p => maxIds.Contains(p)),
												 addedDate);
					}

					Array.Sort(info);
					entry.Update(info[0].AddedDate, satisfied);

					_recalculateStats = true;
				}
			}
		}

		/// <summary>
		/// Reschedule all entries.
		/// </summary>
		public void Reschedule()
		{
			WaitIfMidnight();

			foreach (var item in _data)
			{
				item.Value.MarkReschedule();
			}

			Schedule();
		}

		/// <summary>
		/// Optimize periods for new entries.
		/// </summary>
		[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
		// TODO: Restore ReSharper message
		public void AssignPeriods()
		{
			var newEntries = _data.Values.Where(p => p.Mode == EntryMode.AddNew && p.WeekDay == WNotScheduled);
			var emptySlots = new List<int>();
			var currentDay = (int) DateTime.Now.DayOfWeek;
			var stats = GetStats();
			for (var i = currentDay; i < 7 + currentDay; ++i)
			{
				if (stats[i % 7] == 0)
				{
					emptySlots.Add(i);
				}
			}

			var diff = emptySlots.Count - newEntries.Count();
			if (diff > 0)
			{
				if (emptySlots.Count == 7)
				{
					foreach (var entry in newEntries)
					{
						entry.Period = newEntries.Count();
					}
				} else if (newEntries.Count() >= diff)
				{
					for (var i = 0; i < diff; ++i)
					{
						newEntries.ElementAt(i).Period = emptySlots[emptySlots.Count - diff + i] - emptySlots[i];
					}
				} else
				{
					// Don't know how to handle this, leaving everything as is may be the best solution
				}
			}
		}

		/// <summary>
		/// Save schedule as XML tag.
		/// </summary>
		/// <returns>XML tag ready for caching.</returns>
		// TODO: Remove all xml related staff from that class
		public Tag Serialize()
		{
			var data = new Tag("MyOngoings");

			foreach (var entry in _data)
			{
				data.Content.Add(entry.Value.Serialize());
			}

			return data;
		}

		/// <summary>
		/// Discard all entries.
		/// </summary>
		public void Clear()
		{
			_recalculateStats = true;
			_data.Clear();
		}

		public bool Contains(int id) => _data.ContainsKey(id);

		/// <summary>
		/// Wait if too close to midnight to avoid errors.
		/// </summary>
		private static void WaitIfMidnight()
		{
			if (DateTime.Now.Hour == 23
			 && DateTime.Now.Minute == 59
			 && DateTime.Now.Second == 59
			 && DateTime.Now.Millisecond > 800)
			{
				Thread.Sleep(210);
			}
		}

		/// <summary>
		/// Remove unused entries.
		/// </summary>
		private void RemoveEntries()
		{
			foreach (var key in _data.Where(item => item.Value.Mode == EntryMode.Remove)
									.Select(item => item.Key)
									.ToList())
			{
				_data.Remove(key);
			}
		}

		private int[] GetStats()
		{
			if (_recalculateStats)
			{
				RecalculateStats();
				return _stats;
			}

			return _stats;
		}

		/// <summary>
		/// Calculate episodes per day statistics.
		/// </summary>
		private void RecalculateStats()
		{
			var now = DateTime.Now;
			for (var i = 0; i < 7; ++i)
			{
				_stats[i] = 0;
			}

			foreach (var item in _data)
			{
				var entry = item.Value;
				var list = entry.GetWeekSchedule(now);
				foreach (var weekDay in list)
				{
					_stats[weekDay] += entry.EpisodesPerDay;
				}
			}

			_recalculateStats = false;
		}
	}
}