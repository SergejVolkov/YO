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

    /// <summary>
    /// Helper struct for storing schedule and stats params.
    /// </summary>
    struct MinMaxInfo {
        private int contains_min, contains_max;
        DateTime added_date;

        public MinMaxInfo(int contains_min, int contains_max, DateTime added_date) {
            this.contains_min = contains_min;
            this.contains_max = contains_max;
            this.added_date = added_date;
        }

        public int ContainsMin => contains_min;
        public int ContainsMax => contains_max;
        public DateTime AddedDate => added_date;
    }

    /// <summary>
    /// Anime release status.
    /// </summary>
    public enum AnimeStatus {
        Released,
        RegularOngoing,
        PendingOngoing
    };

    /// <summary>
    /// Entry action mode.
    /// </summary>
    public enum EntryMode {
        Default,
        AddNew,
        AlreadyWatched,
        Remove
    };

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
