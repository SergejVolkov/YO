using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Reflection;
using YO.Modules;

namespace YO {
    /// <summary>
    /// Basic app states.
    /// </summary>
    enum Mode {
        Normal,
        SignIn,
        Refresh,
        Offline
    };

    /// <summary>
    /// Language of anime titles.
    /// </summary>
    public enum TitleLanguage {
        Russian,
        Romaji
    };

    /// <summary>
    /// Sorting of animes in calendar slots and list.
    /// </summary>
    enum SortingMode {
        Score,
        Year,
        Progress,
        Alphabet
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        // String resources
        static string[] month_ru =
        {
            "января",
            "февраля",
            "марта",
            "апреля",
            "мая",
            "июня",
            "июля",
            "августа",
            "сентября",
            "октября",
            "ноября",
            "декабря",
        };
        static string[] day_ru =
        {
            "Понедельник",
            "Вторник",
            "Среда",
            "Четверг",
            "Пятница",
            "Суббота",
            "Воскресенье"
        };
        static string[] backgrounds =
        {
            "gatari_1-2_1500",
            "klk",
            "lwa",
            "madoka",
            "mahoromatic",
            "sasami_1-4_500",
            "sns",
            "uy"
        };

        // Delays, periods, intervals
        static int cover_delay = 300 /* milliseconds */, anime_page_delay = 500 /* milliseconds */, too_many_requests_delay = 1500 /* milliseconds */, additional_sync_sleep = 1500 /* milliseconds */, refresh_interval = 5 /* minutes */, evening_hour = 20 /* hours */, full_refresh_period = 1 /* days */;
        
        // Children indexes to get things done with UI
        const int cover_button_idx = 1, cover_title_idx = 2, list_item_checkbox_idx = 0, list_item_progress_idx = 2, list_item_period_idx = 4, list_item_title_idx = 5;

        // Cached app params
        bool autorun = false, dark_mode = false, week_start_now = false, full_refreshed = false, start_notified = false, evening_notified = false;
        DateTime last_full_refresh, last_startup_notification, last_evening_notification;
        int real_ongoing_delay, total_episodes;
        TitleLanguage language;
        SortingMode sorting_mode;
        // Urls
        string shiki_url = "https://shikimori.one/", img_url = "https://kawai.shikimori.one/system/animes/original/", watching_score_part_url = "/list/anime/mylist/watching,rewatching/order-by/rate_score", watching_year_part_url = "/list/anime/mylist/watching,rewatching/order-by/aired_on", watching_progress_part_url = "/list/anime/mylist/watching,rewatching/order-by/episodes", watching_alphabet_part_url = "/list/anime/mylist/watching,rewatching/order-by/name", watching_part_url, watching_url, account_name;
        // Resources dirs
        static string appdata, tmpdir, res_dir = "pack://application:,,,/Resources/", cacheres_dir = res_dir + "cache/", ui_img_dir = res_dir + "img/";
        // Notification icon tooltips
        static string normal_status = "YO: Твои Онгоинги", no_connection_status = "YO: Оффлайн", busy_status = "YO: Синхронизация...";
        // Cache xml tags
        static Tag preferences, data;

        // Scheduler, timer, and program instance objects
        Scheduler scheduler;
        DispatcherTimer refresh_timer;
        DispatcherTimer animation_timer;
        FileStream instance_stream;

        // UI elements, images, brushes
        System.Windows.Forms.NotifyIcon ni;
        System.Drawing.Icon normal_icon, no_connection_icon, busy_icon;
        WrapPanel[] slots;
        Grid[] borders, nothing_here_placeholders;
        Dictionary<int, Poster> posters;
        List<Grid>[] calendar_items;
        List<Grid> list_view_items;
        Dictionary<int, bool> excluded_this_run;
        List<Brush> background_key_frames;
        BitmapSource edit_icon_image;
        Brush edit_button_normal_brush, edit_button_active_brush, green_highlight_brush, yellow_highlight_brush, red_highlight_brush, gray_highlight_brush, link_normal_brush, link_active_brush, main_font_brush, sec_font_brush, inactive_textbox_brush;
        
        // Not cached app params
        Mode mode = Mode.Normal;
        bool list_mode = false, running = true, excluded_all, excluding_task_running = false, sync_mode = false;
        int uid_counter = 0, current_frame = 0;
        // UI sizes and gaps
        double si_width = 520, si_height = 235, norm_width = 1070, norm_height = 600, slot_height = 300, slot_border_height = 45, slot_title_height = 50, shadow_blur_radius = 12, slot_horiz_gap = 18, slot_vert_gap = 12, max_poster_aspect_ratio = 0.75, edit_icon_height = 33, edit_icon_gap = 3;

        // Comparers
        delegate int EntryComparer(Entry a, Entry b);
        delegate int UIElementComparer(Grid a, Grid b);

        // Caching tools, save-read preferences and data
        #region Cache
        /// <summary>
        /// Check if cache files exist and create default files otherwise.
        /// </summary>
        /// <returns>Flag indicating whether the app runs for the first time.</returns>
        public static bool CheckFirstRun() {
            bool reply = false;
            if (!File.Exists(appdata + "preferences") && !File.Exists(appdata + "data")) {
                FirstRun();
                reply = true;
            } else if (!File.Exists(appdata + "preferences")) {
                CopyCache("preferences");
            } else if (!File.Exists(appdata + "data")) {
                CopyCache("data");
            }
            if (!Directory.Exists(tmpdir)) {
                Directory.CreateDirectory(tmpdir);
            }
            preferences = ReadCache("preferences");
            data = ReadCache("data");
            return reply;
        }

        /// <summary>
        /// Create default cache files.
        /// </summary>
        public static void FirstRun() {
            Directory.CreateDirectory(tmpdir);
            CopyCache("preferences");
            CopyCache("data");
        }

        /// <summary>
        /// Load cache file from disk.
        /// </summary>
        /// <param name="key">Cache file name.</param>
        /// <returns>Loaded cache xml tag.</returns>
        public static Tag ReadCache(string key) {
            TagReader reader;
            try { reader = new TagReader(appdata + key); } catch {
                CopyCache(key);
                reader = new TagReader(appdata + key);
            }
            try {
                var cache = reader.Read()[0];
                reader.Dispose();
                if (cache.Name.ToLower() != key) throw new OperationCanceledException();
                return cache;
            } catch {
                throw new CacheFileCorruptedException(key);
            }
        }

        /// <summary>
        /// Copy any resource to AppData folder.
        /// </summary>
        /// <param name="resource">Internal resource file.</param>
        /// <param name="path">Destination path.</param>
        private static void CopyRes(string resource, string path) {
            var resourceInfo = Application.GetResourceStream(new Uri(resource));
            var cachestream = new FileStream(path, FileMode.Create);
            resourceInfo.Stream.CopyTo(cachestream);
            cachestream.Flush();
            cachestream.Dispose();
            resourceInfo.Stream.Dispose();
        }

        /// <summary>
        /// Copy default cache file to AppData folder.
        /// </summary>
        /// <param name="key">Cache file name.</param>
        private static void CopyCache(string key) => CopyRes(cacheres_dir + key, appdata + key);

        /// <summary>
        /// Copy image resource to AppData folder.
        /// </summary>
        /// <param name="key">Image resource name.</param>
        /// <param name="path">Destination path.</param>
        private static void CopyImgRes(string key, string path) => CopyRes(ui_img_dir + key, path);

        /// <summary>
        /// Save cache to disk.
        /// </summary>
        /// <param name="cache">Xml tag with cache content.</param>
        public static void WriteCache(Tag cache) {
            TagWriter writer = new TagWriter(appdata + cache.Name.ToLower());
            writer.WriteTag(cache);
            writer.Dispose();
        }

        /// <summary>
        /// Update and save preferences to disk.
        /// </summary>
        void UpdatePrefCache() {
            if (this.WindowState == WindowState.Maximized)
                preferences.SetValue("WindowState", "Maximized");
            else
                preferences.SetValue("WindowState", "Normal");

            if (sorting_mode == SortingMode.Score)
                preferences.SetValue("Sorting", "Score");
            else if (sorting_mode == SortingMode.Year)
                preferences.SetValue("Sorting", "Year");
            else if (sorting_mode == SortingMode.Progress)
                preferences.SetValue("Sorting", "Progress");
            else
                preferences.SetValue("Sorting", "Alphabet");

            if (week_start_now)
                preferences.SetValue("StartWeekFrom", "Now");
            else
                preferences.SetValue("StartWeekFrom", "Monday");

            preferences.SetValue("Autorun", autorun.ToString());

            if (language == TitleLanguage.Russian)
                preferences.SetValue("Language", "Russian");
            else
                preferences.SetValue("Language", "Romaji");

            if (dark_mode)
                preferences.SetValue("Theme", "Dark");
            else
                preferences.SetValue("Theme", "Light");

            preferences.SetValue("RealOngoingDelay", real_ongoing_delay.ToString());
            preferences.SetValue("LastFullRefresh", last_full_refresh.ToString());
            preferences.SetValue("LastStartupNotification", last_startup_notification.ToString());
            preferences.SetValue("LastEveningNotification", last_evening_notification.ToString());
            WriteCache(preferences);
        }

        /// <summary>
        /// Update and save data to disk.
        /// </summary>
        void UpdateDataCache() {
            data.GetContent("AccountInfo").SetValue("Name", account_name);
            data.GetContent("StatsInfo").SetValue("TotalEpisodes", total_episodes.ToString());
            if (account_name != "") {
                var schedule_tag = scheduler.Serialize();
                schedule_tag.SetValue("Account", account_name);
                data.Content.RemoveAll(p => p.Name == "MyOngoings" && p.CheckValue("Account", account_name));
                data.Content.Add(schedule_tag);
            }
            WriteCache(data);
        }

        /// <summary>
        /// Update and save everything to disk.
        /// </summary>
        void UpdateCache() {
            UpdatePrefCache();
            UpdateDataCache();
        }

        /// <summary>
        /// Load schedule from cache.
        /// </summary>
        void LoadSchedule() {
            scheduler = new Scheduler(data.Content.Find(p => p.Name == "MyOngoings" && p.CheckValue("Account", account_name)), real_ongoing_delay);
        }
        #endregion

        // Window constructor parts
        #region Prepare
        /// <summary>
        /// Prevent app from having two copies running concurrently.
        /// </summary>
        void PrepareInstance() {
            if (CheckInstance()) {
                running = false;
                this.Close();
            }
        }

        /// <summary>
        /// Load UI elements.
        /// </summary>
        void PrepareUI() {
            edit_icon_image = new BitmapImage(new Uri(ui_img_dir + "pencil.png"));
            posters = new Dictionary<int, Poster>();
            list_view_items = new List<Grid>();
            excluded_this_run = new Dictionary<int, bool>();
            calendar_items = new List<Grid>[7];
            slots = new WrapPanel[7];
            nothing_here_placeholders = new Grid[7];
            borders = new Grid[7];
            for (int i = 0; i < 7; ++i) {
                calendar_items[i] = new List<Grid>();
                nothing_here_placeholders[i] = GetPlaceholder();
                borders[i] = new Grid {
                    Height = slot_border_height
                };
                borders[i].ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(slot_horiz_gap) });
                borders[i].ColumnDefinitions.Add(new ColumnDefinition());
                Label weekday = new Label {
                    FontSize = 30,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Bottom
                };
                Label ldate = new Label {
                    FontSize = mainWindow.FontSize,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetColumn(weekday, 1);
                Grid.SetColumn(ldate, 1);
                borders[i].Children.Add(weekday);
                borders[i].Children.Add(ldate);
                slots[i] = new WrapPanel() {
                    MinHeight = slot_height,
                    Orientation = Orientation.Horizontal
                };
                calendarPanel.Children.Add(borders[i]);
                calendarPanel.Children.Add(slots[i]);
                calendarPanel.Children.Add(nothing_here_placeholders[i]);
            }
            listPlaceholder.Children.Add(GetPlaceholder());
            listPlaceholder = (Grid)listPlaceholder.Children[0];
            listTopBar.Margin = new Thickness(0, 0, SystemParameters.VerticalScrollBarWidth, 0);

            PrepareTrayIcon();
            Commands.Init();
        }

        /// <summary>
        /// Create notification icon.
        /// </summary>
        void PrepareTrayIcon() {
            normal_icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri(ui_img_dir + "ico/normal_ni.ico")).Stream);
            no_connection_icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri(ui_img_dir + "ico/offline_ni.ico")).Stream);
            busy_icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri(ui_img_dir + "ico/busy_ni.ico")).Stream); ;
            ni = new System.Windows.Forms.NotifyIcon {
                Icon = normal_icon,
                Text = normal_status,
                Visible = true
            };
            ni.DoubleClick += OpenNIClick;
            System.Windows.Forms.MenuItem[] ni_items = new System.Windows.Forms.MenuItem[] {
                new System.Windows.Forms.MenuItem("Развернуть",  OpenNIClick),
                new System.Windows.Forms.MenuItem("Открыть Шикимори...",  ShikiNIClick),
                new System.Windows.Forms.MenuItem("Выйти",  ExitNIClick)
            };
            ni.ContextMenu = new System.Windows.Forms.ContextMenu(ni_items);
            ni.BalloonTipClicked += OpenNIClick;
        }

        /// <summary>
        /// Load cache and apply preferences.
        /// </summary>
        void PrepareCache() {
            if (CheckFirstRun()) {
                InstallOnStartUp();
                preferences.SetValue("Autorun", autorun.ToString());
            }
            SetWindowState();
            if (preferences.CheckValue("Sorting", "Score")) {
                sorting_mode = SortingMode.Score;
                watching_part_url = watching_score_part_url;
                scoreSortBarItem.IsChecked = true;
                CheckSortButton(buttonSortScore);
            } else if (preferences.CheckValue("Sorting", "Year")) {
                sorting_mode = SortingMode.Year;
                watching_part_url = watching_year_part_url;
                yearSortBarItem.IsChecked = true;
                CheckSortButton(buttonSortYear);
            } else if (preferences.CheckValue("Sorting", "Progress")) {
                sorting_mode = SortingMode.Progress;
                watching_part_url = watching_progress_part_url;
                progressSortBarItem.IsChecked = true;
                CheckSortButton(buttonSortProgress);
            } else {
                sorting_mode = SortingMode.Alphabet;
                watching_part_url = watching_alphabet_part_url;
                alphabetSortBarItem.IsChecked = true;
                CheckSortButton(buttonSortTitle);
            }
            if (preferences.CheckValue("Language", "Russian")) {
                language = TitleLanguage.Russian;
            } else {
                language = TitleLanguage.Romaji;
                languageBarItem.IsChecked = true;
            }
            weekStartBarItem.IsChecked = week_start_now = preferences.CheckValue("StartWeekFrom", "Now");
            darkBarItem.IsChecked = dark_mode = preferences.CheckValue("Theme", "Dark");
            startupBarItem.IsChecked = autorun = preferences.IsValueTrue("Autorun");
            real_ongoing_delay = preferences.GetIntValue("RealOngoingDelay");
            last_full_refresh = DateTime.Parse(preferences.GetValue("LastFullRefresh"));
            full_refreshed = DateTime.Now.Subtract(last_full_refresh).Days < full_refresh_period;
            last_startup_notification = DateTime.Parse(preferences.GetValue("LastStartupNotification"));
            start_notified = DateTime.Now.DayOfYear == last_startup_notification.DayOfYear && DateTime.Now.Year == last_startup_notification.Year;
            last_evening_notification = DateTime.Parse(preferences.GetValue("LastEveningNotification"));
            evening_notified = DateTime.Now.DayOfYear == last_evening_notification.DayOfYear && DateTime.Now.Year == last_evening_notification.Year;

            account_name = data.GetContent("AccountInfo").GetValue("Name");
            UpdateWatchingUrl();
            total_episodes = data.GetContent("StatsInfo").GetIntValue("TotalEpisodes");
            if (account_name != "") {
                try {
                    LoadSchedule();
                } catch {
                    account_name = "";
                    scheduler = new Scheduler(real_ongoing_delay);
                }
            } else {
                scheduler = new Scheduler(real_ongoing_delay);
            }
        }

        /// <summary>
        /// Start minimized if run by system, create timers.
        /// </summary>
        void PrepareSystem() {
            if (Environment.CurrentDirectory.ToLower().EndsWith("system32")) {
                this.Visibility = Visibility.Hidden;
                this.ShowInTaskbar = false;
            }
            refresh_timer = new DispatcherTimer {
                Interval = new TimeSpan(0, refresh_interval, 0)
            };
            refresh_timer.Tick += Refresh_Timer_Tick;
        }

        /// <summary>
        /// Apply settings from cache to UI.
        /// </summary>
        void PrepareCachedUI() {
            if (dark_mode) DarkMode();
            else AssignLightModeDependecies();
            if (account_name == "") {
                SignInMode();
            } else {
                UpdateUI();
            }
        }
        #endregion

        /// <summary>
        /// App constructor, check if already running, load preferences and display schedule.
        /// </summary>
        public MainWindow() {
            InitializeComponent();

            appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\YO\\";
            tmpdir = appdata + "tmp\\";

            PrepareInstance();
            PrepareUI();
            PrepareCache();
            PrepareSystem();
            refresh_timer.Start();

            PrepareCachedUI();
            RefreshAsync();
        }

        // Event handlers from UI elements and timers are stored here. Documenting them is unnecessary (I'm too lazy to do this actually)
        #region EventHandlers
        private void HelpBarItem_Click(object sender, RoutedEventArgs e) {
            AboutWindow window = new AboutWindow(dark_mode, false, "Помощь", "Справка", StringResources.Help, StringResources.Notice, 1000);
            window.ResizeMode = ResizeMode.CanResize;
            window.SizeToContent = SizeToContent.Manual;
            window.Height = 600;
            window.Owner = this;
            window.Show();
        }

        private void AboutBarItem_Click(object sender, RoutedEventArgs e) {
            AboutWindow window = new AboutWindow(dark_mode, true, "О программе", "YO: Твои Онгоинги", StringResources.About, StringResources.Notice);
            window.Owner = this;
            window.ShowDialog();
        }

        private void CopyrightBarItem_Click(object sender, RoutedEventArgs e) {
            AboutWindow window = new AboutWindow(dark_mode, false, "Правообладателям", "For Right Holders", StringResources.Copyright, StringResources.Notice, 600);
            window.Owner = this;
            window.ShowDialog();
        }

        private void ExcludeBarItem_Click(object sender, RoutedEventArgs e) {
            refresh_timer.Stop();
            SelectIntDialog dialog = new SelectIntDialog(dark_mode, "Исключить просроченные", "Исключенные аниме больше не будут показываться в расписании.\n\nИсключить все просроченные", 0, 3, -1, new string[] { "на 1 эпизод и более", "на 2 эпизода и более", "на 5 эпизодов и более", "на 10 эпизодов и более" });
            dialog.Owner = this;
            dialog.ShowDialog();
            if (dialog.DialogResult == true) {
                if (mode == Mode.Refresh) {
                    System.Windows.Forms.MessageBox.Show("Пожалуйста, дождитесь завершения синхронизации и попробуйте снова!");
                } else {
                    int threshold = new int[] { 1, 2, 5, 10 }[dialog.Value];
                    var overdue = scheduler.Entries.Where(p => !p.IsExcluded && p.ExpectedEpisodes - p.WatchedEpisodes >= threshold && (!p.CurrentWeekDays.Contains(Convert.ToInt32(DateTime.Now.DayOfWeek)) || p.ExpectedEpisodes - p.WatchedEpisodes > p.EpisodesPerDay));
                    var overdue_checkboxes = list_view_items.FindAll(p => overdue.Any(q => q.ID == GetID(p))).ConvertAll(p => (CheckBox)p.Children[list_item_checkbox_idx]);
                    foreach (var box in overdue_checkboxes)
                        box.IsChecked = false;
                    UpdateCalendar();
                    UpdateDataCache();
                }
            }
            refresh_timer.Start();
        }

        private void ResetBarItem_Click(object sender, RoutedEventArgs e) {
            refresh_timer.Stop();
            SelectIntDialog dialog = new SelectIntDialog(dark_mode, "Сбросить просроченные", "Пересчитать расписание просроченных тайтлов.\n\nСбросить все просроченные", 0, 3, -1, new string[] { "на 1 эпизод и более", "на 2 эпизода и более", "на 5 эпизодов и более", "на 10 эпизодов и более" });
            dialog.Owner = this;
            dialog.ShowDialog();
            if (dialog.DialogResult == true) {
                if (mode == Mode.Refresh) {
                    System.Windows.Forms.MessageBox.Show("Пожалуйста, дождитесь завершения синхронизации и попробуйте снова!");
                } else {
                    int threshold = new int[] { 1, 2, 5, 10 }[dialog.Value];
                    var overdue = scheduler.Entries.Where(p => !p.IsExcluded && !p.IsRegularOngoing && p.ExpectedEpisodes - p.WatchedEpisodes >= threshold && (!p.CurrentWeekDays.Contains(Convert.ToInt32(DateTime.Now.DayOfWeek)) || p.ExpectedEpisodes - p.WatchedEpisodes > p.EpisodesPerDay));
                    foreach (var entry in overdue)
                        entry.MarkReschedule();
                    scheduler.Schedule();
                    UpdateCalendar();
                    UpdateDataCache();
                }
            }
            refresh_timer.Start();
        }

        private void RescheduleBarItem_Click(object sender, RoutedEventArgs e) {
            if (mode == Mode.Refresh) {
                System.Windows.Forms.MessageBox.Show("Пожалуйста, дождитесь завершения синхронизации и попробуйте снова!");
                return;
            }
            scheduler.Reschedule();
            UpdateCalendar();
            UpdateDataCache();
        }

        private void StartupBarItem_Click(object sender, RoutedEventArgs e) {
            if (!autorun) {
                InstallOnStartUp();
            } else {
                UnInstallOnStartUp();
            }
            UpdatePrefCache();
        }

        private void LanguageBarItem_Click(object sender, RoutedEventArgs e) {
            if (language == TitleLanguage.Russian) {
                language = TitleLanguage.Romaji;
            } else {
                language = TitleLanguage.Russian;
            }
            UpdateTitles();
            UpdateSorting();
            UpdatePrefCache();
        }

        private void EditButton_Click(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton != MouseButton.Left || mode == Mode.Refresh)
                return;
            refresh_timer.Stop();
            Entry entry = scheduler[GetID(sender)];
            Grid cover = GetCover(entry, false);
            EditEntryDialog dialog = new EditEntryDialog(dark_mode, language, entry, cover);
            dialog.Owner = this;
            dialog.ShowDialog();
            if (dialog.DialogResult == true) {
                if (mode == Mode.Refresh) {
                    System.Windows.Forms.MessageBox.Show("Не удалось изменить параметры!\nПожалуйста, дождитесь завершения синхронизации и попробуйте снова!");
                } else {
                    entry.Href = dialog.Href;
                    if (entry.IsExcluded != dialog.IsExcluded) {
                        if (!entry.IsExcluded && !excluded_this_run.ContainsKey(entry.ID))
                            excluded_this_run[entry.ID] = true;
                        else if (entry.IsExcluded && !excluded_this_run.ContainsKey(entry.ID))
                            entry.MarkReschedule();
                        entry.IsExcluded = dialog.IsExcluded;
                        ((CheckBox)list_view_items.Find(p => GetID(p) == entry.ID).Children[list_item_checkbox_idx]).IsChecked = !entry.IsExcluded;
                    }
                    if (entry.Status == AnimeStatus.PendingOngoing && entry.OverrideRegularOngoing && dialog.Reset) {
                        entry.Status = AnimeStatus.RegularOngoing;
                        entry.OverrideRegularOngoing = false;
                        entry.Period = 7;
                        entry.EpisodesPerDay = 1;
                        ((ComboBox)list_view_items.Find(p => GetID(p) == entry.ID).Children[list_item_period_idx]).Visibility = Visibility.Collapsed;
                    } else if (!(entry.Status == AnimeStatus.RegularOngoing && dialog.Reset)) {
                        if (entry.Status == AnimeStatus.RegularOngoing && dialog.OverrideRegularOngoing) {
                            entry.Status = AnimeStatus.PendingOngoing;
                            entry.OverrideRegularOngoing = true;
                            entry.MarkReschedule();
                            ((ComboBox)list_view_items.Find(p => GetID(p) == entry.ID).Children[list_item_period_idx]).Visibility = Visibility.Visible;
                        }
                        if (entry.Period != dialog.Period) {
                            entry.MarkReschedule();
                            entry.Period = dialog.Period;
                            ((ComboBox)list_view_items.Find(p => GetID(p) == entry.ID).Children[list_item_period_idx]).SelectedIndex = entry.Period - 1;
                        }
                        if (entry.EpisodesPerDay != dialog.EpisodesPerDay && dialog.EpisodesPerDay > 0) {
                            entry.MarkReschedule();
                            entry.EpisodesPerDay = dialog.EpisodesPerDay;
                        }
                        if (dialog.Reset)
                            entry.MarkReschedule();
                        scheduler.Schedule();
                    }
                    UpdateCalendar();
                    UpdateDataCache();
                }
            }
            refresh_timer.Start();
        }

        private void Period_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Entry entry = scheduler[GetID(sender)];
            int new_period = ((ComboBox)sender).SelectedIndex + 1;
            if (entry.Period != new_period) {
                entry.MarkReschedule();
                entry.Period = new_period;
                scheduler.Schedule();
                UpdateCalendar();
                UpdateDataCache();
            }
        }

        private void Isincluded_Checked(object sender, RoutedEventArgs e) {
            var entry = scheduler[GetID(sender)];
            if (entry.IsExcluded == (((CheckBox)sender).IsChecked == false))
                return;
            if (!entry.IsExcluded && !excluded_this_run.ContainsKey(entry.ID))
                excluded_this_run[entry.ID] = true;
            else if (entry.IsExcluded && !excluded_this_run.ContainsKey(entry.ID))
                entry.MarkReschedule();
            entry.IsExcluded = ((CheckBox)sender).IsChecked == false;
            if (!excluding_task_running) {
                scheduler.Schedule();
                UpdateCalendar();
                UpdateDataCache();
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e) {
            if (mode == Mode.Refresh) {
                System.Windows.Forms.MessageBox.Show("Пожалуйста, дождитесь завершения синхронизации!");
                return;
            }
            if (!excluding_task_running) {
                excluding_task_running = true;
                foreach (Grid item in listPanel.Children) {
                    ((CheckBox)item.Children[list_item_checkbox_idx]).IsChecked = excluded_all;
                }
                scheduler.Schedule();
                UpdateCalendar();
                UpdateDataCache();
                excluding_task_running = false;
            }
        }

        private void DelayBarItem_Click(object sender, RoutedEventArgs e) {
            refresh_timer.Stop();
            SelectIntDialog dialog = new SelectIntDialog(dark_mode, "Выбор задержки онгоингов", "Онгоинги отображаются в расписании с задержкой после выхода нового эпизода, так как возможность посмотреть его в день выхода ограничена. Нулевая задержка означает, что серии онгоингов будут ставиться на день их выхода.\n\nВнимание!!! Изменение задержки ведет к потере текущего расписания! Отменить данное действие невозможно.\n\nУкажите число дней, на которое хотите откладывать онгоинги:", 0, 6, real_ongoing_delay);
            dialog.Owner = this;
            dialog.ShowDialog();
            if (dialog.DialogResult == true) {
                if (mode == Mode.Refresh) {
                    System.Windows.Forms.MessageBox.Show("Пожалуйста, дождитесь завершения синхронизации и попробуйте снова!");
                } else {
                    Scheduler.RealOngoingDelay = real_ongoing_delay = dialog.Value;
                    scheduler.Reschedule();
                    UpdateCalendar();
                    UpdateCache();
                }
            }
            refresh_timer.Start();
        }

        private void SignOutBarItem_Click(object sender, RoutedEventArgs e) {
            if (mode == Mode.Refresh) {
                System.Windows.Forms.MessageBox.Show("Пожалуйста, дождитесь завершения синхронизации!");
            } else {
                SignInMode();
            }
        }

        private void ViewBarItem_Click(object sender, RoutedEventArgs e) {
            list_mode = !list_mode;
            if (list_mode) {
                listView.Visibility = Visibility.Visible;
                mainScrollView.Visibility = Visibility.Collapsed;
                viewBarItem.Header = "Режим календаря";
            } else {
                listView.Visibility = Visibility.Collapsed;
                mainScrollView.Visibility = Visibility.Visible;
                viewBarItem.Header = "Режим списка";
            }
        }

        private void ScoreSortBarItem_Click(object sender, RoutedEventArgs e) {
            if (sorting_mode != SortingMode.Score) {
                yearSortBarItem.IsChecked = false;
                alphabetSortBarItem.IsChecked = false;
                progressSortBarItem.IsChecked = false;
                UnCheckSortButton(buttonSortYear);
                UnCheckSortButton(buttonSortTitle);
                UnCheckSortButton(buttonSortProgress);
                CheckSortButton(buttonSortScore);
                sorting_mode = SortingMode.Score;
                watching_part_url = watching_score_part_url;
                UpdateWatchingUrl();
                UpdateSorting();
                UpdatePrefCache();
            }
            scoreSortBarItem.IsChecked = true;
        }

        private void YearSortBarItem_Click(object sender, RoutedEventArgs e) {
            if (sorting_mode != SortingMode.Year) {
                scoreSortBarItem.IsChecked = false;
                alphabetSortBarItem.IsChecked = false;
                progressSortBarItem.IsChecked = false;
                UnCheckSortButton(buttonSortScore);
                UnCheckSortButton(buttonSortTitle);
                UnCheckSortButton(buttonSortProgress);
                CheckSortButton(buttonSortYear);
                sorting_mode = SortingMode.Year;
                watching_part_url = watching_year_part_url;
                UpdateWatchingUrl();
                UpdateSorting();
                UpdatePrefCache();
            }
            yearSortBarItem.IsChecked = true;
        }

        private void ProgressSortBarItem_Click(object sender, RoutedEventArgs e) {
            if (sorting_mode != SortingMode.Progress) {
                yearSortBarItem.IsChecked = false;
                scoreSortBarItem.IsChecked = false;
                alphabetSortBarItem.IsChecked = false;
                UnCheckSortButton(buttonSortYear);
                UnCheckSortButton(buttonSortTitle);
                UnCheckSortButton(buttonSortScore);
                CheckSortButton(buttonSortProgress);
                sorting_mode = SortingMode.Progress;
                watching_part_url = watching_progress_part_url;
                UpdateWatchingUrl();
                UpdateSorting();
                UpdatePrefCache();
            }
            progressSortBarItem.IsChecked = true;
        }

        private void AlphabetSortBarItem_Click(object sender, RoutedEventArgs e) {
            if (sorting_mode != SortingMode.Alphabet) {
                yearSortBarItem.IsChecked = false;
                scoreSortBarItem.IsChecked = false;
                progressSortBarItem.IsChecked = false;
                UnCheckSortButton(buttonSortYear);
                UnCheckSortButton(buttonSortScore);
                UnCheckSortButton(buttonSortProgress);
                CheckSortButton(buttonSortTitle);
                sorting_mode = SortingMode.Alphabet;
                watching_part_url = watching_alphabet_part_url;
                UpdateWatchingUrl();
                UpdateSorting();
                UpdatePrefCache();
            }
            alphabetSortBarItem.IsChecked = true;
        }

        private void WeekStartBarItem_Click(object sender, RoutedEventArgs e) {
            week_start_now = !week_start_now;
            UpdateCalendar();
            UpdatePrefCache();
        }

        private void DarkBarItem_Click(object sender, RoutedEventArgs e) {
            if (!dark_mode) DarkMode();
            else LightMode();
            UpdateColors();
            UpdatePrefCache();
        }

        private void Refresh_Timer_Tick(object sender, EventArgs e) {
            RefreshAsync();
        }

        private void Animation_Timer_Tick(object sender, EventArgs e) {
            current_frame = (current_frame + 1) % background_key_frames.Count;
            syncGrid.Background = background_key_frames[current_frame];
        }

        private void RefreshBarItem_Click(object sender, RoutedEventArgs e) {
            RefreshAsync();
        }

        private void ShikiBarItem_Click(object sender, RoutedEventArgs e) {
            OpenShiki();
        }

        private void ExitBarItem_Click(object sender, RoutedEventArgs e) {
            Exit();
        }

        private void AccountsBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (accountsBox.SelectedIndex < accountsBox.Items.Count - 1) {
                nameTextBox.Text = (string)((ComboBoxItem)accountsBox.SelectedItem).Content;
                nameTextBox.CaretIndex = nameTextBox.Text.Length;
            } else {
                nameTextBox.Visibility = Visibility.Visible;
                accountsBox.Visibility = Visibility.Collapsed;
            }
        }

        private void SignInButtonOK_Click(object sender, RoutedEventArgs e) {
            if (mode == Mode.SignIn && nameTextBox.Text != "") {
                using (var client = new WebClient()) {
                    client.Encoding = System.Text.Encoding.UTF8;
                    if (!CheckForInternetConnection(5000)) {
                        System.Windows.Forms.MessageBox.Show("Вы оффлайн! Чтобы продолжить,\nпожалуйста, подключитесь к интернету...");
                        return;
                    }
                    try {
                        client.DownloadString(shiki_url + WebUtility.UrlEncode(nameTextBox.Text) + watching_part_url);
                    } catch {
                        System.Windows.Forms.MessageBox.Show("Неверное имя пользователя или закрытый список!\nПожалуйста, укажите имя существующего аккаунта с открытым списком...");
                        return;
                    }
                }
                list_mode = false;
                NormalMode();
                if (account_name != nameTextBox.Text) {
                    UpdateDataCache();
                    account_name = nameTextBox.Text;
                    UpdateWatchingUrl();
                    try {
                        LoadSchedule();
                    } catch {
                        scheduler.Clear();
                    }
                    SyncMode();
                    RefreshAsync();
                }
            }
        }

        private void NameTextBox_GotFocus(object sender, RoutedEventArgs e) {
            if (nameTextBox.Foreground != main_font_brush) {
                nameTextBox.Foreground = main_font_brush;
                nameTextBox.Text = "";
            }
        }

        private void NameTextBox_LostFocus(object sender, RoutedEventArgs e) {
            if (nameTextBox.Text == "") {
                nameTextBox.Foreground = inactive_textbox_brush;
                nameTextBox.Text = "Наберите имя здесь...";
            }
        }

        private void SignInButtonEsc_Click(object sender, RoutedEventArgs e) {
            if (mode == Mode.SignIn && account_name != "") {
                NormalMode();
            } else if (mode != Mode.SignIn) {
                //this.Close();
            }
        }

        private void LinkButton_MouseEnter(object sender, MouseEventArgs e) {
            ((TextBlock)sender).Foreground = link_active_brush;
            ((TextBlock)sender).TextDecorations = TextDecorations.Underline;
        }

        private void LinkButton_MouseLeave(object sender, MouseEventArgs e) {
            ((TextBlock)sender).Foreground = link_normal_brush;
            ((TextBlock)sender).TextDecorations = null;
        }

        private void OpenNIClick(object sender, EventArgs e) {
            if (this.Visibility == Visibility.Hidden) {
                this.Show();
                this.ShowInTaskbar = true;
                ScrollToCurrent();
            } else if (this.WindowState == WindowState.Minimized) {
                this.WindowState = WindowState.Normal;
            } else {
                this.Activate();
            }
        }

        private void ShikiNIClick(object sender, EventArgs e) {
            OpenShiki();
        }

        private void ExitNIClick(object sender, EventArgs e) {
            Exit();
        }

        private void Link_Click(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton != MouseButton.Left)
                return;
            try {
                OpenUrl(scheduler[GetID(sender)].Href);
            } catch {
                System.Windows.Forms.MessageBox.Show("Указанная ссылка недействительна!");
            }
            e.Handled = true;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            if (list_mode && searchTextBox.Foreground != inactive_textbox_brush) {
                FilterList(searchTextBox.Text.ToLower());
            }
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e) {
            searchTextBox.Foreground = main_font_brush;
            searchTextBox.Text = "";
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e) {
            if (searchTextBox.Text == "") {
                searchTextBox.Foreground = inactive_textbox_brush;
                searchTextBox.Text = "Поиск...";
            }
        }

        private void ListItem_MouseLeave(object sender, MouseEventArgs e) {
            ((Grid)sender).Background = GetHighlightBrush(scheduler[GetID(sender)]);
        }

        private void ListItem_MouseEnter(object sender, MouseEventArgs e) {
            ((Grid)sender).Background = gray_highlight_brush;
        }

        private void Edit_button_MouseEnter(object sender, MouseEventArgs e) {
            ((Border)sender).Background = edit_button_active_brush;
        }

        private void Edit_button_MouseLeave(object sender, MouseEventArgs e) {
            ((Border)sender).Background = edit_button_normal_brush;
        }

        private void Cover_MouseEnter(object sender, MouseEventArgs e) {
            if (mode != Mode.Refresh)
                ((Grid)sender).Children[cover_button_idx].Visibility = Visibility.Visible;
        }

        private void Cover_MouseLeave(object sender, MouseEventArgs e) {
            ((Grid)sender).Children[cover_button_idx].Visibility = Visibility.Hidden;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            if (mode != Mode.SignIn)
                ScrollToCurrent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (running) {
                this.Visibility = Visibility.Hidden;
                this.ShowInTaskbar = false;
                e.Cancel = true;
            } else if (mode == Mode.Refresh) {
                System.Windows.Forms.MessageBox.Show("Пожалуйста, дождитесь завершения синхронизации!");
                running = true;
                e.Cancel = true;
            }
        }

        private void Window_Closed(object sender, EventArgs e) {
            if (ni != null) {
                ni.Visible = false;
                ni.Dispose();
                UpdateCache();
            }
            instance_stream.Close();
            instance_stream.Dispose();
        }
        #endregion

        // Various UI managing methods
        #region UI
        /// <summary>
        /// Rebuild UI from scratch after major scheduler update or other destructive actions.
        /// </summary>
        void UpdateUI() {
            for (int i = 0; i < 7; ++i)
                calendar_items[i].Clear();
            double[] max_width = new double[7];
            UidCounter = 0;
            list_view_items.Clear();
            excluded_all = true;
            foreach (var entry in scheduler.Entries) {
                // Calendar view
                for (int rweek_day = 0; rweek_day < 7; ++rweek_day) {
                    Grid cover = GetCover(entry);
                    calendar_items[rweek_day].Add(cover);
                    if (max_width[rweek_day] < cover.Width)
                        max_width[rweek_day] = cover.Width;
                }
                // List view
                Grid list_item = GetListItem(entry);
                list_view_items.Add(list_item);
                if (!entry.IsExcluded)
                    excluded_all = false;
            }
            if (list_view_items.Count == 0)
                listPlaceholder.Visibility = Visibility.Visible;
            else
                listPlaceholder.Visibility = Visibility.Collapsed;
            totalEntriesTextBlock.Text = $"Всего тайтлов: {scheduler.Count}";
            ongoingsEntriesTextBlock.Text = $"Онгоингов: {scheduler.Entries.Count(p => p.IsOngoing)}";
            totalEpisodesTextBlock.Text = $"Просмотрено эпизодов за все время: {total_episodes}";
            // Normalize width of covers
            for (int i = 0; i < 7; ++i) {
                foreach (Grid cover in calendar_items[i]) {
                    cover.Width = max_width[i];
                }
            }
            UpdateUIEasy();
        }

        /// <summary>
        /// Load anime poster from disk.
        /// </summary>
        /// <param name="id"></param>
        void LoadPoster(int id) {
            BitmapSource poster = new BitmapImage(new Uri(tmpdir + id.ToString() + ".jpg"));
            if (poster.PixelWidth / (double)poster.PixelHeight > max_poster_aspect_ratio) {
                int crop_width = (int)(poster.PixelHeight * max_poster_aspect_ratio);
                poster = new CroppedBitmap(poster, new Int32Rect((poster.PixelWidth - crop_width) / 2, 0, crop_width, poster.PixelHeight));
            }
            posters[id] = new Poster(poster);
        }

        /// <summary>
        /// Build anime cover object displayed in calendar and editing dialog.
        /// </summary>
        /// <param name="entry">Anime entry.</param>
        /// <param name="title_and_button">Add link title and edit button.</param>
        /// <returns>Cover object.</returns>
        Grid GetCover(Entry entry, bool title_and_button = true) {
            Grid cover = new Grid {
                Height = slot_height - (title_and_button ? 0 : slot_title_height),
                Uid = $"cover{UidCounter}_{entry.ID}",
                ToolTip = new ToolTip {
                    Content = title_and_button ? GetEntryToolTip(entry) : entry.Href
                }
            };
            cover.RowDefinitions.Add(new RowDefinition { Height = new GridLength(slot_vert_gap) });
            cover.RowDefinitions.Add(new RowDefinition());
            cover.RowDefinitions.Add(new RowDefinition { Height = new GridLength(slot_vert_gap) });
            if (title_and_button)
                cover.RowDefinitions.Add(new RowDefinition { Height = new GridLength(slot_title_height) });
            cover.ColumnDefinitions.Add(new ColumnDefinition());
            cover.ColumnDefinitions.Add(new ColumnDefinition());
            cover.ColumnDefinitions.Add(new ColumnDefinition());
            if (title_and_button) {
                cover.MouseEnter += Cover_MouseEnter;
                cover.MouseLeave += Cover_MouseLeave;
            }
            if (!posters.ContainsKey(entry.ID) || !posters[entry.ID].IsLoaded) {
                try {
                    LoadPoster(entry.ID);
                } catch {
                    posters[entry.ID] = new Poster(new BitmapImage(new Uri(ui_img_dir + "no_cover.jpg")), false);
                }
            }
            Image poster = new Image {
                Source = posters[entry.ID].Source,
                Cursor = Cursors.Hand,
                Uid = $"poster{UidCounter}_{entry.ID}"
            };
            RenderOptions.SetBitmapScalingMode(poster, BitmapScalingMode.HighQuality);
            RenderOptions.SetEdgeMode(poster, EdgeMode.Aliased);
            poster.Effect = new DropShadowEffect {
                BlurRadius = shadow_blur_radius,
                ShadowDepth = 0,
                Color = Colors.Black,
                Opacity = 0.7
            };
            poster.MouseUp += Link_Click;
            Grid.SetRow(poster, 1);
            Grid.SetColumn(poster, 1);
            cover.Children.Add(poster);

            double title_width = ((BitmapSource)poster.Source).PixelWidth * (slot_height - slot_vert_gap * 2 - slot_title_height) / ((BitmapSource)poster.Source).PixelHeight;
            cover.ColumnDefinitions[1].Width = new GridLength(title_width);
            cover.Width = title_width + slot_horiz_gap * 2;

            if (title_and_button) {
                TextBlock title = GetLinkTitle(entry);
                title.MaxWidth = title_width;
                title.MaxHeight = slot_title_height;
                title.HorizontalAlignment = HorizontalAlignment.Left;
                title.VerticalAlignment = VerticalAlignment.Top;
                Grid.SetRow(title, 3);
                Grid.SetColumn(title, 1);
                Border edit_button = new Border {
                    Background = edit_button_normal_brush,
                    Visibility = Visibility.Hidden,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Uid = $"editicon{UidCounter}_{entry.ID}",
                    Cursor = Cursors.Hand,
                    ToolTip = new ToolTip {
                        Content = "Редактировать..."
                    }
                };
                edit_button.MouseUp += EditButton_Click;
                edit_button.MouseEnter += Edit_button_MouseEnter;
                edit_button.MouseLeave += Edit_button_MouseLeave;
                Grid.SetRow(edit_button, 1);
                Grid.SetColumn(edit_button, 1);
                Image edit_icon = new Image {
                    Source = edit_icon_image,
                    Height = edit_icon_height - edit_icon_gap * 2,
                    Margin = new Thickness(edit_icon_gap, edit_icon_gap, edit_icon_gap, edit_icon_gap)
                };
                RenderOptions.SetBitmapScalingMode(edit_icon, BitmapScalingMode.HighQuality);
                RenderOptions.SetEdgeMode(edit_icon, EdgeMode.Aliased);
                edit_button.Child = edit_icon;

                cover.Children.Add(edit_button);
                cover.Children.Add(title);
            }
            return cover;
        }

        /// <summary>
        /// Build list item object with checkbox, buttons, info and stuff.
        /// </summary>
        /// <param name="entry">Anime entry.</param>
        /// <returns>List item object.</returns>
        Grid GetListItem(Entry entry) {
            string anime_title = (language == TitleLanguage.Russian ? entry.RussianName : entry.RomajiName);
            Grid item = new Grid {
                Uid = $"listitem_{entry.ID}",
                Height = 28,
                Cursor = Cursors.Hand,
                ToolTip = new ToolTip {
                    Content = "Редактировать..."
                }
            };
            item.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            item.ColumnDefinitions.Add(new ColumnDefinition());
            item.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            item.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            item.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            item.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            item.MouseUp += EditButton_Click;
            item.MouseEnter += ListItem_MouseEnter;
            item.MouseLeave += ListItem_MouseLeave;
            CheckBox isincluded = new CheckBox {
                Uid = $"ischecked_{entry.ID}",
                IsChecked = !entry.IsExcluded,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Height = 21,
                ToolTip = new ToolTip()
            };
            isincluded.Checked += Isincluded_Checked;
            isincluded.Unchecked += Isincluded_Checked;
            EditEntryDialog.UpdateCheckBoxToolTip(isincluded);
            Grid.SetColumn(isincluded, 0);
            TextBlock title = GetLinkTitle(entry);
            title.Margin = new Thickness(10, 0, 0, 0);
            title.Height = 24;
            title.HorizontalAlignment = HorizontalAlignment.Left;
            title.VerticalAlignment = VerticalAlignment.Bottom;
            title.ToolTip = new ToolTip {
                Content = entry.Href
            };
            Grid.SetColumn(title, 1);
            TextBlock score = new TextBlock {
                Text = entry.Score == 0 ? "-" : entry.Score.ToString(),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Height = 24
            };
            Grid.SetColumn(score, 2);
            TextBlock progress = new TextBlock {
                Text = GetProgress(entry),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Height = 24
            };
            Grid.SetColumn(progress, 3);
            TextBlock year = new TextBlock {
                Text = entry.YearCreated == 0 ? "-" : entry.YearCreated.ToString(),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Height = 24
            };
            Grid.SetColumn(year, 4);
            ComboBox period = new ComboBox {
                Uid = $"period_{entry.ID}",
                SelectedIndex = entry.Period - 1,
                IsEditable = false,
                Visibility = entry.IsRegularOngoing ? Visibility.Collapsed : Visibility.Visible,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 60,
                Height = 24,
                ToolTip = new ToolTip {
                    Content = "Период выхода новых серий"
                }
            };
            Grid.SetColumn(period, 5);
            for (int i = 1; i <= 7; ++i) {
                period.Items.Add(new ComboBoxItem {
                    Content = i.ToString(),
                    IsSelected = i == entry.Period
                });
            }
            period.SelectionChanged += Period_SelectionChanged;

            item.Children.Add(isincluded);
            item.Children.Add(score);
            item.Children.Add(progress);
            item.Children.Add(year);
            item.Children.Add(period);
            item.Children.Add(title);
            return item;
        }

        /// <summary>
        /// Choose which color to use to highlight entry UI element.
        /// </summary>
        /// <param name="entry">Anime entry.</param>
        /// <param name="is_current">Is scheduled for today.</param>
        /// <returns>Hightlight brush.</returns>
        Brush GetHighlightBrush(Entry entry, bool is_current = false) {
            if (is_current)
                return entry.AreConditionsSatisfied ? green_highlight_brush : (entry.ExpectedEpisodes - entry.WatchedEpisodes > entry.EpisodesPerDay ? red_highlight_brush : yellow_highlight_brush);
            if (entry.AreConditionsSatisfied)
                return Brushes.Transparent;
            if (entry.ExpectedEpisodes - entry.WatchedEpisodes > entry.EpisodesPerDay)
                return red_highlight_brush;
            if (entry.CurrentWeekDays.Contains(Convert.ToInt32(DateTime.Now.DayOfWeek)))
                return yellow_highlight_brush;
            return red_highlight_brush;
        }

        /// <summary>
        /// Get entry tooltip text.
        /// </summary>
        /// <param name="entry">Anime entry.</param>
        /// <returns>Entry tooltip text.</returns>
        string GetEntryToolTip(Entry entry) {
            string anime_title = (language == TitleLanguage.Russian ? entry.RussianName : entry.RomajiName);
            return anime_title + $"\n{(entry.IsOngoing ? "Онгоинг\n" : (entry.YearCreated != 0 ? $"{entry.YearCreated}\n" : ""))}{(entry.Score != 0 ? $"Оценка {entry.Score}\n" : "")}Просмотрено {entry.WatchedEpisodes}/{(entry.IsExcluded ? entry.TotalEpisodes : entry.ExpectedEpisodes)}";
        }

        /// <summary>
        /// Get progress text.
        /// </summary>
        /// <param name="entry">Anime entry.</param>
        /// <returns>Watched episodes / Expected episodes.</returns>
        string GetProgress(Entry entry) {
            return $"{entry.WatchedEpisodes} / {(entry.IsExcluded ? entry.TotalEpisodes : entry.ExpectedEpisodes)}";
        }

        /// <summary>
        /// Get anime title with hyperlink.
        /// </summary>
        /// <param name="entry">Anime entry.</param>
        /// <returns>Title object.</returns>
        TextBlock GetLinkTitle(Entry entry) {
            string anime_title = (language == TitleLanguage.Russian ? entry.RussianName : entry.RomajiName);
            Setter setter_hover = new Setter {
                Property = TextBlock.ForegroundProperty,
                Value = link_active_brush
            };
            Trigger trigger = new Trigger {
                Property = TextBlock.IsMouseOverProperty,
                Value = true,
                Setters = { setter_hover }
            };
            Setter setter_normal = new Setter {
                Property = TextBlock.ForegroundProperty,
                Value = link_normal_brush
            };
            Style style = new Style {
                Triggers = { trigger },
                Setters = { setter_normal }
            };
            TextBlock title = new TextBlock {
                Uid = $"linktitle{UidCounter}_{entry.ID}",
                Text = anime_title,
                TextDecorations = TextDecorations.Underline,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Cursor = Cursors.Hand,
                Style = style,
            };
            title.PreviewMouseUp += Link_Click;
            return title;
        }

        /// <summary>
        /// Get "nothing here" placeholder.
        /// </summary>
        /// <returns>Placeholder object.</returns>
        Grid GetPlaceholder() {
            Grid placeholder = new Grid {
                Height = slot_height,
                Visibility = Visibility.Collapsed
            };
            Image nothing_img = new Image {
                Source = new BitmapImage(new Uri(ui_img_dir + "nothing_here.png")),
                Height = slot_height,
                Opacity = 0.4,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(nothing_img, BitmapScalingMode.HighQuality);
            RenderOptions.SetEdgeMode(nothing_img, EdgeMode.Aliased);
            TextBlock nothing_text = new TextBlock {
                Text = "ПУСТОТА",
                Opacity = 0.3,
                FontSize = 72,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            placeholder.Children.Add(nothing_img);
            placeholder.Children.Add(nothing_text);
            return placeholder;
        }

        /// <summary>
        /// Update titles after language change.
        /// </summary>
        void UpdateTitles() {
            for (int i = 0; i < 7; ++i) {
                foreach (var cover in calendar_items[i])
                    ((TextBlock)cover.Children[cover_title_idx]).Text = language == TitleLanguage.Russian ? scheduler[GetID(cover)].RussianName : scheduler[GetID(cover)].RomajiName;
            }
            foreach (var item in list_view_items)
                ((TextBlock)item.Children[list_item_title_idx]).Text = language == TitleLanguage.Russian ? scheduler[GetID(item)].RussianName : scheduler[GetID(item)].RomajiName;
        }

        /// <summary>
        /// Update colors after theme change. Coundn't make it without rebuilding entire UI.
        /// </summary>
        void UpdateColors() {
            /* for (int i = 0; i < 7; ++i)
            {
                foreach (var cover in calendar_items[i]) {
                    var entry = scheduler[GetID(cover)];
                    cover.Children.RemoveAt(cover_title_idx);
                    cover.Children.Add(GetLinkTitle(language == TitleLanguage.Russian ? entry.RussianName : entry.RomajiName, entry));
                }
            }
            foreach (var item in list_view_items)
            {
                var entry = scheduler[GetID(item)];
                item.Children[list_item_title_idx] = GetLinkTitle(language == TitleLanguage.Russian ? entry.RussianName : entry.RomajiName, entry);
            }
            UpdateCovers();
            UpdateSlots(); */
            UpdateUI();
        }

        /// <summary>
        /// Update entry sorting in calendar and list.
        /// </summary>
        void UpdateSorting() {
            UIElementComparer comparer = GetUIElementComparer();
            for (int i = 0; i < 7; ++i) {
                calendar_items[i].Sort((Grid a, Grid b) => comparer(a, b));
                slots[i].Children.Clear();
                foreach (var cover in calendar_items[i])
                    slots[i].Children.Add(cover);
            }
            list_view_items.Sort((Grid a, Grid b) => comparer(a, b));
            listPanel.Children.Clear();
            foreach (var item in list_view_items)
                listPanel.Children.Add(item);
        }

        /// <summary>
        /// Light calendar update, does not remove and add new objects, only existing are changed.
        /// </summary>
        void UpdateCalendar() {
            DateTime date = DateTime.Now;
            int current_week_day = Conv.ToWeekDayRu(date.DayOfWeek);
            if (!week_start_now)
                date = date.AddDays(-current_week_day);
            excluded_all = true;
            // Calendar view cover visibility and coloring
            for (int week_day = 0; week_day < 7; ++week_day) {
                int rweek_day = Conv.ToWeekDayRu(week_day);
                bool is_current = rweek_day == current_week_day;
                if (week_start_now)
                    rweek_day = (rweek_day - current_week_day + 7) % 7;
                foreach (Grid cover in slots[rweek_day].Children) {
                    var entry = scheduler[GetID(cover)];
                    if (entry.GetWeekSchedule(date).Contains(week_day)) {
                        cover.Visibility = Visibility.Visible;
                        ((ToolTip)cover.ToolTip).Content = GetEntryToolTip(entry);
                        if (is_current)
                            cover.Background = GetHighlightBrush(entry, true);
                        else
                            cover.Background = null;
                        if (!entry.IsExcluded)
                            excluded_all = false;
                    } else {
                        cover.Visibility = Visibility.Collapsed;
                    }
                }
            }
            // Calendar day names, dates, and coloring
            for (int i = 0; i < 7; ++i) {
                bool is_current = Conv.ToWeekDayRu(date.DayOfWeek) == current_week_day;

                bool empty = !calendar_items[i].Exists(p => p.Visibility == Visibility.Visible);
                if (empty) {
                    slots[i].Visibility = Visibility.Collapsed;
                    nothing_here_placeholders[i].Visibility = Visibility.Visible;
                } else {
                    slots[i].Visibility = Visibility.Visible;
                    nothing_here_placeholders[i].Visibility = Visibility.Collapsed;
                }
                slots[i].Background = is_current && !empty && scheduler.AreConditionsSatisfied ? green_highlight_brush : Brushes.Transparent;
                borders[i].Background = gray_highlight_brush;
                ((Label)borders[i].Children[0]).Foreground = ((Label)borders[i].Children[1]).Foreground = is_current ? main_font_brush : sec_font_brush;
                ((Label)borders[i].Children[0]).Content = day_ru[Conv.ToWeekDayRu(date.DayOfWeek)];
                ((Label)borders[i].Children[1]).Content = (is_current ? "Сегодня, " : "") + $"{date.Day} {month_ru[date.Month - 1]}";
                date = date.AddDays(1);
            }

            // List view text and coloring
            foreach (var item in list_view_items) {
                ((TextBlock)item.Children[list_item_progress_idx]).Text = GetProgress(scheduler[GetID(item)]);
                item.Background = GetHighlightBrush(scheduler[GetID(item)]);
            }
            // Other settings
            if (excluded_all) {
                ((ToolTip)buttonSelectAll.ToolTip).Content = "Включить все";
            } else {
                ((ToolTip)buttonSelectAll.ToolTip).Content = "Исключить все";
            }
            includedEntriesTextBlock.Text = $"Включено в расписание: {scheduler.Entries.Count(p => !p.IsExcluded)}";
        }

        /// <summary>
        /// Light UI update, does not remove and add new objects, only existing are changed.
        /// </summary>
        void UpdateUIEasy() {
            UpdateSorting();
            UpdateCalendar();
        }

        /// <summary>
        /// Filter list items based on search query.
        /// </summary>
        /// <param name="search_query">Text to search in anime titles.</param>
        void FilterList(string search_query) {
            foreach (Grid item in listPanel.Children) {
                Entry entry = scheduler[GetID(item)];
                if (searchTextBox.Foreground == inactive_textbox_brush || entry.RussianName.ToLower().Contains(search_query) || entry.RomajiName.ToLower().Contains(search_query))
                    item.Visibility = Visibility.Visible;
                else
                    item.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Hide edit buttons when sync is going on.
        /// </summary>
        void HideEditButtons() {
            foreach (var slot in slots)
                foreach (Grid cover in slot.Children)
                    ((Grid)cover).Children[cover_button_idx].Visibility = Visibility.Hidden;
            foreach (Grid item in listPanel.Children) {
                item.Children[list_item_checkbox_idx].IsEnabled = false;
                item.Children[list_item_period_idx].IsEnabled = false;
            }
        }

        /// <summary>
        /// Show 'em after sync is done.
        /// </summary>
        void ShowEditButtons() {
            foreach (Grid item in listPanel.Children) {
                item.Children[list_item_checkbox_idx].IsEnabled = true;
                item.Children[list_item_period_idx].IsEnabled = true;
            }
        }

        /// <summary>
        /// Mark sorting button as active.
        /// </summary>
        /// <param name="button">Button object.</param>
        void CheckSortButton(TextBlock button) {
            if (!button.Text.EndsWith(" ↓"))
                button.Text += " ↓";
        }

        /// <summary>
        /// Deactivate sorting button.
        /// </summary>
        /// <param name="button">Button object.</param>
        void UnCheckSortButton(TextBlock button) {
            if (button.Text.EndsWith(" ↓"))
                button.Text = button.Text.Substring(0, button.Text.Length - 2);
        }

        /// <summary>
        /// Provide random background for sync screen.
        /// </summary>
        /// <returns></returns>
        string GetRandomBackground() {
            var rand = new Random();
            if (rand.Next(100) < 50)
                return backgrounds[0];
            if (rand.Next(100) < 50)
                return backgrounds[rand.Next(backgrounds.Length - 1)];
            return backgrounds[rand.Next(backgrounds.Length)];
        }

        /// <summary>
        /// Change app mode to sync when changing accounts.
        /// </summary>
        void SyncMode() {
            sync_mode = true;
            mainGrid.Visibility = Visibility.Collapsed;
            syncGrid.Visibility = Visibility.Visible;
            string background = GetRandomBackground();
            if (background.Contains("_")) {
                var split = background.Split('_');
                background = split[0];
                var numbers = split[1].Split('-');
                int from = int.Parse(numbers[0]),
                    to = int.Parse(numbers[1]);
                background_key_frames = new List<Brush>();
                for (int i = from; i <= to; ++i) {
                    background_key_frames.Add(new ImageBrush {
                        ImageSource = new BitmapImage(new Uri(ui_img_dir + "screensaver/" + background + $"_{i}.jpg")),
                        Stretch = Stretch.UniformToFill
                    });
                }
                animation_timer = new DispatcherTimer {
                    Interval = new TimeSpan(0, 0, 0, 0, int.Parse(split[2]))
                };
                animation_timer.Tick += Animation_Timer_Tick;
                syncGrid.Background = background_key_frames[0];
                animation_timer.Start();
            } else {
                syncGrid.Background = new ImageBrush {
                    ImageSource = new BitmapImage(new Uri(ui_img_dir + "screensaver/" + background + ".jpg")),
                    Stretch = Stretch.UniformToFill
                };
            }
        }

        /// <summary>
        /// Change app mode to default.
        /// </summary>
        void NormalMode() {
            refresh_timer.Start();
            signInGrid.Visibility = Visibility.Collapsed;
            mainGrid.Visibility = Visibility.Visible;
            if (list_mode) {
                listView.Visibility = Visibility.Visible;
                mainScrollView.Visibility = Visibility.Collapsed;
            } else {
                listView.Visibility = Visibility.Collapsed;
                mainScrollView.Visibility = Visibility.Visible;
            }
            mainWindow.Width = norm_width;
            mainWindow.Height = norm_height;
            mode = Mode.Normal;
            running = true;
            SetWindowState();
            ScrollToCurrent();
        }

        /// <summary>
        /// Change app mode to sign in.
        /// </summary>
        void SignInMode() {
            refresh_timer.Stop();
            mainGrid.Visibility = Visibility.Collapsed;
            signInGrid.Visibility = Visibility.Visible;
            mainWindow.Width = si_width;
            mainWindow.Height = si_height;
            WindowState = WindowState.Normal;
            if (account_name != "") {
                signInButtonEsc.Visibility = Visibility.Visible;
                nameTextBox.Focus();
                nameTextBox.Text = account_name;
                nameTextBox.CaretIndex = account_name.Length;
            } else {
                signInButtonEsc.Visibility = Visibility.Hidden;
            }
            var accounts = data.GetAllContent("MyOngoings").ConvertAll(p => p.GetValue("Account"));
            if (!accounts.Contains(account_name)) {
                accounts.Add(account_name);
            }
            accounts.Reverse();
            if (accounts.Count > 1) {
                accountsBox.Items.Clear();
                for (int i = 0; i < accounts.Count; ++i) {
                    accountsBox.Items.Add(new ComboBoxItem {
                        Content = accounts[i],
                        IsSelected = accounts[i] == account_name
                    });
                    if (accounts[i] == account_name)
                        accountsBox.SelectedIndex = i;
                }
                accountsBox.Items.Add(new ComboBoxItem {
                    Content = "Добавить новый аккаунт...",
                    IsSelected = false
                });
                accountsBox.Visibility = Visibility.Visible;
                nameTextBox.Visibility = Visibility.Collapsed;
            } else {
                accountsBox.Visibility = Visibility.Collapsed;
                nameTextBox.Visibility = Visibility.Visible;
            }
            mode = Mode.SignIn;
            running = false;
        }

        void SetWindowState() {
            if (preferences.CheckValue("WindowState", "Normal")) {
                this.WindowState = WindowState.Normal;
            } else {
                this.WindowState = WindowState.Maximized;
            }
        }

        /// <summary>
        /// Scroll calendar to today's position.
        /// </summary>
        void ScrollToCurrent() {
            int current_week_day = week_start_now ? 0 : Conv.ToWeekDayRu(DateTime.Now.DayOfWeek);
            double total_slots_height = 0;
            for (int i = 0; i < current_week_day; ++i) {
                total_slots_height += Math.Max(slots[i].ActualHeight, slot_height);
            }
            mainScrollView.ScrollToVerticalOffset(total_slots_height + slot_border_height * current_week_day);
        }

        /// <summary>
        /// Send Windows notification.
        /// </summary>
        /// <param name="text_to_show">Notification text.</param>
        void SendNotification(string text_to_show) {
            ni.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            ni.BalloonTipText = text_to_show;
            ni.ShowBalloonTip(10000);
        }

        /// <summary>
        /// Exit app.
        /// </summary>
        void Exit() {
            if (mode == Mode.Refresh) {
                System.Windows.Forms.MessageBox.Show("Пожалуйста, дождитесь завершения синхронизации!");
                return;
            }
            running = false;
            this.Close();
        }

        /// <summary>
        /// Dark mode helper function.
        /// </summary>
        void AssignDarkModeDependecies() {
            main_font_brush = Brushes.White;
            sec_font_brush = Brushes.DarkGray;
            edit_button_normal_brush = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0));
            edit_button_active_brush = Brushes.RoyalBlue;
            green_highlight_brush = new SolidColorBrush(Color.FromRgb(10, 60, 10));
            yellow_highlight_brush = new SolidColorBrush(Color.FromRgb(100, 90, 0));
            red_highlight_brush = new SolidColorBrush(Color.FromRgb(100, 23, 23));
            gray_highlight_brush = new SolidColorBrush(Color.FromRgb(54, 54, 54));
            link_normal_brush = Brushes.LightSkyBlue;
            link_active_brush = Brushes.Orange;
            inactive_textbox_brush = new SolidColorBrush(Color.FromRgb(120, 120, 120));
        }

        /// <summary>
        /// Light mode helper function.
        /// </summary>
        void AssignLightModeDependecies() {
            main_font_brush = Brushes.Black;
            sec_font_brush = Brushes.Gray;
            edit_button_normal_brush = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0));
            edit_button_active_brush = Brushes.RoyalBlue;
            green_highlight_brush = Brushes.LightGreen;
            yellow_highlight_brush = Brushes.Khaki;
            red_highlight_brush = Brushes.LightSalmon;
            gray_highlight_brush = Brushes.LightGray;
            link_normal_brush = Brushes.RoyalBlue;
            link_active_brush = Brushes.Coral;
            inactive_textbox_brush = new SolidColorBrush(Color.FromRgb(0xDF, 0xDF, 0xDF));
        }

        /// <summary>
        /// To the dark side, **heavy breathing**.
        /// </summary>
        void DarkMode() {
            dark_mode = true;
            AssignDarkModeDependecies();
            mainWindow.Background = listTopBar.Background = new SolidColorBrush(Color.FromRgb(34, 34, 34));
            mainWindow.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
            mainWindow.Foreground = main_font_brush;
            signInButtonEsc.Foreground = buttonSelectAll.Foreground = buttonSortProgress.Foreground = buttonSortScore.Foreground = buttonSortTitle.Foreground = buttonSortYear.Foreground = link_normal_brush;
            if (!searchTextBox.IsFocused)
                searchTextBox.Foreground = inactive_textbox_brush;
            if (!nameTextBox.IsFocused)
                nameTextBox.Foreground = inactive_textbox_brush;
            imageLogo.Source = Invert((BitmapSource)imageLogo.Source);
            mainWindow.Resources.Clear();
        }

        /// <summary>
        /// To the light side, YOda magister is waiting.
        /// </summary>
        void LightMode() {
            dark_mode = false;
            AssignLightModeDependecies();
            mainWindow.Background = listTopBar.Background = Brushes.White;
            mainWindow.Foreground = main_font_brush;
            mainWindow.BorderBrush = null;
            signInButtonEsc.Foreground = buttonSelectAll.Foreground = buttonSortProgress.Foreground = buttonSortScore.Foreground = buttonSortTitle.Foreground = buttonSortYear.Foreground = link_normal_brush;
            if (!searchTextBox.IsFocused)
                searchTextBox.Foreground = inactive_textbox_brush;
            if (!nameTextBox.IsFocused)
                nameTextBox.Foreground = inactive_textbox_brush;
            imageLogo.Source = Invert((BitmapSource)imageLogo.Source);
            mainWindow.Resources.Add(typeof(Button), new Style() { TargetType = typeof(Button) });
            mainWindow.Resources.Add(typeof(TextBox), new Style() { TargetType = typeof(TextBox) });
            mainWindow.Resources.Add(typeof(Menu), new Style() { TargetType = typeof(Menu) });
            mainWindow.Resources.Add(typeof(Separator), new Style() { TargetType = typeof(Separator) });
            mainWindow.Resources.Add(typeof(MenuItem), new Style() { TargetType = typeof(MenuItem) });
            mainWindow.Resources.Add(typeof(ComboBox), new Style() { TargetType = typeof(ComboBox) });
            mainWindow.Resources.Add(typeof(System.Windows.Controls.Primitives.ScrollBar), new Style() { TargetType = typeof(System.Windows.Controls.Primitives.ScrollBar) });
        }
        #endregion

        // Backend app logic
        #region Logic
        /// <summary>
        /// Check for internet connection.
        /// </summary>
        /// <param name="timeoutMs">Time to wait for reply from server.</param>
        /// <param name="url">Which server to use.</param>
        /// <returns>Internet connection status.</returns>
        public static bool CheckForInternetConnection(int timeoutMs = 10000, string url = null) {
            try {
                if (url == null) {
                    url = "http://www.gstatic.com/generate_204";
                }
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.KeepAlive = false;
                request.Timeout = timeoutMs;
                using (var response = (HttpWebResponse)request.GetResponse()) { }
                return true;
            } catch {
                return false;
            }
        }

        /// <summary>
        /// Nasty servers tend to bound the number of requests per second from your app. Try to receive HTML several times and halt with offline status if not succesful.
        /// </summary>
        /// <param name="client">WebClient object.</param>
        /// <param name="href">Address to load HTML.</param>
        /// <param name="max_iter">Maximum repeats.</param>
        /// <returns>If succesful, return HTML code, otherwise return empty string.</returns>
        string ReadHTML(WebClient client, string href, int max_iter = 10) {
            bool read = false;

            string htmlCode = "";
            for (int iter = 0; !read && iter < max_iter; ++iter) {
                try {
                    htmlCode = client.DownloadString(href);
                    read = true;
                } catch {
                    if (iter == max_iter - 1) {
                        throw new TimeoutException($"Request timed out after {max_iter} attempts...");
                    }
                    Thread.Sleep(too_many_requests_delay);
                }
            }
            return htmlCode;
        }

        /// <summary>
        /// Parse anime ongoing day of week when new series are released from HTML.
        /// </summary>
        /// <param name="htmlCode">Anime page HTML code.</param>
        /// <returns>If succesful, return day of week, otherwise return Scheduler.WNotScheduled</returns>
        int GetWeekDayFromHTML(string htmlCode) {
            int start = htmlCode.IndexOf("<div class=\'key\'>Следующий эпизод:</div>");
            if (start < 0) return Scheduler.WNotScheduled;
            htmlCode = htmlCode.Substring(start + "<div class=\'key\'>Следующий эпизод:</div>".Length);
            start = htmlCode.IndexOf("<div class=\'value\'>") + "<div class=\'value\'>".Length;
            int length = htmlCode.IndexOf("</div>") - start;
            htmlCode = htmlCode.Substring(start, length).ToLower();
            string[] sdate = htmlCode.Split(' ');
            try {
                int m_day = int.Parse(sdate[0]), month = Array.IndexOf(month_ru, sdate[1]) + 1;
                DateTime now = DateTime.Now;
                int year = now.Year;
                if (month == 1 && now.Month == 12) ++year;
                DateTime date = new DateTime(year, month, m_day);
                if (date.Subtract(now).Days < 7) {
                    return Convert.ToInt32(date.DayOfWeek);
                }
            } catch { }
            return Scheduler.WNotScheduled;
        }

        /// <summary>
        /// Parse anime year of creation from HTML.
        /// </summary>
        /// <param name="htmlCode">Anime page HTML code.</param>
        /// <returns>If succesful, return year of creation, otherwise return 0.</returns>
        int GetYearCreatedFromHTML(string htmlCode) {
            int start = htmlCode.IndexOf("<div class=\"l-content\"><div class=\"block\">");
            if (start < 0) return 0;
            htmlCode = htmlCode.Substring(start + "<div class=\"l-content\"><div class=\"block\">".Length);
            int length = htmlCode.IndexOf("<div class=\"b-db_entry\">");
            File.WriteAllText(appdata + "tmp\\watching.html", htmlCode.Substring(0, length));
            List<Tag> html_code;
            using (var reader = new TagReader(appdata + "tmp\\watching.html", true)) {
                html_code = reader.Read();
            }
            try {
                return int.Parse(html_code.Find(p => p.CheckValue("itemprop", "dateCreated")).GetValue("content").Split('-')[0]);
            } catch {
                return 0;
            }
        }

        /// <summary>
        /// Heart of the app, syncs shikimori anime list and scheduler. Do not call this method directly, use RefreshAsync so the app won't freeze.
        /// </summary>
        /// <returns>Flag indicating whether any changes has been made to scheduler's data.</returns>
        bool Refresh() {
            bool changes_made = false;

            string htmlCode;
            var client = new WebClient();
            client.Encoding = System.Text.Encoding.UTF8;
            try {
                htmlCode = client.DownloadString(watching_url);
            } catch {
                mode = Mode.Offline;
                return changes_made;
            }
            int start = htmlCode.IndexOf("<tbody class=\"entries\">"),
                length = htmlCode.IndexOf("</tbody>") + "</tbody>".Length - start;
            if (start < 0) return changes_made;
            File.WriteAllText(appdata + "tmp\\watching.html", htmlCode.Substring(start, length));
            Tag html_code;
            using (var reader = new TagReader(appdata + "tmp\\watching.html", true)) {
                html_code = reader.Read()[0];
            }
            bool offline = false;
            List<int> dead_entries = new List<int>(scheduler.IDs);
            foreach (var item in html_code.Content) {
                string romaji_name, russian_name, type, href;
                int id, total_ep, watched_ep, score;
                bool is_ongoing;
                try {
                    var episodes_tag = item.Content.Find(p => p.ContainsContent("span") && p.GetContent("span").ContainsKey("data-field") && p.GetContent("span").CheckValue("data-field", "episodes"));
                    var ep_data_tag = episodes_tag.Content.Find(p => p.ContainsKey("class") && p.CheckValue("class", "current-value"));
                    var score_tag = item.Content.Find(p => p.ContainsContent("span") && p.GetContent("span").ContainsKey("data-field") && p.GetContent("span").CheckValue("data-field", "score")).GetContent("span");
                    var status_tag = item.Content.Find(p => p.ContainsContent("a"));
                    var type_tag = item.Content.Last();
                    id = item.GetIntValue("data-target_id");
                    try {
                        score = score_tag.GetIntValue();
                    } catch {
                        score = 0;
                    }
                    romaji_name = item.GetValue("data-target_name");
                    russian_name = item.GetValue("data-target_russian");
                    type = type_tag.GetValue();
                    is_ongoing = status_tag.Content.Exists(p => p.CheckValue("class", "ongoing"));
                    href = shiki_url + status_tag.GetContent("a").GetValue("href").Substring(1);
                    watched_ep = ep_data_tag.Content[0].GetIntValue();
                    if (is_ongoing) {
                        total_ep = episodes_tag.Content.Find(p => p.ContainsKey("class") && p.CheckValue("class", "misc-value")).GetIntValue();
                    } else {
                        total_ep = ep_data_tag.GetIntValue("data-max");
                    }
                } catch {
                    throw new Exception("Unknown error during html parsing!");
                }
                if (!Scheduler.IsSupported(type)) {
                    continue;
                }
                bool already_present = scheduler.Contains(id);
                int week_day = Scheduler.WNotScheduled, year_created = 0;
                if ((is_ongoing && !full_refreshed) || !already_present) {
                    Thread.Sleep(anime_page_delay);
                    try {
                        htmlCode = ReadHTML(client, href);
                    } catch {
                        offline = true;
                        break;
                    }
                    if (is_ongoing) {
                        week_day = GetWeekDayFromHTML(htmlCode);
                    }
                    if (!already_present) {
                        year_created = GetYearCreatedFromHTML(htmlCode);
                    }
                }
                if (already_present) {
                    dead_entries.Remove(id);
                    var entry = scheduler[id];

                    changes_made |= entry.WatchedEpisodes != watched_ep;
                    total_episodes += watched_ep - entry.WatchedEpisodes;
                    entry.WatchedEpisodes = watched_ep;
                    changes_made |= entry.TotalEpisodes != total_ep;
                    entry.TotalEpisodes = total_ep;
                    changes_made |= entry.Score != score;
                    entry.Score = score;
                    changes_made |= entry.RomajiName != romaji_name;
                    entry.RomajiName = romaji_name;
                    changes_made |= entry.RussianName != russian_name;
                    entry.RussianName = russian_name;
                    if (!full_refreshed) {
                        AnimeStatus status = entry.Status;
                        bool to_reschedule = false;
                        if (is_ongoing) {
                            if (week_day < 0) {
                                status = AnimeStatus.PendingOngoing;
                                if (entry.Status == AnimeStatus.RegularOngoing) {
                                    to_reschedule = true;
                                }
                            } else if (!(status == AnimeStatus.PendingOngoing && entry.OverrideRegularOngoing)) {
                                status = AnimeStatus.RegularOngoing;
                                changes_made |= entry.WeekDay != week_day;
                                entry.WeekDay = week_day;
                            }
                        } else {
                            status = AnimeStatus.Released;
                            if (entry.Status == AnimeStatus.RegularOngoing) {
                                to_reschedule = true;
                            } else if (entry.Status == AnimeStatus.PendingOngoing && entry.OverrideRegularOngoing) {
                                changes_made = true;
                                entry.OverrideRegularOngoing = false;
                            }
                        }
                        changes_made |= entry.Status != status;
                        entry.Status = status;
                        if (to_reschedule) {
                            scheduler.MarkReschedule(id);
                        }
                    }
                } else {
                    changes_made = true;
                    scheduler.Add(new Entry(id, romaji_name, russian_name, type, href, total_ep, watched_ep, score, year_created, is_ongoing, week_day));
                }
                string cover_path = tmpdir + id.ToString() + ".jpg";
                if (!File.Exists(cover_path)) {
                    Thread.Sleep(cover_delay);
                    try {
                        client.DownloadFile(new Uri(img_url + id.ToString() + ".jpg"), cover_path);
                    } catch {
                        CopyImgRes("no_cover.jpg", cover_path);
                    }
                }
            }
            if (!full_refreshed && !offline) {
                full_refreshed = true;
                last_full_refresh = DateTime.Now;
            }
            foreach (var key in dead_entries) {
                changes_made = true;
                scheduler.MarkRemove(key);
            }
            if (changes_made) {
                scheduler.AssignPeriods();
                scheduler.Schedule();
                UpdateDataCache();
            }
            if (total_episodes < 0)
                total_episodes = 0;
            if (offline)
                mode = Mode.Offline;
            return changes_made;
        }

        /// <summary>
        /// Wait some additional time after refresh when in sign in mode, so the genga image won't flash in a blink of an eye.
        /// </summary>
        /// <returns>Flag indicating whether any changes has been made.</returns>
        bool RefreshWithWait() {
            DateTime start_refresh = DateTime.Now;
            var reply = Refresh();
            if (DateTime.Now.Subtract(start_refresh).Milliseconds < additional_sync_sleep)
                Thread.Sleep(additional_sync_sleep);
            return reply;
        }

        /// <summary>
        /// Async refresh method, does some extra work before and after waiting for normal Refresh method to execute. This method can be used in app and is called by refresh timer every refresh_interval minutes.
        /// </summary>
        async void RefreshAsync() {
            if (mode == Mode.Normal || mode == Mode.Offline) {
                this.Title = ni.Text = busy_status;
                ni.Icon = busy_icon;
                HideEditButtons();
                mode = Mode.Refresh;
                bool to_refresh = !full_refreshed, changes_made = false;
                DateTime last_refresh_time = DateTime.Now.AddMinutes(-refresh_interval);
                await Task.Run(() => changes_made = (sync_mode ? RefreshWithWait() : Refresh()));
                if (to_refresh && mode != Mode.Offline) {
                    UpdatePrefCache();
                }
                if (changes_made || sync_mode) {
                    UpdateUI();
                } else {
                    ShowEditButtons();
                    if (last_refresh_time.DayOfWeek != DateTime.Now.DayOfWeek)
                        UpdateUIEasy();
                }
                if (sync_mode) {
                    sync_mode = false;
                    if (animation_timer != null) {
                        animation_timer.Stop();
                        background_key_frames.Clear();
                    }
                    syncGrid.Visibility = Visibility.Collapsed;
                    mainGrid.Visibility = Visibility.Visible;
                }
                if (mode == Mode.Offline) {
                    this.Title = ni.Text = no_connection_status;
                    ni.Icon = no_connection_icon;
                } else {
                    mode = Mode.Normal;
                    this.Title = ni.Text = normal_status;
                    ni.Icon = normal_icon;
                }
                SendNotifications();
            }
        }

        /// <summary>
        /// Construct and send windows notifications.
        /// </summary>
        void SendNotifications() {
            if (!start_notified) {
                start_notified = true;
                last_startup_notification = DateTime.Now;
                if (!scheduler.AreConditionsSatisfied) {
                    SendNotification("Аниме на сегодня:\n" + GetCurrentAnimeListText());
                } else if (scheduler.Count == 0) {
                    SendNotification("Ваш список \"Смотрю\" пуст!\nЧтобы добавить аниме, нажмите\nФайл → Открыть Шикимори");
                } else {
                    TrySendOverdueNotification();
                }
                UpdatePrefCache();
            } else if (!evening_notified && DateTime.Now.Hour >= evening_hour) {
                evening_notified = true;
                last_evening_notification = DateTime.Now;
                if (!scheduler.AreConditionsSatisfied) {
                    SendNotification("У вас остались непросмотренные серии!\nАниме на вечер:\n" + GetCurrentAnimeListText());
                } else {
                    TrySendOverdueNotification();
                }
                UpdatePrefCache();
            }
        }

        /// <summary>
        /// Get string with anime title and current episodes.
        /// </summary>
        /// <param name="entry">Anime entry.</param>
        /// <returns>String that is used in notifications.</returns>
        string GetEntryNotificationText(Entry entry) {
            string text = $"{(language == TitleLanguage.Russian ? entry.RussianName : entry.RomajiName)} ";
            if (entry.ExpectedEpisodes - entry.WatchedEpisodes > 1) {
                text += $"#{entry.WatchedEpisodes + 1}-{entry.ExpectedEpisodes}";
            } else {
                text += $"#{entry.WatchedEpisodes + 1}";
            }
            return text;
        }

        /// <summary>
        /// Concat entry strings of all the current animes.
        /// </summary>
        /// <returns>String that is used in notifications.</returns>
        string GetCurrentAnimeListText() {
            string text = "";
            foreach (var entry in scheduler.AnimesToWatchToday) {
                if (text != "")
                    text += "\n";
                text += GetEntryNotificationText(entry);
            }
            return text;
        }

        /// <summary>
        /// If you are all done, try to kindly suggest watching overdue animes.
        /// </summary>
        void TrySendOverdueNotification() {
            var overdue = scheduler.Entries.Where(p => !p.IsExcluded && !p.AreConditionsSatisfied);
            if (overdue.Count() == 0)
                return;
            var entry = overdue.ElementAt(new Random().Next(overdue.Count()));
            string text = scheduler.AnimesToday.Count == 0 ? $"На сегодня нет запланированных аниме, но можно посмотреть просроченные!\nКак насчет {GetEntryNotificationText(entry)} ?" : $"Отлично, вы посмотрели все аниме на сегодня!\nТеперь попробуйте нагнать просроченные тайтлы, например {GetEntryNotificationText(entry)}";
            SendNotification(text);
        }

        /// <summary>
        /// Get comparer to sort UI elements.
        /// </summary>
        /// <returns>Comparer of Grids.</returns>
        UIElementComparer GetUIElementComparer() {
            EntryComparer entry_comparer = GetEntryComparer();
            return delegate (Grid a, Grid b) {
                return entry_comparer(scheduler[GetID(a)], scheduler[GetID(b)]);
            };
        }

        /// <summary>
        /// Get comparer to sort entries.
        /// </summary>
        /// <returns>Comparer of entries.</returns>
        EntryComparer GetEntryComparer() {
            EntryComparer title_comparer;
            if (language == TitleLanguage.Russian) {
                title_comparer = (a, b) => StringComparer.InvariantCulture.Compare(a.RussianName, b.RussianName);
            } else {
                title_comparer = (a, b) => StringComparer.InvariantCulture.Compare(a.RomajiName, b.RomajiName);
            }
            if (sorting_mode == SortingMode.Score) {
                return delegate (Entry a, Entry b) {
                    if (a.Score == b.Score)
                        return title_comparer(a, b);
                    if (a.Score > b.Score)
                        return -1;
                    return 1;
                };
            } else if (sorting_mode == SortingMode.Year) {
                return delegate (Entry a, Entry b) {
                    if (a.YearCreated == b.YearCreated)
                        return title_comparer(a, b);
                    if (a.YearCreated < b.YearCreated)
                        return -1;
                    return 1;
                };
            } else if (sorting_mode == SortingMode.Progress) {
                return delegate (Entry a, Entry b) {
                    if (a.IsExcluded && b.IsExcluded) {
                        if (a.TotalEpisodes - a.WatchedEpisodes == b.TotalEpisodes - b.WatchedEpisodes)
                            return title_comparer(a, b);
                        if (a.TotalEpisodes - a.WatchedEpisodes > b.TotalEpisodes - b.WatchedEpisodes)
                            return -1;
                        return 1;
                    } else if (a.IsExcluded) {
                        return 1;
                    } else if (b.IsExcluded) {
                        return -1;
                    }
                    if (a.ExpectedEpisodes - a.WatchedEpisodes == b.ExpectedEpisodes - b.WatchedEpisodes) {
                        return title_comparer(a, b);
                    }
                    if (a.ExpectedEpisodes - a.WatchedEpisodes > b.ExpectedEpisodes - b.WatchedEpisodes) {
                        return -1;
                    }
                    return 1;
                };
            }
            return title_comparer;
        }

        void UpdateWatchingUrl() {
            watching_url = shiki_url + WebUtility.UrlEncode(account_name) + watching_part_url;
        }

        void OpenShiki() {
            if (mode != Mode.SignIn) {
                OpenUrl(watching_url);
            }
        }

        void OpenUrl(string url) => System.Diagnostics.Process.Start(url);

        /// <summary>
        /// Install app to Windows startup.
        /// </summary>
        void InstallOnStartUp() {
            try {
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                Assembly curAssembly = Assembly.GetExecutingAssembly();
                key.SetValue(curAssembly.GetName().Name, curAssembly.Location);
                autorun = true;
            } catch {
                System.Windows.Forms.MessageBox.Show("Something went wrong...");
                autorun = false;
                startupBarItem.IsChecked = false;
            }
        }

        /// <summary>
        /// Remove app from startup.
        /// </summary>
        void UnInstallOnStartUp() {
            try {
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                Assembly curAssembly = Assembly.GetExecutingAssembly();
                key.DeleteValue(curAssembly.GetName().Name);
            } catch {
                System.Windows.Forms.MessageBox.Show("Something went wrong...");
            }
            autorun = false;
            startupBarItem.IsChecked = false;
        }

        /// <summary>
        /// Check if another copy of the app is already running.
        /// </summary>
        /// <returns>Already running flag.</returns>
        bool CheckInstance() {
            if (File.Exists(tmpdir + "running") && IsFileLocked(new FileInfo(tmpdir + "running"))) {
                return true;
            }
            if (!Directory.Exists(tmpdir)) {
                Directory.CreateDirectory(tmpdir);
            }
            instance_stream = File.Open(tmpdir + "running", FileMode.Create);
            return false;
        }

        /// <summary>
        /// Parse entry ID from UI element Uid.
        /// </summary>
        /// <param name="sender">UI element with Uid set.</param>
        /// <returns>Entry ID.</returns>
        int GetID(object sender) => int.Parse(((UIElement)sender).Uid.Split('_')[1]);

        /// <summary>
        /// Helps assign unique Uids for UI elements.
        /// </summary>
        int UidCounter {
            get {
                return uid_counter++;
            }
            set {
                uid_counter = value;
            }
        }

        /* https://stackoverflow.com/questions/876473/is-there-a-way-to-check-if-a-file-is-in-use by ChrisW */

        /// <summary>
        /// Check if file is in use by another process.
        /// </summary>
        /// <param name="file">Path to file.</param>
        /// <returns>Already in use flag.</returns>
        bool IsFileLocked(FileInfo file) {
            try {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None)) {
                    stream.Close();
                }
            } catch (IOException) {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }

        /* https://stackoverflow.com/questions/40979793/how-to-invert-an-image by Patric */

        /// <summary>
        /// Invert colors of an image.
        /// </summary>
        /// <param name="source">Input image.</param>
        /// <returns>Inverted image.</returns>
        public static BitmapSource Invert(BitmapSource source) {
            // Calculate stride of source
            int stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;

            // Create data array to hold source pixel data
            int length = stride * source.PixelHeight;
            byte[] data = new byte[length];

            // Copy source image pixels to the data array
            source.CopyPixels(data, stride, 0);

            // Change this loop for other formats
            for (int i = 0; i < length; i += 4) {
                data[i] = (byte)(255 - data[i]); //R
                data[i + 1] = (byte)(255 - data[i + 1]); //G
                data[i + 2] = (byte)(255 - data[i + 2]); //B
                                                         //data[i + 3] = (byte)(255 - data[i + 3]); //A
            }

            // Create a new BitmapSource from the inverted pixel buffer
            return BitmapSource.Create(
                source.PixelWidth, source.PixelHeight,
                source.DpiX, source.DpiY, source.Format,
                null, data, stride);
        }
        #endregion
    }

    /// <summary>
    /// Stores anime posters with loaded flag to deal with sudden deletion of poster image file on disk.
    /// </summary>
    class Poster {
        BitmapSource source;
        bool is_loaded;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Poster() {
            is_loaded = false;
        }

        /// <summary>
        /// Construct poster from image and optional flag.
        /// </summary>
        /// <param name="source">Poster image.</param>
        /// <param name="is_loaded">Loaded flag.</param>
        public Poster(BitmapSource source, bool is_loaded = true) {
            this.source = source;
            this.is_loaded = is_loaded;
        }

        public BitmapSource Source => source;
        public bool IsLoaded => is_loaded;
    }

    /// <summary>
    /// Massive string resources class.
    /// </summary>
    public static class StringResources {
        public static string Help => "С чего начать?\n\nРасписание просмотра серий аниме составляется автоматически, основываясь на Вашем списке \"Смотрю\". Мы принципиально не просим пароль от Вашего аккаунта, поэтому поддерживаются только открытые списки. Изменить видимость списка можно в настройках Шикимори.\n\nИсключить тайтл из расписания и изменить период выхода серий (по умолчанию раз в неделю) можно, нажав на карандашик в правом верхнем углу обложки. Чтобы вернуть исключенное аниме, перейдите в Режим списка (Ctrl+M), там же доступен удобный интерфейс для управления параметрами всех Ваших аниме одновременно. Зеленым цветом подсвечиваются тайтлы, план по которым был выполнен сегодня, желтым — готовые к просмотру сегодня, красным цветом — просроченные. \n\nОдна из ключевых настроек программы — задержка онгоингов. Хотя в Японии новые серии можно посмотреть по телевизору в день выхода, для зарубежных зрителей это не так просто, поэтому по умолчанию установлена однодневная задержка между выходом новой серии и ее положением в расписании. Изменить период выхода и положение в расписании для текущих онгоингов с известными датами выхода серий можно, нажав кнопку \"Догнать\" в окошке редактирования.\n\nВы можете отключить автозапуск программы в настройках, но тогда Вы не будете получать уведомления, пока не запустите ее самостоятельно.\n\n\nЧАВО:\n\nВопрос: После некоторого времени использования программы расписание стало несбалансированным. Что делать?\nОтвет: Для тайтлов, которые стоят в неудобные дни, можно нажать кнопку \"Сброс\" в окне редактирования. Если все совсем плохо, воспользуйтесь \"Действия → Перестроить расписание\". Расписание будет составлено заново, но при этом вы потеряете все данные о текущем графике, включая задолженности!\n\nВопрос: Я посмотрел больше серий, чем нужно по плану, почему приложение не показывает, что я опережаю график?\nОтвет: Планировщик учитывает это, и обновляет прогресс тайтла так, чтобы Вы досмотрели его быстрее. :)\n\nВопрос: Приложение не запускается.\nОтвет: Проверьте значок программы в блоке фоновых приложений на панели задач. Для запуска кликните по нему два раза.\n\nВопрос: Вместо обложки аниме отображается 404-тян, хотя у тайтла есть постер на Шикимори. Как это исправить?\nОтвет: Скорее всего, это произошло из-за обрыва связи при синхронизации. Выйдите из программы (Ctrl+Q) , скопируйте в адресную строку проводника \"%APPDATA%/YO\", оказавшись там, удалите папку \"tmp\". Остальные файлы трогать не стоит, если не хотите потерять свои данные. ;)\n\nВопрос: ЧАВО не помогает, произошла неизвестная ошибка, программа упала, я нашел баг, опечатку.\nОтвет: Это замечательно! Пожалуйста, напишите о Вашей проблеме в топик ошибок, и она будет исправлена.\n\nСообщить об ошибке или предложить идею по улучшению можно в клубе темы (ссылку можно выделить и скопировать):\nhttps://shikimori.one/clubs/3417\n\nРепозиторий с исходным кодом, если Вы хотите поменять программу по своему усмотрению:\nhttps://github.com/SergejVolkov/YO";
        public static string About => "YO: Твои Онгоинги — это удобный и функциональный планировщик просмотра аниме, синхронизирующийся с Вашим списком на Шикимори. С YO Вы не только можете смотреть релизнутые тайтлы так, как будто они выходят сейчас, но и планировать просмотр текущих онгоингов, догонять их и многое другое!\n\nВыбирайте периодичность выхода серий и их количество, а умный алгоритм составит расписание наиболее эффективным образом. Каждый день приложение будет рассылать уведомления с указанием эпизодов на сегодня. Вы также можете настроить для тайтлов любые ссылки на онлайн-кинотеатры: запланированные серии всего в одном клике от Вас! А если пропустили несколько эпизодов — ничего страшного: просроченные тайтлы можно обнулить или исключить из расписания.\n\nВ приложении доступна темная тема, переключение между различными аккаунтами. Мы не собираем о Вас никакие данные, все хранится только на Вашем компьютере.";
        public static string Copyright => "YO: Your Ongoings is a strictly non-profit open-source project distributed under GNU GPLv3 (GNU General Public License 3.0). The author does not own any of the drawings and images that are used in this program except the logo icon, however, neither this program nor any of its derivatives shall not gain profit of them, so the inclusion of the aforementioned images can be considered fair use.\n\nIf you hold the rights to any of the images in this program and would like to stop their use, please contact the author.\n\nE-mail: volkovsergej17@gmail.com\nTelegram: @SergejVolkov";
        public static string Notice => "SergejVolkov, 2021";
    }

    /// <summary>
    /// Keyboard shortcuts.
    /// </summary>
    public static class Commands {
        public static RoutedCommand F5 = new RoutedCommand();
        public static RoutedCommand Esc = new RoutedCommand();
        public static RoutedCommand CtrlO = new RoutedCommand();
        public static RoutedCommand CtrlM = new RoutedCommand();
        public static RoutedCommand CtrlQ = new RoutedCommand();

        /// <summary>
        /// Initialize shortcut class.
        /// </summary>
        public static void Init() {
            F5.InputGestures.Add(new KeyGesture(Key.F5));
            Esc.InputGestures.Add(new KeyGesture(Key.Escape));
            CtrlO.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
            CtrlM.InputGestures.Add(new KeyGesture(Key.M, ModifierKeys.Control));
            CtrlQ.InputGestures.Add(new KeyGesture(Key.Q, ModifierKeys.Control));
        }
    }

    /// <summary>
    /// Day of week converter class.
    /// </summary>
    public static class Conv {
        /// <summary>
        /// Convert Sunday-Saturday format to Monday-Sunday format.
        /// </summary>
        /// <param name="international_week_day">Day of week in Sunday-Saturday format.</param>
        /// <returns>Day of week in Monday-Sunday format.</returns>
        public static int ToWeekDayRu(int international_week_day) {
            return (international_week_day - 1 + 7) % 7;
        }

        /// <summary>
        /// Convert Sunday-Saturday format to Monday-Sunday format.
        /// </summary>
        /// <param name="international_week_day">Day of week in Sunday-Saturday format.</param>
        /// <returns>Day of week in Monday-Sunday format.</returns>
        public static int ToWeekDayRu(DayOfWeek international_week_day) {
            return ToWeekDayRu(Convert.ToInt32(international_week_day));
        }
    }
}
