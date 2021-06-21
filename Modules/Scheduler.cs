using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace YO.Modules {
    /// <summary>
    /// Anime scheduler class.
    /// </summary>
    public class Scheduler {
        static string[] supported_types = { "TV Series", "Сериал", "OVA", "ONA", "Special", "Спешл" };
        public static int WNotScheduled => -1;

        /// <summary>
        /// Ongoing delay parameter.
        /// </summary>
        public static int RealOngoingDelay;
        /// <summary>
        /// Maximum episodes ahead gap to not update scheduler.
        /// </summary>
        public static int MaxEpisodesGap = 0;

        Random rand = new Random();
        int[] stats = new int[7];
        bool recalc_stats = true;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="real_ongoing_delay">Ongoing delay parameter.</param>
        public Scheduler(int real_ongoing_delay = 1) {
            RealOngoingDelay = real_ongoing_delay;
        }

        /// <summary>
        /// Construct from XML tag.
        /// </summary>
        /// <param name="data">XML tag with serialized scheduler data.</param>
        /// <param name="real_ongoing_delay">Ongoing delay parameter.</param>
        public Scheduler(Tag data, int real_ongoing_delay = 1) {
            RealOngoingDelay = real_ongoing_delay;
            foreach (var entry in data.Content) {
                Add(new Entry(entry));
            }
        }

        /// <summary>
        /// Add new anime entry.
        /// </summary>
        /// <param name="entry">Anime entry.</param>
        public void Add(Entry entry) {
            Data.Add(entry.ID, entry);
            recalc_stats = true;
        }

        /// <summary>
        /// Mark entry as not scheduled.
        /// </summary>
        /// <param name="id">Entry ID.</param>
        public void MarkReschedule(int id) {
            this[id].MarkReschedule();
            recalc_stats = true;
        }

        /// <summary>
        /// Mark entry for removal.
        /// </summary>
        /// <param name="id">Entry ID.</param>
        public void MarkRemove(int id) {
            this[id].MarkRemove();
            recalc_stats = true;
        }

        /// <summary>
        /// Remove unused entries.
        /// </summary>
        void RemoveEntries() {
            List<int> to_remove = new List<int>();
            foreach (var item in Data) {
                if (item.Value.Mode == EntryMode.Remove)
                    to_remove.Add(item.Key);
            }
            foreach (var key in to_remove) {
                Data.Remove(key);
            }
        }

        /// <summary>
        /// Schedule not scheduled entries.
        /// </summary>
        public void Schedule() {
            WaitIfMidnight();
            DateTime now = DateTime.Now;
            RemoveEntries();
            foreach (var item in Data) {
                var entry = item.Value;
                if (entry.Mode != EntryMode.Default && !entry.IsRegularOngoing && !entry.IsExcluded) {
                    bool satisfied = entry.Mode == EntryMode.AlreadyWatched;
                    int min = Stats.Min(), max = Stats.Max();
                    List<int> min_idxs = new List<int>();
                    List<int> max_idxs = new List<int>();
                    for (int i = 0; i < 7; ++i) {
                        if (Stats[i] == min) min_idxs.Add(i);
                        if (Stats[i] == max) max_idxs.Add(i);
                    }
                    int start_random_day = rand.Next(7);
                    MinMaxInfo[] info = new MinMaxInfo[entry.Period];
                    for (int i = 0; i < entry.Period; ++i) {
                        int week_day = (start_random_day + i) % 7;
                        DateTime added_date = now;
                        int real_week_day = Convert.ToInt32(added_date.DayOfWeek);
                        if (week_day < real_week_day) week_day += 7;
                        while (week_day >= real_week_day) week_day -= entry.Period;
                        week_day += entry.Period;
                        added_date = added_date.AddDays(week_day - real_week_day);
                        entry.Update(added_date, satisfied);

                        info[i] = new MinMaxInfo(entry.GetWeekSchedule(now).Count(p => min_idxs.Contains(p)), entry.GetWeekSchedule(now).Count(p => max_idxs.Contains(p)), added_date);
                    }
                    Array.Sort(info, delegate (MinMaxInfo a, MinMaxInfo b) {
                        if (a.ContainsMax == b.ContainsMax) {
                            if (a.ContainsMin == b.ContainsMin)
                                return 0;
                            if (a.ContainsMin > b.ContainsMin)
                                return -1;
                            return 1;
                        }
                        if (a.ContainsMax < b.ContainsMax)
                            return -1;
                        return 1;
                    });
                    entry.Update(info[0].AddedDate, satisfied);

                    recalc_stats = true;
                }
            }
        }

        /// <summary>
        /// Reschedule all entries.
        /// </summary>
        public void Reschedule() {
            WaitIfMidnight();
            foreach (var item in Data) {
                item.Value.MarkReschedule();
            }
            Schedule();
        }

        /// <summary>
        /// Optimize periods for new entries.
        /// </summary>
        public void AssignPeriods() {
            var new_entries = Data.Values.Where(p => p.Mode == EntryMode.AddNew && p.WeekDay == WNotScheduled);
            List<int> empty_slots = new List<int>();
            int current_day = Convert.ToInt32(DateTime.Now.DayOfWeek);
            for (int i = current_day; i < 7 + current_day; ++i) {
                if (Stats[i % 7] == 0) {
                    empty_slots.Add(i);
                }
            }
            int diff = empty_slots.Count - new_entries.Count();
            if (diff > 0) {
                if (empty_slots.Count == 7) {
                    foreach (var entry in new_entries) {
                        entry.Period = new_entries.Count();
                    }
                } else if (new_entries.Count() >= diff) {
                    for (int i = 0; i < diff; ++i) {
                        new_entries.ElementAt(i).Period = empty_slots[empty_slots.Count - diff + i] - empty_slots[i];
                    }
                } else {
                    // Don't know how to handle this, leaving everything as is may be the best solution
                }
            }
        }

        /// <summary>
        /// Calculate episodes per day statistics.
        /// </summary>
        private void RecalcStats() {
            DateTime now = DateTime.Now;
            for (int i = 0; i < 7; ++i) {
                stats[i] = 0;
            }
            foreach (var item in Data) {
                var entry = item.Value;
                var list = entry.GetWeekSchedule(now);
                foreach (var week_day in list) {
                    stats[week_day] += entry.EpisodesPerDay;
                }
            }
            recalc_stats = false;
        }

        /// <summary>
        /// Save schedule as XML tag.
        /// </summary>
        /// <returns>XML tag ready for caching.</returns>
        public Tag Serialize() {
            Tag data = new Tag("MyOngoings");
            foreach (var entry in this.Data) {
                data.Content.Add(entry.Value.Serialize());
            }
            return data;
        }

        /// <summary>
        /// Discard all entries.
        /// </summary>
        public void Clear() {
            recalc_stats = true;
            Data.Clear();
        }

        /// <summary>
        /// Wait if too close to midnight to avoid errors.
        /// </summary>
        static void WaitIfMidnight() {
            if (DateTime.Now.Hour == 23 && DateTime.Now.Minute == 59 && DateTime.Now.Second == 59 && DateTime.Now.Millisecond > 800) {
                Thread.Sleep(210);
            }
        }

        static public bool IsSupported(string type) => supported_types.Contains(type);
        /// <summary>
        /// Has unwatched planned episodes flag.
        /// </summary>
        public bool AreConditionsSatisfied => AnimesToWatchToday.Count == 0;
        public bool Contains(int id) => Data.ContainsKey(id);
        public int Count => Data.Count;
        /// <summary>
        /// List of animes with unwatched episodes planned for today.
        /// </summary>
        public List<Entry> AnimesToWatchToday => new List<Entry>(Entries).FindAll(p => !p.IsExcluded && !p.AreConditionsSatisfied && p.CurrentWeekDays.Contains(Convert.ToInt32(DateTime.Now.DayOfWeek)));
        /// <summary>
        /// List of animes with episodes planned for today.
        /// </summary>
        public List<Entry> AnimesToday => new List<Entry>(Entries).FindAll(p => !p.IsExcluded && p.CurrentWeekDays.Contains(Convert.ToInt32(DateTime.Now.DayOfWeek)));
        public Entry this[int id] => Data[id];
        public Dictionary<int, Entry>.KeyCollection IDs => Data.Keys;
        public Dictionary<int, Entry>.ValueCollection Entries => Data.Values;
        public Dictionary<int, Entry> Data { get; } = new Dictionary<int, Entry>();

        private int[] Stats {
            get {
                if (recalc_stats) {
                    RecalcStats();
                    return stats;
                }
                return stats;
            }
        }
    }
}
