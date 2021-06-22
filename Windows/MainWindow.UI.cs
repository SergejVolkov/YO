using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using YO.Modules;

namespace YO.Windows
{
	public partial class MainWindow
	{
		/// <summary>
		/// Rebuild UI from scratch after major scheduler update or other destructive actions.
		/// </summary>
		private void UpdateUi()
		{
			for (var i = 0; i < 7; ++i)
			{
				_calendarItems[i].Clear();
			}

			var maxWidth = new double[7];
			UidCounter = 0;
			_listViewItems.Clear();
			_excludedAll = true;
			foreach (var entry in _scheduler.Entries)
			{
				// Calendar view
				for (var rWeekDay = 0; rWeekDay < 7; ++rWeekDay)
				{
					var cover = GetCover(entry);
					_calendarItems[rWeekDay].Add(cover);
					if (maxWidth[rWeekDay] < cover.Width)
					{
						maxWidth[rWeekDay] = cover.Width;
					}
				}

				// List view
				var listItem = GetListItem(entry);
				_listViewItems.Add(listItem);
				if (!entry.IsExcluded)
				{
					_excludedAll = false;
				}
			}

			listPlaceholder.Visibility = _listViewItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
			totalEntriesTextBlock.Text = $"Всего тайтлов: {_scheduler.Count}";
			ongoingsEntriesTextBlock.Text = $"Онгоингов: {_scheduler.Entries.Count(p => p.IsOngoing)}";
			totalEpisodesTextBlock.Text = $"Просмотрено эпизодов за все время: {_totalEpisodes}";

			// Normalize width of covers
			for (var i = 0; i < 7; ++i)
			{
				foreach (var cover in _calendarItems[i])
				{
					cover.Width = maxWidth[i];
				}
			}

			UpdateUiEasy();
		}

		/// <summary>
		/// Load anime poster from disk.
		/// </summary>
		/// <param name="id"></param>
		private void LoadPoster(int id)
		{
			BitmapSource poster = new BitmapImage(new Uri(_tmpdir + id + ".jpg"));
			if (poster.PixelWidth / (double) poster.PixelHeight > MaxPosterAspectRatio)
			{
				var cropWidth = (int) (poster.PixelHeight * MaxPosterAspectRatio);
				poster = new CroppedBitmap(
					poster, new Int32Rect((poster.PixelWidth - cropWidth) / 2, 0, cropWidth, poster.PixelHeight));
			}

			_posters[id] = new Poster(poster);
		}

		/// <summary>
		/// Build anime cover object displayed in calendar and editing dialog.
		/// </summary>
		/// <param name="entry">Anime entry.</param>
		/// <param name="title_and_button">Add link title and edit button.</param>
		/// <returns>Cover object.</returns>
		private Grid GetCover(Entry entry, bool titleAndButton = true)
		{
			var cover = new Grid
			{
				Height = SlotHeight - (titleAndButton ? 0 : SlotTitleHeight),
				Uid = $"cover{UidCounter}_{entry.Id}",
				ToolTip = new ToolTip
				{
					Content = titleAndButton ? GetEntryToolTip(entry) : entry.Href
				}
			};
			cover.RowDefinitions.Add(new RowDefinition {Height = new GridLength(SlotVertGap)});
			cover.RowDefinitions.Add(new RowDefinition());
			cover.RowDefinitions.Add(new RowDefinition {Height = new GridLength(SlotVertGap)});
			if (titleAndButton)
			{
				cover.RowDefinitions.Add(new RowDefinition {Height = new GridLength(SlotTitleHeight)});
			}

			cover.ColumnDefinitions.Add(new ColumnDefinition());
			cover.ColumnDefinitions.Add(new ColumnDefinition());
			cover.ColumnDefinitions.Add(new ColumnDefinition());
			if (titleAndButton)
			{
				cover.MouseEnter += Cover_MouseEnter;
				cover.MouseLeave += Cover_MouseLeave;
			}

			if (!_posters.ContainsKey(entry.Id) || !_posters[entry.Id].IsLoaded)
			{
				try
				{
					LoadPoster(entry.Id);
				} catch
				{
					_posters[entry.Id] = new Poster(new BitmapImage(new Uri(UiImgDir + "no_cover.jpg")), false);
				}
			}

			var poster = new Image
			{
				Source = _posters[entry.Id].Source,
				Cursor = Cursors.Hand,
				Uid = $"poster{UidCounter}_{entry.Id}"
			};
			RenderOptions.SetBitmapScalingMode(poster, BitmapScalingMode.HighQuality);
			RenderOptions.SetEdgeMode(poster, EdgeMode.Aliased);
			poster.Effect = new DropShadowEffect
			{
				BlurRadius = ShadowBlurRadius,
				ShadowDepth = 0,
				Color = Colors.Black,
				Opacity = 0.7
			};
			poster.MouseUp += Link_Click;
			Grid.SetRow(poster, 1);
			Grid.SetColumn(poster, 1);
			cover.Children.Add(poster);

			var titleWidth = ((BitmapSource) poster.Source).PixelWidth *
				(SlotHeight - SlotVertGap * 2 - SlotTitleHeight) / ((BitmapSource) poster.Source).PixelHeight;
			cover.ColumnDefinitions[1].Width = new GridLength(titleWidth);
			cover.Width = titleWidth + SlotHorizGap * 2;

			if (titleAndButton)
			{
				var title = GetLinkTitle(entry);
				title.MaxWidth = titleWidth;
				title.MaxHeight = SlotTitleHeight;
				title.HorizontalAlignment = HorizontalAlignment.Left;
				title.VerticalAlignment = VerticalAlignment.Top;
				Grid.SetRow(title, 3);
				Grid.SetColumn(title, 1);
				var editButton = new Border
				{
					Background = _editButtonNormalBrush,
					Visibility = Visibility.Hidden,
					HorizontalAlignment = HorizontalAlignment.Right,
					VerticalAlignment = VerticalAlignment.Top,
					Uid = $"editicon{UidCounter}_{entry.Id}",
					Cursor = Cursors.Hand,
					ToolTip = new ToolTip
					{
						Content = "Редактировать..."
					}
				};
				editButton.MouseUp += EditButton_Click;
				editButton.MouseEnter += Edit_button_MouseEnter;
				editButton.MouseLeave += Edit_button_MouseLeave;
				Grid.SetRow(editButton, 1);
				Grid.SetColumn(editButton, 1);
				var editIcon = new Image
				{
					Source = _editIconImage,
					Height = EditIconHeight - EditIconGap * 2,
					Margin = new Thickness(EditIconGap, EditIconGap, EditIconGap, EditIconGap)
				};
				RenderOptions.SetBitmapScalingMode(editIcon, BitmapScalingMode.HighQuality);
				RenderOptions.SetEdgeMode(editIcon, EdgeMode.Aliased);
				editButton.Child = editIcon;

				cover.Children.Add(editButton);
				cover.Children.Add(title);
			}

			return cover;
		}

		/// <summary>
		/// Build list item object with checkbox, buttons, info and stuff.
		/// </summary>
		/// <param name="entry">Anime entry.</param>
		/// <returns>List item object.</returns>
		private Grid GetListItem(Entry entry)
		{
			var item = new Grid
			{
				Uid = $"listitem_{entry.Id}",
				Height = 28,
				Cursor = Cursors.Hand,
				ToolTip = new ToolTip
				{
					Content = "Редактировать..."
				}
			};
			item.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(40)});
			item.ColumnDefinitions.Add(new ColumnDefinition());
			item.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(70)});
			item.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(80)});
			item.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(60)});
			item.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(80)});
			item.MouseUp += EditButton_Click;
			item.MouseEnter += ListItem_MouseEnter;
			item.MouseLeave += ListItem_MouseLeave;
			var isincluded = new CheckBox
			{
				Uid = $"ischecked_{entry.Id}",
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
			var title = GetLinkTitle(entry);
			title.Margin = new Thickness(10, 0, 0, 0);
			title.Height = 24;
			title.HorizontalAlignment = HorizontalAlignment.Left;
			title.VerticalAlignment = VerticalAlignment.Bottom;
			title.ToolTip = new ToolTip
			{
				Content = entry.Href
			};
			Grid.SetColumn(title, 1);
			var score = new TextBlock
			{
				Text = entry.Score == 0 ? "-" : entry.Score.ToString(),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
				Height = 24
			};
			Grid.SetColumn(score, 2);
			var progress = new TextBlock
			{
				Text = GetProgress(entry),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
				Height = 24
			};
			Grid.SetColumn(progress, 3);
			var year = new TextBlock
			{
				Text = entry.YearCreated == 0 ? "-" : entry.YearCreated.ToString(),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
				Height = 24
			};
			Grid.SetColumn(year, 4);
			var period = new ComboBox
			{
				Uid = $"period_{entry.Id}",
				SelectedIndex = entry.Period - 1,
				IsEditable = false,
				Visibility = entry.IsRegularOngoing ? Visibility.Collapsed : Visibility.Visible,
				VerticalAlignment = VerticalAlignment.Center,
				Width = 60,
				Height = 24,
				ToolTip = new ToolTip
				{
					Content = "Период выхода новых серий"
				}
			};
			Grid.SetColumn(period, 5);
			for (var i = 1; i <= 7; ++i)
			{
				period.Items.Add(new ComboBoxItem
				{
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
		private Brush GetHighlightBrush(Entry entry, bool isCurrent = false)
			=> isCurrent
				? entry.AreConditionsSatisfied
					? _greenHighlightBrush
					: entry.ExpectedEpisodes - entry.WatchedEpisodes > entry.EpisodesPerDay
						? _redHighlightBrush
						: _yellowHighlightBrush
				: entry.AreConditionsSatisfied
					? Brushes.Transparent
					: entry.ExpectedEpisodes - entry.WatchedEpisodes > entry.EpisodesPerDay
						? _redHighlightBrush
						: entry.CurrentWeekDays.Contains(Convert.ToInt32(DateTime.Now.DayOfWeek))
							? _yellowHighlightBrush
							: _redHighlightBrush;

		/// <summary>
		/// Get entry tooltip text.
		/// </summary>
		/// <param name="entry">Anime entry.</param>
		/// <returns>Entry tooltip text.</returns>
		private string GetEntryToolTip(Entry entry)
		{
			var animeTitle = _language == TitleLanguage.Russian ? entry.RussianName : entry.RomajiName;
			return animeTitle +
				   $"\n{(entry.IsOngoing ? "Онгоинг\n" : entry.YearCreated != 0 ? $"{entry.YearCreated}\n" : "")}{(entry.Score != 0 ? $"Оценка {entry.Score}\n" : "")}Просмотрено {entry.WatchedEpisodes}/{(entry.IsExcluded ? entry.TotalEpisodes : entry.ExpectedEpisodes)}";
		}

		/// <summary>
		/// Get progress text.
		/// </summary>
		/// <param name="entry">Anime entry.</param>
		/// <returns>Watched episodes / Expected episodes.</returns>
		private static string GetProgress(Entry entry)
			=> $"{entry.WatchedEpisodes} / {(entry.IsExcluded ? entry.TotalEpisodes : entry.ExpectedEpisodes)}";

		/// <summary>
		/// Get anime title with hyperlink.
		/// </summary>
		/// <param name="entry">Anime entry.</param>
		/// <returns>Title object.</returns>
		private TextBlock GetLinkTitle(Entry entry)
		{
			var animeTitle = _language == TitleLanguage.Russian ? entry.RussianName : entry.RomajiName;
			var setterHover = new Setter
			{
				Property = TextBlock.ForegroundProperty,
				Value = _linkActiveBrush
			};
			var trigger = new Trigger
			{
				Property = IsMouseOverProperty,
				Value = true,
				Setters = {setterHover}
			};
			var setterNormal = new Setter
			{
				Property = TextBlock.ForegroundProperty,
				Value = _linkNormalBrush
			};
			var style = new Style
			{
				Triggers = {trigger},
				Setters = {setterNormal}
			};
			var title = new TextBlock
			{
				Uid = $"linktitle{UidCounter}_{entry.Id}",
				Text = animeTitle,
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
		private static Grid GetPlaceholder()
		{
			var placeholder = new Grid
			{
				Height = SlotHeight,
				Visibility = Visibility.Collapsed
			};
			var nothingImg = new Image
			{
				Source = new BitmapImage(new Uri(UiImgDir + "nothing_here.png")),
				Height = SlotHeight,
				Opacity = 0.4,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			RenderOptions.SetBitmapScalingMode(nothingImg, BitmapScalingMode.HighQuality);
			RenderOptions.SetEdgeMode(nothingImg, EdgeMode.Aliased);
			var nothingText = new TextBlock
			{
				Text = "ПУСТОТА",
				Opacity = 0.3,
				FontSize = 72,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			placeholder.Children.Add(nothingImg);
			placeholder.Children.Add(nothingText);
			return placeholder;
		}

		/// <summary>
		/// Update titles after language change.
		/// </summary>
		private void UpdateTitles()
		{
			for (var i = 0; i < 7; ++i)
			{
				foreach (var cover in _calendarItems[i])
				{
					((TextBlock) cover.Children[CoverTitleIdx]).Text = _language == TitleLanguage.Russian
						? _scheduler[GetId(cover)].RussianName
						: _scheduler[GetId(cover)].RomajiName;
				}
			}

			foreach (var item in _listViewItems)
			{
				((TextBlock) item.Children[ListItemTitleIdx]).Text = _language == TitleLanguage.Russian
					? _scheduler[GetId(item)].RussianName
					: _scheduler[GetId(item)].RomajiName;
			}
		}

		/// <summary>
		/// Update colors after theme change. Coundn't make it without rebuilding entire UI.
		/// </summary>
		private void UpdateColors()
		{
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
			UpdateUi();
		}

		/// <summary>
		/// Update entry sorting in calendar and list.
		/// </summary>
		private void UpdateSorting()
		{
			var comparer = GetUiElementComparer();
			for (var i = 0; i < 7; ++i)
			{
				_calendarItems[i].Sort((Grid a, Grid b) => comparer(a, b));
				_slots[i].Children.Clear();
				foreach (var cover in _calendarItems[i])
				{
					_slots[i].Children.Add(cover);
				}
			}

			_listViewItems.Sort((Grid a, Grid b) => comparer(a, b));
			listPanel.Children.Clear();
			foreach (var item in _listViewItems)
			{
				listPanel.Children.Add(item);
			}
		}

		/// <summary>
		/// Light calendar update, does not remove and add new objects, only existing are changed.
		/// </summary>
		private void UpdateCalendar()
		{
			var date = DateTime.Now;
			var currentWeekDay = WeekDayConverter.ToWeekDayRu(date.DayOfWeek);
			if (!_weekStartNow)
			{
				date = date.AddDays(-currentWeekDay);
			}

			_excludedAll = true;
			// Calendar view cover visibility and coloring
			for (var weekDay = 0; weekDay < 7; ++weekDay)
			{
				var weekDayRu = WeekDayConverter.ToWeekDayRu(weekDay);
				var isCurrent = weekDayRu == currentWeekDay;
				if (_weekStartNow)
				{
					weekDayRu = (weekDayRu - currentWeekDay + 7) % 7;
				}

				foreach (Grid cover in _slots[weekDayRu].Children)
				{
					var entry = _scheduler[GetId(cover)];
					if (entry.GetWeekSchedule(date).Contains(weekDay))
					{
						cover.Visibility = Visibility.Visible;
						((ToolTip) cover.ToolTip).Content = GetEntryToolTip(entry);
						cover.Background = isCurrent ? GetHighlightBrush(entry, true) : null;
						if (!entry.IsExcluded)
							_excludedAll = false;
					} else
					{
						cover.Visibility = Visibility.Collapsed;
					}
				}
			}

			// Calendar day names, dates, and coloring
			for (var i = 0; i < 7; ++i)
			{
				var isCurrent = WeekDayConverter.ToWeekDayRu(date.DayOfWeek) == currentWeekDay;

				var empty = !_calendarItems[i].Exists(p => p.Visibility == Visibility.Visible);
				if (empty)
				{
					_slots[i].Visibility = Visibility.Collapsed;
					_nothingHerePlaceholders[i].Visibility = Visibility.Visible;
				} else
				{
					_slots[i].Visibility = Visibility.Visible;
					_nothingHerePlaceholders[i].Visibility = Visibility.Collapsed;
				}

				_slots[i].Background = isCurrent && !empty && _scheduler.AreConditionsSatisfied
					? _greenHighlightBrush
					: Brushes.Transparent;
				_borders[i].Background = _grayHighlightBrush;
				((Label) _borders[i].Children[0]).Foreground = ((Label) _borders[i].Children[1]).Foreground =
					isCurrent ? _mainFontBrush : _secFontBrush;
				((Label) _borders[i].Children[0]).Content = DayRu[WeekDayConverter.ToWeekDayRu(date.DayOfWeek)];
				((Label) _borders[i].Children[1]).Content =
					(isCurrent ? "Сегодня, " : "") + $"{date.Day} {MonthRu[date.Month - 1]}";
				date = date.AddDays(1);
			}

			// List view text and coloring
			foreach (var item in _listViewItems)
			{
				((TextBlock) item.Children[ListItemProgressIdx]).Text = GetProgress(_scheduler[GetId(item)]);
				item.Background = GetHighlightBrush(_scheduler[GetId(item)]);
			}

			// Other settings
			((ToolTip) buttonSelectAll.ToolTip).Content = _excludedAll ? "Включить все" : "Исключить все";

			includedEntriesTextBlock.Text = $"Включено в расписание: {_scheduler.Entries.Count(p => !p.IsExcluded)}";
		}

		/// <summary>
		/// Light UI update, does not remove and add new objects, only existing are changed.
		/// </summary>
		private void UpdateUiEasy()
		{
			UpdateSorting();
			UpdateCalendar();
		}

		/// <summary>
		/// Filter list items based on search query.
		/// </summary>
		/// <param name="search_query">Text to search in anime titles.</param>
		private void FilterList(string searchQuery)
		{
			foreach (Grid item in listPanel.Children)
			{
				var entry = _scheduler[GetId(item)];
				var isItemVisible = searchTextBox.Foreground == _inactiveTextboxBrush
								 || entry.RussianName.ToLower().Contains(searchQuery)
								 || entry.RomajiName.ToLower().Contains(searchQuery);
				item.Visibility = isItemVisible ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		/// <summary>
		/// Hide edit buttons when sync is going on.
		/// </summary>
		private void HideEditButtons()
		{
			foreach (var cover in _slots.Select(s => s.Children).OfType<Grid>())
			{
				cover.Children[CoverButtonIdx].Visibility = Visibility.Hidden;
			}

			foreach (Grid item in listPanel.Children)
			{
				item.Children[ListItemCheckboxIdx].IsEnabled = false;
				item.Children[ListItemPeriodIdx].IsEnabled = false;
			}
		}

		/// <summary>
		/// Show 'em after sync is done.
		/// </summary>
		private void ShowEditButtons()
		{
			foreach (Grid item in listPanel.Children)
			{
				item.Children[ListItemCheckboxIdx].IsEnabled = true;
				item.Children[ListItemPeriodIdx].IsEnabled = true;
			}
		}

		/// <summary>
		/// Mark sorting button as active.
		/// </summary>
		/// <param name="button">Button object.</param>
		private static void CheckSortButton(TextBlock button)
		{
			if (!button.Text.EndsWith(" ↓"))
			{
				button.Text += " ↓";
			}
		}

		/// <summary>
		/// Deactivate sorting button.
		/// </summary>
		/// <param name="button">Button object.</param>
		private static void UnCheckSortButton(TextBlock button)
		{
			if (button.Text.EndsWith(" ↓"))
			{
				button.Text = button.Text.Substring(0, button.Text.Length - 2);
			}
		}

		/// <summary>
		/// Provide random background for sync screen.
		/// </summary>
		/// <returns></returns>
		private static string GetRandomBackground()
		{
			var rand = new Random();
			return Backgrounds[rand.Next(Backgrounds.Length)];
		}

		/// <summary>
		/// Change app mode to sync when changing accounts.
		/// </summary>
		private void SyncMode()
		{
			_syncMode = true;
			mainGrid.Visibility = Visibility.Collapsed;
			syncGrid.Visibility = Visibility.Visible;
			var background = GetRandomBackground();
			if (background.Contains("_"))
			{
				var split = background.Split('_');
				background = split[0];
				var numbers = split[1].Split('-');
				var from = int.Parse(numbers[0]);
				var to = int.Parse(numbers[1]);
				_backgroundKeyFrames = new List<Brush>();
				for (var i = @from; i <= to; ++i)
				{
					_backgroundKeyFrames.Add(new ImageBrush
					{
						ImageSource = new BitmapImage(new Uri(UiImgDir + "screensaver/" + background + $"_{i}.jpg")),
						Stretch = Stretch.UniformToFill
					});
				}

				_animationTimer = new DispatcherTimer
				{
					Interval = new TimeSpan(0, 0, 0, 0, int.Parse(split[2]))
				};
				_animationTimer.Tick += Animation_Timer_Tick;
				syncGrid.Background = _backgroundKeyFrames[0];
				_animationTimer.Start();
			} else
			{
				syncGrid.Background = new ImageBrush
				{
					ImageSource = new BitmapImage(new Uri(UiImgDir + "screensaver/" + background + ".jpg")),
					Stretch = Stretch.UniformToFill
				};
			}
		}

		/// <summary>
		/// Change app mode to default.
		/// </summary>
		private void NormalMode()
		{
			_refreshTimer.Start();
			signInGrid.Visibility = Visibility.Collapsed;
			mainGrid.Visibility = Visibility.Visible;
			if (_listMode)
			{
				listView.Visibility = Visibility.Visible;
				mainScrollView.Visibility = Visibility.Collapsed;
			} else
			{
				listView.Visibility = Visibility.Collapsed;
				mainScrollView.Visibility = Visibility.Visible;
			}

			mainWindow.Width = NormWidth;
			mainWindow.Height = NormHeight;
			_mode = Mode.Normal;
			_running = true;
			SetWindowState();
			ScrollToCurrent();
		}

		/// <summary>
		/// Change app mode to sign in.
		/// </summary>
		private void SignInMode()
		{
			_refreshTimer.Stop();
			mainGrid.Visibility = Visibility.Collapsed;
			signInGrid.Visibility = Visibility.Visible;
			mainWindow.Width = SiWidth;
			mainWindow.Height = SiHeight;
			WindowState = WindowState.Normal;
			if (_accountName != "")
			{
				signInButtonEsc.Visibility = Visibility.Visible;
				nameTextBox.Focus();
				nameTextBox.Text = _accountName;
				nameTextBox.CaretIndex = _accountName.Length;
			} else
			{
				signInButtonEsc.Visibility = Visibility.Hidden;
			}

			var accounts = _data.GetAllContent("MyOngoings").ConvertAll(p => p.GetValue("Account"));
			if (!accounts.Contains(_accountName))
			{
				accounts.Add(_accountName);
			}

			accounts.Reverse();
			if (accounts.Count > 1)
			{
				accountsBox.Items.Clear();
				for (var i = 0; i < accounts.Count; ++i)
				{
					accountsBox.Items.Add(new ComboBoxItem
					{
						Content = accounts[i],
						IsSelected = accounts[i] == _accountName
					});
					if (accounts[i] == _accountName)
						accountsBox.SelectedIndex = i;
				}

				accountsBox.Items.Add(new ComboBoxItem
				{
					Content = "Добавить новый аккаунт...",
					IsSelected = false
				});
				accountsBox.Visibility = Visibility.Visible;
				nameTextBox.Visibility = Visibility.Collapsed;
			} else
			{
				accountsBox.Visibility = Visibility.Collapsed;
				nameTextBox.Visibility = Visibility.Visible;
			}

			_mode = Mode.SignIn;
			_running = false;
		}

		private void SetWindowState()
		{
			WindowState = _preferences.CheckValue("WindowState", "Normal") ? WindowState.Normal : WindowState.Maximized;
		}

		/// <summary>
		/// Scroll calendar to today's position.
		/// </summary>
		private void ScrollToCurrent()
		{
			var currentWeekDay = _weekStartNow ? 0 : WeekDayConverter.ToWeekDayRu(DateTime.Now.DayOfWeek);
			double totalSlotsHeight = 0;
			for (var i = 0; i < currentWeekDay; ++i)
			{
				totalSlotsHeight += Math.Max(_slots[i].ActualHeight, SlotHeight);
			}

			mainScrollView.ScrollToVerticalOffset(totalSlotsHeight + SlotBorderHeight * currentWeekDay);
		}

		/// <summary>
		/// Send Windows notification.
		/// </summary>
		/// <param name="text_to_show">Notification text.</param>
		private void SendNotification(string textToShow)
		{
			_ni.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
			_ni.BalloonTipText = textToShow;
			_ni.ShowBalloonTip(10000);
		}

		/// <summary>
		/// Exit app.
		/// </summary>
		private void Exit()
		{
			if (_mode == Mode.Refresh)
			{
				System.Windows.Forms.MessageBox.Show("Пожалуйста, дождитесь завершения синхронизации!");
				return;
			}

			_running = false;
			Close();
		}

		/// <summary>
		/// Dark mode helper function.
		/// </summary>
		private void AssignDarkModeDependencies()
		{
			_mainFontBrush = Brushes.White;
			_secFontBrush = Brushes.DarkGray;
			_editButtonNormalBrush = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0));
			_editButtonActiveBrush = Brushes.RoyalBlue;
			_greenHighlightBrush = new SolidColorBrush(Color.FromRgb(10, 60, 10));
			_yellowHighlightBrush = new SolidColorBrush(Color.FromRgb(100, 90, 0));
			_redHighlightBrush = new SolidColorBrush(Color.FromRgb(100, 23, 23));
			_grayHighlightBrush = new SolidColorBrush(Color.FromRgb(54, 54, 54));
			_linkNormalBrush = Brushes.LightSkyBlue;
			_linkActiveBrush = Brushes.Orange;
			_inactiveTextboxBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120));
		}

		/// <summary>
		/// Light mode helper function.
		/// </summary>
		private void AssignLightModeDependencies()
		{
			_mainFontBrush = Brushes.Black;
			_secFontBrush = Brushes.Gray;
			_editButtonNormalBrush = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0));
			_editButtonActiveBrush = Brushes.RoyalBlue;
			_greenHighlightBrush = Brushes.LightGreen;
			_yellowHighlightBrush = Brushes.Khaki;
			_redHighlightBrush = Brushes.LightSalmon;
			_grayHighlightBrush = Brushes.LightGray;
			_linkNormalBrush = Brushes.RoyalBlue;
			_linkActiveBrush = Brushes.Coral;
			_inactiveTextboxBrush = new SolidColorBrush(Color.FromRgb(0xDF, 0xDF, 0xDF));
		}

		/// <summary>
		/// To the dark side, **heavy breathing**.
		/// </summary>
		private void DarkMode()
		{
			_darkMode = true;
			AssignDarkModeDependencies();
			mainWindow.Background = listTopBar.Background = new SolidColorBrush(Color.FromRgb(34, 34, 34));
			mainWindow.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
			mainWindow.Foreground = _mainFontBrush;
			signInButtonEsc.Foreground = buttonSelectAll.Foreground = buttonSortProgress.Foreground =
				buttonSortScore.Foreground = buttonSortTitle.Foreground = buttonSortYear.Foreground = _linkNormalBrush;
			if (!searchTextBox.IsFocused)
			{
				searchTextBox.Foreground = _inactiveTextboxBrush;
			}

			if (!nameTextBox.IsFocused)
			{
				nameTextBox.Foreground = _inactiveTextboxBrush;
			}

			imageLogo.Source = Invert((BitmapSource) imageLogo.Source);
			mainWindow.Resources.Clear();
		}

		/// <summary>
		/// To the light side, YOda magister is waiting.
		/// </summary>
		private void LightMode()
		{
			_darkMode = false;
			AssignLightModeDependencies();
			mainWindow.Background = listTopBar.Background = Brushes.White;
			mainWindow.Foreground = _mainFontBrush;
			mainWindow.BorderBrush = null;
			signInButtonEsc.Foreground = buttonSelectAll.Foreground = buttonSortProgress.Foreground =
				buttonSortScore.Foreground = buttonSortTitle.Foreground = buttonSortYear.Foreground = _linkNormalBrush;
			if (!searchTextBox.IsFocused)
			{
				searchTextBox.Foreground = _inactiveTextboxBrush;
			}

			if (!nameTextBox.IsFocused)
			{
				nameTextBox.Foreground = _inactiveTextboxBrush;
			}

			imageLogo.Source = Invert((BitmapSource) imageLogo.Source);
			mainWindow.Resources.Add(typeof(Button), new Style() {TargetType = typeof(Button)});
			mainWindow.Resources.Add(typeof(TextBox), new Style() {TargetType = typeof(TextBox)});
			mainWindow.Resources.Add(typeof(Menu), new Style() {TargetType = typeof(Menu)});
			mainWindow.Resources.Add(typeof(Separator), new Style() {TargetType = typeof(Separator)});
			mainWindow.Resources.Add(typeof(MenuItem), new Style() {TargetType = typeof(MenuItem)});
			mainWindow.Resources.Add(typeof(ComboBox), new Style() {TargetType = typeof(ComboBox)});
			mainWindow.Resources.Add(typeof(System.Windows.Controls.Primitives.ScrollBar),
									 new Style() {TargetType = typeof(System.Windows.Controls.Primitives.ScrollBar)});
		}
	}
}