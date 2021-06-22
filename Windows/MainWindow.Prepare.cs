using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using YO.Modules;

namespace YO.Windows
{
	public partial class MainWindow
	{
		/// <summary>
		/// Prevent app from having two copies running concurrently.
		/// </summary>
		private void PrepareInstance()
		{
			if (CheckInstance())
			{
				_running = false;
				Close();
			}
		}

		/// <summary>
		/// Load UI elements.
		/// </summary>
		private void PrepareUi()
		{
			_editIconImage = new BitmapImage(new Uri(UiImgDir + "pencil.png"));
			_posters = new Dictionary<int, Poster>();
			_listViewItems = new List<Grid>();
			_excludedThisRun = new Dictionary<int, bool>();
			_calendarItems = new List<Grid>[7];
			_slots = new WrapPanel[7];
			_nothingHerePlaceholders = new Grid[7];
			_borders = new Grid[7];
			for (var i = 0; i < 7; ++i)
			{
				_calendarItems[i] = new List<Grid>();
				_nothingHerePlaceholders[i] = GetPlaceholder();
				_borders[i] = new Grid
				{
					Height = SlotBorderHeight
				};
				_borders[i].ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(SlotHorizGap)});
				_borders[i].ColumnDefinitions.Add(new ColumnDefinition());
				var weekday = new Label
				{
					FontSize = 30,
					FontWeight = FontWeights.Bold,
					VerticalAlignment = VerticalAlignment.Bottom
				};
				var ldate = new Label
				{
					FontSize = mainWindow.FontSize,
					VerticalAlignment = VerticalAlignment.Bottom,
					HorizontalAlignment = HorizontalAlignment.Right
				};
				Grid.SetColumn(weekday, 1);
				Grid.SetColumn(ldate, 1);
				_borders[i].Children.Add(weekday);
				_borders[i].Children.Add(ldate);
				_slots[i] = new WrapPanel()
				{
					MinHeight = SlotHeight,
					Orientation = Orientation.Horizontal
				};
				calendarPanel.Children.Add(_borders[i]);
				calendarPanel.Children.Add(_slots[i]);
				calendarPanel.Children.Add(_nothingHerePlaceholders[i]);
			}

			listPlaceholder.Children.Add(GetPlaceholder());
			listPlaceholder = (Grid) listPlaceholder.Children[0];
			listTopBar.Margin = new Thickness(0, 0, SystemParameters.VerticalScrollBarWidth, 0);

			PrepareTrayIcon();
			Commands.Init();
		}

		/// <summary>
		/// Create notification icon.
		/// </summary>
		private void PrepareTrayIcon()
		{
			_normalIcon =
				new System.Drawing.Icon(Application.GetResourceStream(new Uri(UiImgDir + "ico/normal_ni.ico"))
												   .Stream);
			_noConnectionIcon =
				new System.Drawing.Icon(
					Application.GetResourceStream(new Uri(UiImgDir + "ico/offline_ni.ico")).Stream);
			_busyIcon = new System.Drawing.Icon(Application.GetResourceStream(new Uri(UiImgDir + "ico/busy_ni.ico"))
														   .Stream);
			;
			_ni = new System.Windows.Forms.NotifyIcon
			{
				Icon = _normalIcon,
				Text = NormalStatus,
				Visible = true
			};
			_ni.DoubleClick += OpenNiClick;
			var niItems = new[]
			{
				new System.Windows.Forms.MenuItem("Развернуть", OpenNiClick),
				new System.Windows.Forms.MenuItem("Открыть Шикимори...", ShikiNiClick),
				new System.Windows.Forms.MenuItem("Выйти", ExitNiClick)
			};
			_ni.ContextMenu = new System.Windows.Forms.ContextMenu(niItems);
			_ni.BalloonTipClicked += OpenNiClick;
		}

		/// <summary>
		/// Load cache and apply preferences.
		/// </summary>
		private void PrepareCache()
		{
			if (CheckFirstRun())
			{
				InstallOnStartUp();
				_preferences.SetValue("Autorun", _autorun.ToString());
			}

			SetWindowState();
			if (_preferences.CheckValue("Sorting", "Score"))
			{
				_sortingMode = SortingMode.Score;
				_watchingPartUrl = WatchingScorePartUrl;
				scoreSortBarItem.IsChecked = true;
				CheckSortButton(buttonSortScore);
			} else if (_preferences.CheckValue("Sorting", "Year"))
			{
				_sortingMode = SortingMode.Year;
				_watchingPartUrl = WatchingYearPartUrl;
				yearSortBarItem.IsChecked = true;
				CheckSortButton(buttonSortYear);
			} else if (_preferences.CheckValue("Sorting", "Progress"))
			{
				_sortingMode = SortingMode.Progress;
				_watchingPartUrl = WatchingProgressPartUrl;
				progressSortBarItem.IsChecked = true;
				CheckSortButton(buttonSortProgress);
			} else
			{
				_sortingMode = SortingMode.Alphabet;
				_watchingPartUrl = WatchingAlphabetPartUrl;
				alphabetSortBarItem.IsChecked = true;
				CheckSortButton(buttonSortTitle);
			}

			if (_preferences.CheckValue("Language", "Russian"))
			{
				_language = TitleLanguage.Russian;
			} else
			{
				_language = TitleLanguage.Romaji;
				languageBarItem.IsChecked = true;
			}

			weekStartBarItem.IsChecked = _weekStartNow = _preferences.CheckValue("StartWeekFrom", "Now");
			darkBarItem.IsChecked = _darkMode = _preferences.CheckValue("Theme", "Dark");
			startupBarItem.IsChecked = _autorun = _preferences.IsValueTrue("Autorun");
			_realOngoingDelay = _preferences.GetIntValue("RealOngoingDelay");
			_lastFullRefresh = DateTime.Parse(_preferences.GetValue("LastFullRefresh"));
			_fullRefreshed = DateTime.Now.Subtract(_lastFullRefresh).Days < FullRefreshPeriod;
			_lastStartupNotification = DateTime.Parse(_preferences.GetValue("LastStartupNotification"));
			_startNotified = DateTime.Now.DayOfYear == _lastStartupNotification.DayOfYear
						  && DateTime.Now.Year == _lastStartupNotification.Year;
			_lastEveningNotification = DateTime.Parse(_preferences.GetValue("LastEveningNotification"));
			_eveningNotified = DateTime.Now.DayOfYear == _lastEveningNotification.DayOfYear
							&& DateTime.Now.Year == _lastEveningNotification.Year;

			_accountName = _data.GetContent("AccountInfo").GetValue("Name");
			UpdateWatchingUrl();
			_totalEpisodes = _data.GetContent("StatsInfo").GetIntValue("TotalEpisodes");
			if (_accountName != "")
			{
				try
				{
					LoadSchedule();
				} catch
				{
					_accountName = "";
					_scheduler = new Scheduler(_realOngoingDelay);
				}
			} else
			{
				_scheduler = new Scheduler(_realOngoingDelay);
			}
		}

		/// <summary>
		/// Start minimized if run by system, create timers.
		/// </summary>
		private void PrepareSystem()
		{
			if (Environment.CurrentDirectory.ToLower().EndsWith("system32"))
			{
				Visibility = Visibility.Hidden;
				ShowInTaskbar = false;
			}

			_refreshTimer = new DispatcherTimer
			{
				Interval = new TimeSpan(0, RefreshInterval, 0)
			};
			_refreshTimer.Tick += Refresh_Timer_Tick;
		}

		/// <summary>
		/// Apply settings from cache to UI.
		/// </summary>
		private void PrepareCachedUi()
		{
			if (_darkMode) DarkMode();
			else AssignLightModeDependencies();
			if (_accountName == "")
			{
				SignInMode();
			} else
			{
				UpdateUi();
			}
		}
	}
}