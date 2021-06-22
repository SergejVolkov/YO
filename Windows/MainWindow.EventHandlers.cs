using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YO.Modules;

namespace YO.Windows
{
	public partial class MainWindow
	{
		private void HelpBarItem_Click(object sender, RoutedEventArgs e)
		{
			var window = new AboutWindow(_darkMode,
										 false,
										 "Помощь",
										 "Справка",
										 StringResources.Help,
										 StringResources.Notice,
										 1000)
			{
				ResizeMode = ResizeMode.CanResize,
				SizeToContent = SizeToContent.Manual,
				Height = 600,
				Owner = this
			};
			window.Show();
		}

		private void AboutBarItem_Click(object sender, RoutedEventArgs e)
		{
			var window = new AboutWindow(_darkMode,
										 true,
										 "О программе",
										 "YO: Твои Онгоинги",
										 StringResources.About,
										 StringResources.Notice)
			{
				Owner = this
			};
			window.ShowDialog();
		}

		private void CopyrightBarItem_Click(object sender, RoutedEventArgs e)
		{
			var window = new AboutWindow(_darkMode,
										 false,
										 "Правообладателям",
										 "For Right Holders",
										 StringResources.Copyright,
										 StringResources.Notice,
										 600)
			{
				Owner = this
			};
			window.ShowDialog();
		}

		private void ExcludeBarItem_Click(object sender, RoutedEventArgs e)
		{
			_refreshTimer.Stop();
			var dialog = new SelectIntDialog(_darkMode,
											 "Исключить просроченные",
											 "Исключенные аниме больше не будут показываться в расписании.\n\nИсключить все просроченные",
											 0,
											 3,
											 -1,
											 new[]
											 {
												 "на 1 эпизод и более", "на 2 эпизода и более",
												 "на 5 эпизодов и более", "на 10 эпизодов и более"
											 })
			{
				Owner = this
			};
			dialog.ShowDialog();
			if (dialog.DialogResult == true)
			{
				if (_mode == Mode.Refresh)
				{
					System.Windows.Forms.MessageBox.Show(
						"Пожалуйста, дождитесь завершения синхронизации и попробуйте снова!");
				} else
				{
					var threshold = new[] {1, 2, 5, 10}[dialog.Value];
					var overdue = _scheduler.Entries.Where(p => !p.IsExcluded
															 && p.ExpectedEpisodes - p.WatchedEpisodes >= threshold
															 && (!p.CurrentWeekDays.Contains(
																	 (int) DateTime.Now.DayOfWeek)
															  || p.ExpectedEpisodes - p.WatchedEpisodes >
																 p.EpisodesPerDay));
					var overdueCheckboxes = _listViewItems.FindAll(p => overdue.Any(q => q.Id == GetId(p)))
														  .ConvertAll(p => (CheckBox) p.Children[ListItemCheckboxIdx]);
					foreach (var box in overdueCheckboxes)
					{
						box.IsChecked = false;
					}

					UpdateCalendar();
					UpdateDataCache();
				}
			}

			_refreshTimer.Start();
		}

		private void ResetBarItem_Click(object sender, RoutedEventArgs e)
		{
			_refreshTimer.Stop();
			var dialog = new SelectIntDialog(_darkMode,
											 "Сбросить просроченные",
											 "Пересчитать расписание просроченных тайтлов.\n\nСбросить все просроченные",
											 0,
											 3,
											 -1,
											 new[]
											 {
												 "на 1 эпизод и более", "на 2 эпизода и более",
												 "на 5 эпизодов и более", "на 10 эпизодов и более"
											 })
			{
				Owner = this
			};
			dialog.ShowDialog();
			if (dialog.DialogResult == true)
			{
				if (_mode == Mode.Refresh)
				{
					System.Windows.Forms.MessageBox.Show(
						"Пожалуйста, дождитесь завершения синхронизации и попробуйте снова!");
				} else
				{
					var threshold = new int[] {1, 2, 5, 10}[dialog.Value];
					var overdue = _scheduler.Entries.Where(p => !p.IsExcluded
															 && !p.IsRegularOngoing
															 && p.ExpectedEpisodes - p.WatchedEpisodes >= threshold
															 && (!p.CurrentWeekDays.Contains(
																	 (int) DateTime.Now.DayOfWeek)
															  || p.ExpectedEpisodes - p.WatchedEpisodes >
																 p.EpisodesPerDay));
					foreach (var entry in overdue)
					{
						entry.MarkReschedule();
					}

					_scheduler.Schedule();
					UpdateCalendar();
					UpdateDataCache();
				}
			}

			_refreshTimer.Start();
		}

		private void RescheduleBarItem_Click(object sender, RoutedEventArgs e)
		{
			if (_mode == Mode.Refresh)
			{
				System.Windows.Forms.MessageBox.Show(
					"Пожалуйста, дождитесь завершения синхронизации и попробуйте снова!");
				return;
			}

			_scheduler.Reschedule();
			UpdateCalendar();
			UpdateDataCache();
		}

		private void StartupBarItem_Click(object sender, RoutedEventArgs e)
		{
			if (!_autorun)
			{
				InstallOnStartUp();
			} else
			{
				UnInstallOnStartUp();
			}

			UpdatePrefCache();
		}

		private void LanguageBarItem_Click(object sender, RoutedEventArgs e)
		{
			_language = _language == TitleLanguage.Russian
				? TitleLanguage.Romaji
				: TitleLanguage.Russian;

			UpdateTitles();
			UpdateSorting();
			UpdatePrefCache();
		}

		private void EditButton_Click(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left
			 || _mode == Mode.Refresh)
			{
				return;
			}

			_refreshTimer.Stop();
			var entry = _scheduler[GetId(sender)];
			var cover = GetCover(entry, false);
			var dialog = new EditEntryDialog(_darkMode, _language, entry, cover)
			{
				Owner = this
			};
			dialog.ShowDialog();
			if (dialog.DialogResult == true)
			{
				if (_mode == Mode.Refresh)
				{
					System.Windows.Forms.MessageBox.Show(
						"Не удалось изменить параметры!\nПожалуйста, дождитесь завершения синхронизации и попробуйте снова!");
				} else
				{
					entry.Href = dialog.Href;
					if (entry.IsExcluded != dialog.IsExcluded)
					{
						switch (entry.IsExcluded)
						{
							case false when !_excludedThisRun.ContainsKey(entry.Id):
								_excludedThisRun[entry.Id] = true;
								break;
							case true when !_excludedThisRun.ContainsKey(entry.Id):
								entry.MarkReschedule();
								break;
						}

						entry.IsExcluded = dialog.IsExcluded;
						((CheckBox) _listViewItems.Find(p => GetId(p) == entry.Id).Children[ListItemCheckboxIdx])
							.IsChecked = !entry.IsExcluded;
					}

					if (entry.Status == AnimeStatus.PendingOngoing
					 && entry.OverrideRegularOngoing
					 && dialog.Reset)
					{
						entry.Status = AnimeStatus.RegularOngoing;
						entry.OverrideRegularOngoing = false;
						entry.Period = 7;
						entry.EpisodesPerDay = 1;
						((ComboBox) _listViewItems.Find(p => GetId(p) == entry.Id).Children[ListItemPeriodIdx])
							.Visibility = Visibility.Collapsed;
					} else if (!(entry.Status == AnimeStatus.RegularOngoing
							  && dialog.Reset))
					{
						if (entry.Status == AnimeStatus.RegularOngoing
						 && dialog.OverrideRegularOngoing)
						{
							entry.Status = AnimeStatus.PendingOngoing;
							entry.OverrideRegularOngoing = true;
							entry.MarkReschedule();
							((ComboBox) _listViewItems.Find(p => GetId(p) == entry.Id).Children[ListItemPeriodIdx])
								.Visibility = Visibility.Visible;
						}

						if (entry.Period != dialog.Period)
						{
							entry.MarkReschedule();
							entry.Period = dialog.Period;
							((ComboBox) _listViewItems.Find(p => GetId(p) == entry.Id).Children[ListItemPeriodIdx])
								.SelectedIndex = entry.Period - 1;
						}

						if (entry.EpisodesPerDay != dialog.EpisodesPerDay && dialog.EpisodesPerDay > 0)
						{
							entry.MarkReschedule();
							entry.EpisodesPerDay = dialog.EpisodesPerDay;
						}

						if (dialog.Reset)
							entry.MarkReschedule();
						_scheduler.Schedule();
					}

					UpdateCalendar();
					UpdateDataCache();
				}
			}

			_refreshTimer.Start();
		}

		private void Period_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var entry = _scheduler[GetId(sender)];
			var newPeriod = ((ComboBox) sender).SelectedIndex + 1;

			if (entry.Period != newPeriod)
			{
				entry.MarkReschedule();
				entry.Period = newPeriod;
				_scheduler.Schedule();
				UpdateCalendar();
				UpdateDataCache();
			}
		}

		private void Isincluded_Checked(object sender, RoutedEventArgs e)
		{
			var entry = _scheduler[GetId(sender)];
			if (entry.IsExcluded == (((CheckBox) sender).IsChecked == false))
			{
				return;
			}

			switch (entry.IsExcluded)
			{
				case false when !_excludedThisRun.ContainsKey(entry.Id):
					_excludedThisRun[entry.Id] = true;
					break;
				case true when !_excludedThisRun.ContainsKey(entry.Id):
					entry.MarkReschedule();
					break;
			}

			entry.IsExcluded = ((CheckBox) sender).IsChecked == false;
			if (!_excludingTaskRunning)
			{
				_scheduler.Schedule();
				UpdateCalendar();
				UpdateDataCache();
			}
		}

		private void SelectAllButton_Click(object sender, RoutedEventArgs e)
		{
			if (_mode == Mode.Refresh)
			{
				System.Windows.Forms.MessageBox.Show("Пожалуйста, дождитесь завершения синхронизации!");
				return;
			}

			if (!_excludingTaskRunning)
			{
				_excludingTaskRunning = true;
				foreach (Grid item in listPanel.Children)
				{
					((CheckBox) item.Children[ListItemCheckboxIdx]).IsChecked = _excludedAll;
				}

				_scheduler.Schedule();
				UpdateCalendar();
				UpdateDataCache();
				_excludingTaskRunning = false;
			}
		}

		private void DelayBarItem_Click(object sender, RoutedEventArgs e)
		{
			_refreshTimer.Stop();
			var dialog = new SelectIntDialog(_darkMode,
											 "Выбор задержки онгоингов",
											 "Онгоинги отображаются в расписании с задержкой после выхода нового эпизода, так как возможность посмотреть его в день выхода ограничена. Нулевая задержка означает, что серии онгоингов будут ставиться на день их выхода.\n\nВнимание!!! Изменение задержки ведет к потере текущего расписания! Отменить данное действие невозможно.\n\nУкажите число дней, на которое хотите откладывать онгоинги:",
											 0,
											 6,
											 _realOngoingDelay)
			{
				Owner = this
			};
			dialog.ShowDialog();
			if (dialog.DialogResult == true)
			{
				if (_mode == Mode.Refresh)
				{
					System.Windows.Forms.MessageBox.Show(
						"Пожалуйста, дождитесь завершения синхронизации и попробуйте снова!");
				} else
				{
					Scheduler.RealOngoingDelay = _realOngoingDelay = dialog.Value;
					_scheduler.Reschedule();
					UpdateCalendar();
					UpdateCache();
				}
			}

			_refreshTimer.Start();
		}

		private void SignOutBarItem_Click(object sender, RoutedEventArgs e)
		{
			if (_mode == Mode.Refresh)
			{
				System.Windows.Forms.MessageBox.Show("Пожалуйста, дождитесь завершения синхронизации!");
			} else
			{
				SignInMode();
			}
		}

		private void ViewBarItem_Click(object sender, RoutedEventArgs e)
		{
			_listMode = !_listMode;
			if (_listMode)
			{
				listView.Visibility = Visibility.Visible;
				mainScrollView.Visibility = Visibility.Collapsed;
				viewBarItem.Header = "Режим календаря";
			} else
			{
				listView.Visibility = Visibility.Collapsed;
				mainScrollView.Visibility = Visibility.Visible;
				viewBarItem.Header = "Режим списка";
			}
		}

		private void ScoreSortBarItem_Click(object sender, RoutedEventArgs e)
		{
			if (_sortingMode != SortingMode.Score)
			{
				yearSortBarItem.IsChecked = false;
				alphabetSortBarItem.IsChecked = false;
				progressSortBarItem.IsChecked = false;
				UnCheckSortButton(buttonSortYear);
				UnCheckSortButton(buttonSortTitle);
				UnCheckSortButton(buttonSortProgress);
				CheckSortButton(buttonSortScore);
				_sortingMode = SortingMode.Score;
				_watchingPartUrl = WatchingScorePartUrl;
				UpdateWatchingUrl();
				UpdateSorting();
				UpdatePrefCache();
			}

			scoreSortBarItem.IsChecked = true;
		}

		private void YearSortBarItem_Click(object sender, RoutedEventArgs e)
		{
			if (_sortingMode != SortingMode.Year)
			{
				scoreSortBarItem.IsChecked = false;
				alphabetSortBarItem.IsChecked = false;
				progressSortBarItem.IsChecked = false;
				UnCheckSortButton(buttonSortScore);
				UnCheckSortButton(buttonSortTitle);
				UnCheckSortButton(buttonSortProgress);
				CheckSortButton(buttonSortYear);
				_sortingMode = SortingMode.Year;
				_watchingPartUrl = WatchingYearPartUrl;
				UpdateWatchingUrl();
				UpdateSorting();
				UpdatePrefCache();
			}

			yearSortBarItem.IsChecked = true;
		}

		private void ProgressSortBarItem_Click(object sender, RoutedEventArgs e)
		{
			if (_sortingMode != SortingMode.Progress)
			{
				yearSortBarItem.IsChecked = false;
				scoreSortBarItem.IsChecked = false;
				alphabetSortBarItem.IsChecked = false;
				UnCheckSortButton(buttonSortYear);
				UnCheckSortButton(buttonSortTitle);
				UnCheckSortButton(buttonSortScore);
				CheckSortButton(buttonSortProgress);
				_sortingMode = SortingMode.Progress;
				_watchingPartUrl = WatchingProgressPartUrl;
				UpdateWatchingUrl();
				UpdateSorting();
				UpdatePrefCache();
			}

			progressSortBarItem.IsChecked = true;
		}

		private void AlphabetSortBarItem_Click(object sender, RoutedEventArgs e)
		{
			if (_sortingMode != SortingMode.Alphabet)
			{
				yearSortBarItem.IsChecked = false;
				scoreSortBarItem.IsChecked = false;
				progressSortBarItem.IsChecked = false;
				UnCheckSortButton(buttonSortYear);
				UnCheckSortButton(buttonSortScore);
				UnCheckSortButton(buttonSortProgress);
				CheckSortButton(buttonSortTitle);
				_sortingMode = SortingMode.Alphabet;
				_watchingPartUrl = WatchingAlphabetPartUrl;
				UpdateWatchingUrl();
				UpdateSorting();
				UpdatePrefCache();
			}

			alphabetSortBarItem.IsChecked = true;
		}

		private void WeekStartBarItem_Click(object sender, RoutedEventArgs e)
		{
			_weekStartNow = !_weekStartNow;
			UpdateCalendar();
			UpdatePrefCache();
		}

		private void DarkBarItem_Click(object sender, RoutedEventArgs e)
		{
			if (!_darkMode) DarkMode();
			else LightMode();
			UpdateColors();
			UpdatePrefCache();
		}

		private void Refresh_Timer_Tick(object sender, EventArgs e)
		{
			RefreshAsync();
		}

		private void Animation_Timer_Tick(object sender, EventArgs e)
		{
			_currentFrame = (_currentFrame + 1) % _backgroundKeyFrames.Count;
			syncGrid.Background = _backgroundKeyFrames[_currentFrame];
		}

		private void RefreshBarItem_Click(object sender, RoutedEventArgs e)
		{
			RefreshAsync();
		}

		private void ShikiBarItem_Click(object sender, RoutedEventArgs e)
		{
			OpenShiki();
		}

		private void ExitBarItem_Click(object sender, RoutedEventArgs e)
		{
			Exit();
		}

		private void AccountsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (accountsBox.SelectedIndex < accountsBox.Items.Count - 1)
			{
				nameTextBox.Text = (string) ((ComboBoxItem) accountsBox.SelectedItem).Content;
				nameTextBox.CaretIndex = nameTextBox.Text.Length;
			} else
			{
				nameTextBox.Visibility = Visibility.Visible;
				accountsBox.Visibility = Visibility.Collapsed;
			}
		}

		private void SignInButtonOK_Click(object sender, RoutedEventArgs e)
		{
			if (_mode == Mode.SignIn && nameTextBox.Text != "")
			{
				using (var client = new WebClient())
				{
					client.Encoding = System.Text.Encoding.UTF8;
					if (!CheckForInternetConnection(5000))
					{
						System.Windows.Forms.MessageBox.Show(
							"Вы оффлайн! Чтобы продолжить,\nпожалуйста, подключитесь к интернету...");
						return;
					}

					try
					{
						client.DownloadString(ShikiUrl + WebUtility.UrlEncode(nameTextBox.Text) + _watchingPartUrl);
					} catch
					{
						System.Windows.Forms.MessageBox.Show(
							"Неверное имя пользователя или закрытый список!\nПожалуйста, укажите имя существующего аккаунта с открытым списком...");
						return;
					}
				}

				_listMode = false;
				NormalMode();
				if (_accountName != nameTextBox.Text)
				{
					UpdateDataCache();
					_accountName = nameTextBox.Text;
					UpdateWatchingUrl();
					try
					{
						LoadSchedule();
					} catch
					{
						_scheduler.Clear();
					}

					SyncMode();
					RefreshAsync();
				}
			}
		}

		private void NameTextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			if (nameTextBox.Foreground != _mainFontBrush)
			{
				nameTextBox.Foreground = _mainFontBrush;
				nameTextBox.Text = "";
			}
		}

		private void NameTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (nameTextBox.Text == "")
			{
				nameTextBox.Foreground = _inactiveTextboxBrush;
				nameTextBox.Text = "Наберите имя здесь...";
			}
		}

		private void SignInButtonEsc_Click(object sender, RoutedEventArgs e)
		{
			if (_mode == Mode.SignIn && _accountName != "")
			{
				NormalMode();
			} else if (_mode != Mode.SignIn)
			{
				//this.Close();
			}
		}

		private void LinkButton_MouseEnter(object sender, MouseEventArgs e)
		{
			((TextBlock) sender).Foreground = _linkActiveBrush;
			((TextBlock) sender).TextDecorations = TextDecorations.Underline;
		}

		private void LinkButton_MouseLeave(object sender, MouseEventArgs e)
		{
			((TextBlock) sender).Foreground = _linkNormalBrush;
			((TextBlock) sender).TextDecorations = null;
		}

		private void OpenNiClick(object sender, EventArgs e)
		{
			if (Visibility == Visibility.Hidden)
			{
				Show();
				ShowInTaskbar = true;
				ScrollToCurrent();
			} else if (WindowState == WindowState.Minimized)
			{
				WindowState = WindowState.Normal;
			} else
			{
				Activate();
			}
		}

		private void ShikiNiClick(object sender, EventArgs e)
		{
			OpenShiki();
		}

		private void ExitNiClick(object sender, EventArgs e)
		{
			Exit();
		}

		private void Link_Click(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left)
				return;
			try
			{
				OpenUrl(_scheduler[GetId(sender)].Href);
			} catch
			{
				System.Windows.Forms.MessageBox.Show("Указанная ссылка недействительна!");
			}

			e.Handled = true;
		}

		private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (_listMode && searchTextBox.Foreground != _inactiveTextboxBrush)
			{
				FilterList(searchTextBox.Text.ToLower());
			}
		}

		private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			searchTextBox.Foreground = _mainFontBrush;
			searchTextBox.Text = "";
		}

		private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (searchTextBox.Text == "")
			{
				searchTextBox.Foreground = _inactiveTextboxBrush;
				searchTextBox.Text = "Поиск...";
			}
		}

		private void ListItem_MouseLeave(object sender, MouseEventArgs e)
		{
			((Grid) sender).Background = GetHighlightBrush(_scheduler[GetId(sender)]);
		}

		private void ListItem_MouseEnter(object sender, MouseEventArgs e)
		{
			((Grid) sender).Background = _grayHighlightBrush;
		}

		private void Edit_button_MouseEnter(object sender, MouseEventArgs e)
		{
			((Border) sender).Background = _editButtonActiveBrush;
		}

		private void Edit_button_MouseLeave(object sender, MouseEventArgs e)
		{
			((Border) sender).Background = _editButtonNormalBrush;
		}

		private void Cover_MouseEnter(object sender, MouseEventArgs e)
		{
			if (_mode != Mode.Refresh)
				((Grid) sender).Children[CoverButtonIdx].Visibility = Visibility.Visible;
		}

		private void Cover_MouseLeave(object sender, MouseEventArgs e)
		{
			((Grid) sender).Children[CoverButtonIdx].Visibility = Visibility.Hidden;
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (_mode != Mode.SignIn)
				ScrollToCurrent();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (_running)
			{
				Visibility = Visibility.Hidden;
				ShowInTaskbar = false;
				e.Cancel = true;
			} else if (_mode == Mode.Refresh)
			{
				System.Windows.Forms.MessageBox.Show("Пожалуйста, дождитесь завершения синхронизации!");
				_running = true;
				e.Cancel = true;
			}
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			if (_ni != null)
			{
				_ni.Visible = false;
				_ni.Dispose();
				UpdateCache();
			}

			_instanceStream.Close();
			_instanceStream.Dispose();
		}
	}
}