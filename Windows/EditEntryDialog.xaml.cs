using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YO.Modules;

namespace YO.Windows
{
	/// <summary>
	/// Interaction logic for EditEntryDialog.xaml
	/// </summary>
	// TODO: Refactor in MVVM pattern
	public partial class EditEntryDialog
	{
		private static readonly Regex Regex = new Regex("[^0-9]+");

		// String resources
		// TODO: Separate it to resources
		private static readonly string[] DayRu =
		{
			"понедельникам",
			"вторникам",
			"средам",
			"четвергам",
			"пятницам",
			"субботам",
			"воскресеньям"
		};

		// TODO: Separate it to resources
		private static readonly string[] IncludedTooltipText =
		{
			"Включить",
			"Исключить"
		};

		// Entry params
		private readonly Entry _entry;
		private readonly bool _isOngoing;
		private bool _overrideOngoing;

		/// <summary>
		/// Construct entry editing window.
		/// </summary>
		/// <param name="darkMode">Dark theme flag.</param>
		/// <param name="language">Anime title language.</param>
		/// <param name="entry">Anime entry.</param>
		/// <param name="cover">Anime cover.</param>
		public EditEntryDialog(bool darkMode,
							   TitleLanguage language,
							   Entry entry,
							   Grid cover)
		{
			InitializeComponent();
			if (darkMode)
			{
				WeebDarkFantasy();
			}

			var title = language == TitleLanguage.Russian ? entry.RussianName : entry.RomajiName;
			titleTextBox.Text = Title = title;
			_entry = entry;
			for (var i = 1; i <= 7; ++i)
			{
				periodBox.Items.Add(new ComboBoxItem
				{
					Content = i.ToString(),
					IsSelected = i == entry.Period
				});
			}

			periodBox.SelectedIndex = entry.Period - 1;
			if (entry.IsRegularOngoing)
			{
				periodPanel.Visibility = Visibility.Collapsed;
				episodesPanel.Visibility = Visibility.Collapsed;
				if (entry.WatchedEpisodes < entry.TotalEpisodes)
				{
					buttonReset.Content = "Догнать";
					buttonReset.ToolTip = "Догнать онгоинг";
				} else
				{
					buttonReset.Visibility = Visibility.Collapsed;
				}

				_isOngoing = true;
			} else if (entry.Status == AnimeStatus.PendingOngoing && entry.OverrideRegularOngoing)
			{
				buttonReset.Content = "Сброс";
				buttonReset.ToolTip = "Не догонять онгоинг";
				_overrideOngoing = true;
				_isOngoing = true;
			}

			includedCheckBox.IsChecked = !entry.IsExcluded;
			linkTextBox.Text = entry.Href;
			episodesBox.Text = entry.EpisodesPerDay.ToString();
			UpdateCheckBoxToolTip(includedCheckBox);
			UpdateDescription();

			coverPanel.Children.Add(cover);
			coverPanel.Children.Add(new Grid
			{
				Height = 20
			});
			propertiesPanel.MaxWidth = Width - cover.Width;
			linkTextBox.Width = Width - cover.Width - 20 * 2 - 15;
		}

		public bool Reset { get; private set; }
		public int Period 
			=> periodBox.SelectedIndex + 1;
		public bool IsExcluded 
			=> includedCheckBox.IsChecked == false;
		public string Href 
			=> linkTextBox.Text;
		public int EpisodesPerDay 
			=> episodesBox.Text == "" 
				? _entry.EpisodesPerDay 
				: int.Parse(episodesBox.Text);
		public bool OverrideRegularOngoing 
			=> _overrideOngoing;

		public static void UpdateCheckBoxToolTip(CheckBox includedCheckBox)
		{
			((ToolTip) includedCheckBox.ToolTip).Content =
				IncludedTooltipText[Convert.ToInt32(includedCheckBox.IsChecked == true)];
		}

		private static bool IsTextAllowed(string text) 
			=> !Regex.IsMatch(text);
		
		private static string GetPeriodPhrase(int period)
		{
			if (period == 1)
			{
				return "каждый день";
			}

			if (2 <= period && period <= 4)
			{
				return $"каждые {period} дня";
			}

			return $"каждые {period} дней";
		}
		
		/// <summary>
		/// Become ♂️ FULL MASTER ♂️ of your anime.
		/// </summary>
		private void WeebDarkFantasy()
		{
			Background = new SolidColorBrush(Color.FromRgb(34, 34, 34));
			BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
			Foreground = includedCheckBox.Foreground = Brushes.White;
			Resources.Clear();
		}
		
		private void UpdateDescription()
		{
			descriptionTextBox.Text =
				$"{_entry.Type}{(_entry.YearCreated != 0 ? $", {_entry.YearCreated}" : "")}{(_entry.IsOngoing ? ", онгоинг" : "")}\nВсего эпизодов: {_entry.TotalEpisodes}\n{(_entry.Score != 0 ? $"Оценка: {_entry.Score}\n" : "")}{(_entry.IsExcluded ? (includedCheckBox.IsChecked == true ? "Ожидает включения" : "Исключено из расписания") : (Period % 7 == 0 && _entry.Period % 7 == 0 ? $"Выходит по {DayRu[WeekDayConverter.ToWeekDayRu(_entry.ActualWeekDay)]}" : $"Выходит {GetPeriodPhrase(Period)}"))}\nПросмотрено: {_entry.WatchedEpisodes}/{_entry.ExpectedEpisodes}";
		}

		private void PeriodBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateDescription();
		}

		private void EpisodesBox_TextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = episodesBox.Text.Length >= 4 || !IsTextAllowed(e.Text);
		}

		private void EpisodesBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (episodesBox.Text.Length > 4)
			{
				episodesBox.Text = episodesBox.Text.Substring(0, 4);
				episodesBox.CaretIndex = 4;
			}
		}

		private void IncludedCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			UpdateCheckBoxToolTip(includedCheckBox);
			UpdateDescription();
		}

		private void ButtonEsc_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void ButtonOK_Click(object sender, RoutedEventArgs e)
		{
			if (Period != _entry.Period || _entry.IsExcluded != IsExcluded || _entry.Href != Href ||
				EpisodesPerDay != _entry.EpisodesPerDay)
			{
				DialogResult = true;
				return;
			}

			Close();
		}

		private void ButtonReset_Click(object sender, RoutedEventArgs e)
		{
			if (_isOngoing && !_overrideOngoing)
			{
				periodPanel.Visibility = Visibility.Visible;
				episodesPanel.Visibility = Visibility.Visible;
				periodBox.SelectedIndex = 0;
				buttonReset.Content = "Сброс";
				buttonReset.ToolTip = "Не догонять онгоинг";
				_overrideOngoing = true;
			} else
			{
				_overrideOngoing = false;
				Reset = true;
				DialogResult = true;
			}
		}
	}
}