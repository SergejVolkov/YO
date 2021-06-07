using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YO.Modules;

namespace YO {
    /// <summary>
    /// Interaction logic for EditEntryDialog.xaml
    /// </summary>
    public partial class EditEntryDialog : Window {
        // String resources
        static string[] day_ru =
        {
            "понедельникам",
            "вторникам",
            "средам",
            "четвергам",
            "пятницам",
            "субботам",
            "воскресеньям"
        };
        public static string[] IncludedTooltipText =
        {
            "Включить",
            "Исключить"
        };

        // Entry params
        Entry entry;
        bool reset = false;
        bool is_ongoing = false, override_ongoing = false;

        /// <summary>
        /// Construct entry editing window.
        /// </summary>
        /// <param name="dark_mode">Dark theme flag.</param>
        /// <param name="language">Anime title language.</param>
        /// <param name="entry">Anime entry.</param>
        /// <param name="cover">Anime cover.</param>
        public EditEntryDialog(bool dark_mode, TitleLanguage language, Entry entry, Grid cover) {
            InitializeComponent();
            if (dark_mode)
                WeebDarkFantasy();
            string title = (language == TitleLanguage.Russian ? entry.RussianName : entry.RomajiName);
            titleTextBox.Text = this.Title = title;
            this.entry = entry;
            for (int i = 1; i <= 7; ++i) {
                periodBox.Items.Add(new ComboBoxItem {
                    Content = i.ToString(),
                    IsSelected = i == entry.Period
                });
            }
            periodBox.SelectedIndex = entry.Period - 1;
            if (entry.IsRegularOngoing) {
                periodPanel.Visibility = Visibility.Collapsed;
                episodesPanel.Visibility = Visibility.Collapsed;
                if (entry.WatchedEpisodes < entry.TotalEpisodes) {
                    buttonReset.Content = "Догнать";
                    buttonReset.ToolTip = "Догнать онгоинг";
                } else {
                    buttonReset.Visibility = Visibility.Collapsed;
                }
                is_ongoing = true;
            } else if (entry.Status == AnimeStatus.PendingOngoing && entry.OverrideRegularOngoing) {
                buttonReset.Content = "Сброс";
                buttonReset.ToolTip = "Не догонять онгоинг";
                override_ongoing = true;
                is_ongoing = true;
            }
            includedCheckBox.IsChecked = !entry.IsExcluded;
            linkTextBox.Text = entry.Href;
            episodesBox.Text = entry.EpisodesPerDay.ToString();
            UpdateCheckBoxToolTip(includedCheckBox);
            UpdateDescription();

            coverPanel.Children.Add(cover);
            coverPanel.Children.Add(new Grid {
                Height = 20
            });
            propertiesPanel.MaxWidth = this.Width - cover.Width;
            linkTextBox.Width = this.Width - cover.Width - 20 * 2 - 15;
        }

        /// <summary>
        /// Become ♂️ FULL MASTER ♂️ of your anime.
        /// </summary>
        void WeebDarkFantasy() {
            this.Background = new SolidColorBrush(Color.FromRgb(34, 34, 34));
            this.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
            this.Foreground = includedCheckBox.Foreground = Brushes.White;
            this.Resources.Clear();
        }

        public static void UpdateCheckBoxToolTip(CheckBox includedCheckBox) {
            ((ToolTip)includedCheckBox.ToolTip).Content = IncludedTooltipText[Convert.ToInt32(includedCheckBox.IsChecked == true)];
        }

        void UpdateDescription() {
            descriptionTextBox.Text = $"{entry.Type}{(entry.YearCreated != 0 ? $", {entry.YearCreated}" : "")}{(entry.IsOngoing ? ", онгоинг" : "")}\nВсего эпизодов: {entry.TotalEpisodes}\n{(entry.Score != 0 ? $"Оценка: {entry.Score}\n" : "")}{(entry.IsExcluded ? (includedCheckBox.IsChecked == true ? "Ожидает включения" : "Исключено из расписания") : (Period % 7 == 0 && entry.Period % 7 == 0 ? $"Выходит по {day_ru[Conv.ToWeekDayRu(entry.ActualWeekDay)]}" : $"Выходит {GetPeriodPhrase(Period)}"))}\nПросмотрено: {entry.WatchedEpisodes}/{entry.ExpectedEpisodes}";
        }

        string GetPeriodPhrase(int period) {
            if (period == 1) {
                return "каждый день";
            } else if (2 <= period && period <= 4) {
                return $"каждые {period} дня";
            } else {
                return $"каждые {period} дней";
            }
        }

        private void PeriodBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            UpdateDescription();
        }

        private static readonly Regex _regex = new Regex("[^0-9]+");
        private static bool IsTextAllowed(string text) => !_regex.IsMatch(text);

        private void EpisodesBox_TextInput(object sender, TextCompositionEventArgs e) {
            e.Handled = episodesBox.Text.Length >= 4 || !IsTextAllowed(e.Text);
        }

        private void EpisodesBox_TextChanged(object sender, TextChangedEventArgs e) {
            if (episodesBox.Text.Length > 4) {
                episodesBox.Text = episodesBox.Text.Substring(0, 4);
                episodesBox.CaretIndex = 4;
            }
        }

        private void IncludedCheckBox_Checked(object sender, RoutedEventArgs e) {
            UpdateCheckBoxToolTip(includedCheckBox);
            UpdateDescription();
        }

        private void ButtonEsc_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e) {
            if (Period != entry.Period || entry.IsExcluded != IsExcluded || entry.Href != Href || EpisodesPerDay != entry.EpisodesPerDay) {
                DialogResult = true;
                return;
            }
            this.Close();
        }

        private void ButtonReset_Click(object sender, RoutedEventArgs e) {
            if (is_ongoing && !override_ongoing) {
                periodPanel.Visibility = Visibility.Visible;
                episodesPanel.Visibility = Visibility.Visible;
                periodBox.SelectedIndex = 0;
                buttonReset.Content = "Сброс";
                buttonReset.ToolTip = "Не догонять онгоинг";
                override_ongoing = true;
            } else {
                override_ongoing = false;
                reset = true;
                DialogResult = true;
            }
        }

        public int Period => periodBox.SelectedIndex + 1;
        public bool IsExcluded => includedCheckBox.IsChecked == false;
        public string Href => linkTextBox.Text;
        public bool Reset => reset;
        public int EpisodesPerDay => episodesBox.Text == "" ? entry.EpisodesPerDay : int.Parse(episodesBox.Text);
        public bool OverrideRegularOngoing => override_ongoing;
    }
}
