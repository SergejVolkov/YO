using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using YO.Modules;

namespace YO.Windows
{
	public partial class MainWindow
	{
		/// <summary>
		/// Check for internet connection.
		/// </summary>
		/// <param name="timeoutMs">Time to wait for reply from server.</param>
		/// <param name="url">Which server to use.</param>
		/// <returns>Internet connection status.</returns>
		private static bool CheckForInternetConnection(int timeoutMs = 10000, string url = null)
		{
			try
			{
				if (url == null)
				{
					url = "http://www.gstatic.com/generate_204";
				}

				var request = (HttpWebRequest) WebRequest.Create(url);
				request.KeepAlive = false;
				request.Timeout = timeoutMs;
				using ((HttpWebResponse) request.GetResponse())
				{ }

				return true;
			} catch
			{
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
		private static string ReadHtml(WebClient client, string href, int maxIter = 10)
		{
			var read = false;

			var htmlCode = "";
			for (var iter = 0; !read && iter < maxIter; ++iter)
			{
				try
				{
					htmlCode = client.DownloadString(href);
					read = true;
				} catch
				{
					if (iter == maxIter - 1)
					{
						throw new TimeoutException($"Request timed out after {maxIter} attempts...");
					}

					Thread.Sleep(TooManyRequestsDelay);
				}
			}

			return htmlCode;
		}

		/// <summary>
		/// Parse anime ongoing day of week when new series are released from HTML.
		/// </summary>
		/// <param name="htmlCode">Anime page HTML code.</param>
		/// <returns>If succesful, return day of week, otherwise return Scheduler.WNotScheduled</returns>
		private static int GetWeekDayFromHtml(string htmlCode)
		{
			var start = htmlCode.IndexOf("<div class=\'key\'>Следующий эпизод:</div>");
			if (start < 0) return Scheduler.WNotScheduled;
			htmlCode = htmlCode.Substring(start + "<div class=\'key\'>Следующий эпизод:</div>".Length);
			start = htmlCode.IndexOf("<div class=\'value\'>") + "<div class=\'value\'>".Length;
			var length = htmlCode.IndexOf("</div>") - start;
			htmlCode = htmlCode.Substring(start, length).ToLower();
			var sdate = htmlCode.Split(' ');
			try
			{
				int mDay = int.Parse(sdate[0]), month = Array.IndexOf(MonthRu, sdate[1]) + 1;
				var now = DateTime.Now;
				var year = now.Year;
				if (month == 1 && now.Month == 12) ++year;
				var date = new DateTime(year, month, mDay);
				if (date.Subtract(now).Days < 7)
				{
					return Convert.ToInt32(date.DayOfWeek);
				}
			} catch
			{ }

			return Scheduler.WNotScheduled;
		}

		/// <summary>
		/// Parse anime year of creation from HTML.
		/// </summary>
		/// <param name="htmlCode">Anime page HTML code.</param>
		/// <returns>If succesful, return year of creation, otherwise return 0.</returns>
		private static int GetYearCreatedFromHtml(string htmlCode)
		{
			var start = htmlCode.IndexOf("<div class=\"l-content\"><div class=\"block\">");
			if (start < 0) return 0;
			htmlCode = htmlCode.Substring(start + "<div class=\"l-content\"><div class=\"block\">".Length);
			var length = htmlCode.IndexOf("<div class=\"b-db_entry\">");
			File.WriteAllText(_appdata + "tmp\\watching.html", htmlCode.Substring(0, length));
			List<Tag> tags;
			using (var reader = new TagReader(_appdata + "tmp\\watching.html", true))
			{
				tags = reader.Read();
			}

			try
			{
				return int.Parse(tags.Find(p => p.CheckValue("itemprop", "dateCreated")).GetValue("content")
									 .Split('-')[0]);
			} catch
			{
				return 0;
			}
		}

		/// <summary>
		/// Heart of the app, syncs shikimori anime list and scheduler. Do not call this method directly, use RefreshAsync so the app won't freeze.
		/// </summary>
		/// <returns>Flag indicating whether any changes has been made to scheduler's data.</returns>
		private bool Refresh()
		{
			var changesMade = false;

			string rawHtmlCode;
			var client = new WebClient
			{
				Encoding = System.Text.Encoding.UTF8
			};
			try
			{
				rawHtmlCode = client.DownloadString(_watchingUrl);
			} catch
			{
				_mode = Mode.Offline;
				return false;
			}

			int start = rawHtmlCode.IndexOf("<tbody class=\"entries\">"),
				length = rawHtmlCode.IndexOf("</tbody>") + "</tbody>".Length - start;
			if (start < 0)
			{
				return false;
			}

			File.WriteAllText(_appdata + "tmp\\watching.html", rawHtmlCode.Substring(start, length));
			Tag htmlCode;
			using (var reader = new TagReader(_appdata + "tmp\\watching.html", true))
			{
				htmlCode = reader.Read()[0];
			}

			var offline = false;
			var deadEntries = new List<int>(_scheduler.Ids);
			foreach (var item in htmlCode.Content)
			{
				string romajiName, russianName, type, href;
				int id, totalEp, watchedEp, score;
				bool isOngoing;
				try
				{
					var episodesTag = item.Content
										  .Find(p => p.ContainsContent("span")
												  && p.GetContent("span").ContainsKey("data-field")
												  && p.GetContent("span").CheckValue("data-field", "episodes"));
					var epDataTag = episodesTag.Content.Find(p => p.ContainsKey("class")
															   && p.CheckValue("class", "current-value"));
					var scoreTag = item.Content
									   .Find(p => p.ContainsContent("span")
											   && p.GetContent("span").ContainsKey("data-field")
											   && p.GetContent("span").CheckValue("data-field", "score"))
									   .GetContent("span");
					var statusTag = item.Content
										.Find(p => p.ContainsContent("a"));
					var typeTag = item.Content
									  .Last();
					id = item.GetIntValue("data-target_id");
					try
					{
						score = scoreTag.GetIntValue();
					} catch
					{
						score = 0;
					}

					romajiName = item.GetValue("data-target_name");
					russianName = item.GetValue("data-target_russian");
					type = typeTag.GetValue();
					isOngoing = statusTag.Content.Exists(p => p.CheckValue("class", "ongoing"));
					href = ShikiUrl + statusTag.GetContent("a").GetValue("href").Substring(1);
					watchedEp = epDataTag.Content[0].GetIntValue();
					if (isOngoing)
					{
						totalEp = episodesTag.Content
											 .Find(p => p.ContainsKey("class")
													 && p.CheckValue("class", "misc-value"))
											 .GetIntValue();
					} else
					{
						totalEp = epDataTag.GetIntValue("data-max");
					}
				} catch
				{
					throw new Exception("Unknown error during html parsing!");
				}

				if (!Scheduler.IsSupported(type))
				{
					continue;
				}

				var alreadyPresent = _scheduler.Contains(id);
				int weekDay = Scheduler.WNotScheduled, yearCreated = 0;
				if (isOngoing && !_fullRefreshed || !alreadyPresent)
				{
					Thread.Sleep(AnimePageDelay);
					try
					{
						rawHtmlCode = ReadHtml(client, href);
					} catch
					{
						offline = true;
						break;
					}

					if (isOngoing)
					{
						weekDay = GetWeekDayFromHtml(rawHtmlCode);
					}

					if (!alreadyPresent)
					{
						yearCreated = GetYearCreatedFromHtml(rawHtmlCode);
					}
				}

				if (alreadyPresent)
				{
					deadEntries.Remove(id);
					var entry = _scheduler[id];

					changesMade |= entry.WatchedEpisodes != watchedEp;
					_totalEpisodes += watchedEp - entry.WatchedEpisodes;
					entry.WatchedEpisodes = watchedEp;
					changesMade |= entry.TotalEpisodes != totalEp;
					entry.TotalEpisodes = totalEp;
					changesMade |= entry.Score != score;
					entry.Score = score;
					changesMade |= entry.RomajiName != romajiName;
					entry.RomajiName = romajiName;
					changesMade |= entry.RussianName != russianName;
					entry.RussianName = russianName;
					if (!_fullRefreshed)
					{
						var status = entry.Status;
						var toReschedule = false;
						if (isOngoing)
						{
							if (weekDay < 0)
							{
								status = AnimeStatus.PendingOngoing;
								if (entry.Status == AnimeStatus.RegularOngoing)
								{
									toReschedule = true;
								}
							} else if (!(status == AnimeStatus.PendingOngoing && entry.OverrideRegularOngoing))
							{
								status = AnimeStatus.RegularOngoing;
								changesMade |= entry.WeekDay != weekDay;
								entry.WeekDay = weekDay;
							}
						} else
						{
							status = AnimeStatus.Released;
							switch (entry.Status)
							{
								case AnimeStatus.RegularOngoing:
									toReschedule = true;
									break;
								case AnimeStatus.PendingOngoing when entry.OverrideRegularOngoing:
									changesMade = true;
									entry.OverrideRegularOngoing = false;
									break;
							}
						}

						changesMade |= entry.Status != status;
						entry.Status = status;
						if (toReschedule)
						{
							_scheduler.MarkReschedule(id);
						}
					}
				} else
				{
					changesMade = true;
					_scheduler.Add(new Entry(id,
											 romajiName,
											 russianName,
											 type,
											 href,
											 totalEp,
											 watchedEp,
											 score,
											 yearCreated,
											 isOngoing,
											 weekDay));
				}

				var coverPath = _tmpdir + id + ".jpg";
				if (!File.Exists(coverPath))
				{
					Thread.Sleep(CoverDelay);
					try
					{
						client.DownloadFile(new Uri(ImgUrl + id.ToString() + ".jpg"), coverPath);
					} catch
					{
						CopyImgRes("no_cover.jpg", coverPath);
					}
				}
			}

			if (!_fullRefreshed && !offline)
			{
				_fullRefreshed = true;
				_lastFullRefresh = DateTime.Now;
			}

			foreach (var key in deadEntries)
			{
				changesMade = true;
				_scheduler.MarkRemove(key);
			}

			if (changesMade)
			{
				_scheduler.AssignPeriods();
				_scheduler.Schedule();
				UpdateDataCache();
			}

			if (_totalEpisodes < 0)
				_totalEpisodes = 0;
			if (offline)
				_mode = Mode.Offline;
			return changesMade;
		}

		/// <summary>
		/// Wait some additional time after refresh when in sign in mode, so the genga image won't flash in a blink of an eye.
		/// </summary>
		/// <returns>Flag indicating whether any changes has been made.</returns>
		private bool RefreshWithWait()
		{
			var startRefresh = DateTime.Now;
			var reply = Refresh();
			if (DateTime.Now.Subtract(startRefresh).Milliseconds < AdditionalSyncSleep)
			{
				Thread.Sleep(AdditionalSyncSleep);
			}

			return reply;
		}

		/// <summary>
		/// Async refresh method, does some extra work before and after waiting for normal Refresh method to execute. This method can be used in app and is called by refresh timer every refresh_interval minutes.
		/// </summary>
		private async void RefreshAsync()
		{
			if (_mode == Mode.Normal
			 || _mode == Mode.Offline)
			{
				Title = _ni.Text = BusyStatus;
				_ni.Icon = _busyIcon;
				HideEditButtons();
				_mode = Mode.Refresh;
				bool toRefresh = !_fullRefreshed, changesMade = false;
				var lastRefreshTime = DateTime.Now.AddMinutes(-RefreshInterval);
				await Task.Run(() => changesMade = _syncMode ? RefreshWithWait() : Refresh());
				if (toRefresh && _mode != Mode.Offline)
				{
					UpdatePrefCache();
				}

				if (changesMade || _syncMode)
				{
					UpdateUi();
				} else
				{
					ShowEditButtons();
					if (lastRefreshTime.DayOfWeek != DateTime.Now.DayOfWeek)
						UpdateUiEasy();
				}

				if (_syncMode)
				{
					_syncMode = false;
					if (_animationTimer != null)
					{
						_animationTimer.Stop();
						_backgroundKeyFrames.Clear();
					}

					syncGrid.Visibility = Visibility.Collapsed;
					mainGrid.Visibility = Visibility.Visible;
				}

				if (_mode == Mode.Offline)
				{
					Title = _ni.Text = NoConnectionStatus;
					_ni.Icon = _noConnectionIcon;
				} else
				{
					_mode = Mode.Normal;
					Title = _ni.Text = NormalStatus;
					_ni.Icon = _normalIcon;
				}

				SendNotifications();
			}
		}

		/// <summary>
		/// Construct and send windows notifications.
		/// </summary>
		private void SendNotifications()
		{
			if (!_startNotified)
			{
				_startNotified = true;
				_lastStartupNotification = DateTime.Now;
				if (!_scheduler.AreConditionsSatisfied)
				{
					SendNotification("Аниме на сегодня:\n" + GetCurrentAnimeListText());
				} else if (_scheduler.Count == 0)
				{
					SendNotification(
						"Ваш список \"Смотрю\" пуст!\nЧтобы добавить аниме, нажмите\nФайл → Открыть Шикимори");
				} else
				{
					TrySendOverdueNotification();
				}

				UpdatePrefCache();
			} else if (!_eveningNotified && DateTime.Now.Hour >= EveningHour)
			{
				_eveningNotified = true;
				_lastEveningNotification = DateTime.Now;
				if (!_scheduler.AreConditionsSatisfied)
				{
					SendNotification("У вас остались непросмотренные серии!\nАниме на вечер:\n" +
									 GetCurrentAnimeListText());
				} else
				{
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
		private string GetEntryNotificationText(Entry entry)
		{
			var text = $"{(_language == TitleLanguage.Russian ? entry.RussianName : entry.RomajiName)} ";
			if (entry.ExpectedEpisodes - entry.WatchedEpisodes > 1)
			{
				text += $"#{entry.WatchedEpisodes + 1}-{entry.ExpectedEpisodes}";
			} else
			{
				text += $"#{entry.WatchedEpisodes + 1}";
			}

			return text;
		}

		/// <summary>
		/// Concat entry strings of all the current animes.
		/// </summary>
		/// <returns>String that is used in notifications.</returns>
		private string GetCurrentAnimeListText()
		{
			var text = "";
			foreach (var entry in _scheduler.AnimesToWatchToday)
			{
				if (text != "")
					text += "\n";
				text += GetEntryNotificationText(entry);
			}

			return text;
		}

		/// <summary>
		/// If you are all done, try to kindly suggest watching overdue animes.
		/// </summary>
		private void TrySendOverdueNotification()
		{
			var overdue = _scheduler.Entries.Where(p => !p.IsExcluded && !p.AreConditionsSatisfied);
			if (!overdue.Any())
			{
				return;
			}

			var entry = overdue.ElementAt(new Random().Next(overdue.Count()));
			var text = !_scheduler.AnimesToday.Any()
				? $"На сегодня нет запланированных аниме, но можно посмотреть просроченные!\nКак насчет {GetEntryNotificationText(entry)} ?"
				: $"Отлично, вы посмотрели все аниме на сегодня!\nТеперь попробуйте нагнать просроченные тайтлы, например {GetEntryNotificationText(entry)}";
			SendNotification(text);
		}

		/// <summary>
		/// Get comparer to sort UI elements.
		/// </summary>
		/// <returns>Comparer of Grids.</returns>
		private Comparison<Grid> GetUiElementComparer()
		{
			var entryComparer = GetEntryComparer();
			return (a, b) => entryComparer(_scheduler[GetId(a)], _scheduler[GetId(b)]);
		}

		/// <summary>
		/// Get comparer to sort entries.
		/// </summary>
		/// <returns>Comparer of entries.</returns>
		private Comparison<Entry> GetEntryComparer()
		{
			var titleComparer = _language == TitleLanguage.Russian
				? (Comparison<Entry>) ((a, b) => StringComparer.InvariantCulture.Compare(a.RussianName, b.RussianName))
				: (a, b) => StringComparer.InvariantCulture.Compare(a.RomajiName, b.RomajiName);

			switch (_sortingMode)
			{
				case SortingMode.Score:
					return (a, b) =>
					{
						var comparison = a.Score.CompareTo(b.Score);
						return comparison == 0 ? titleComparer(a, b) : comparison;
					};
				case SortingMode.Year:
					return (a, b) =>
					{
						var comparison = a.YearCreated.CompareTo(b.YearCreated);
						return comparison == 0 ? titleComparer(a, b) : comparison;
					};
				case SortingMode.Progress:
					return (a, b) =>
					{
						if (a.IsExcluded && b.IsExcluded)
						{
							var aEpisodesLeft = a.TotalEpisodes - a.WatchedEpisodes;
							var bEpisodesLeft = b.TotalEpisodes - b.WatchedEpisodes;
							var comparison = aEpisodesLeft.CompareTo(bEpisodesLeft);
							return comparison == 0 ? titleComparer(a, b) : comparison;
						}

						if (a.IsExcluded)
						{
							return 1;
						}

						if (b.IsExcluded)
						{
							return -1;
						}

						if (a.ExpectedEpisodes - a.WatchedEpisodes == b.ExpectedEpisodes - b.WatchedEpisodes)
						{
							return titleComparer(a, b);
						}

						if (a.ExpectedEpisodes - a.WatchedEpisodes > b.ExpectedEpisodes - b.WatchedEpisodes)
						{
							return -1;
						}

						return 1;
					};
				default:
					return titleComparer;
			}
		}

		private void UpdateWatchingUrl()
		{
			_watchingUrl = ShikiUrl + WebUtility.UrlEncode(_accountName) + _watchingPartUrl;
		}

		private void OpenShiki()
		{
			if (_mode != Mode.SignIn)
			{
				OpenUrl(_watchingUrl);
			}
		}

		private static void OpenUrl(string url)
			=> System.Diagnostics.Process.Start(url);

		/// <summary>
		/// Install app to Windows startup.
		/// </summary>
		private void InstallOnStartUp()
		{
			try
			{
				var key =
					Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",
																	true);
				var curAssembly = Assembly.GetExecutingAssembly();
				key.SetValue(curAssembly.GetName().Name, curAssembly.Location);
				_autorun = true;
			} catch
			{
				System.Windows.Forms.MessageBox.Show("Something went wrong...");
				_autorun = false;
				startupBarItem.IsChecked = false;
			}
		}

		/// <summary>
		/// Remove app from startup.
		/// </summary>
		private void UnInstallOnStartUp()
		{
			try
			{
				var key =
					Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",
																	true);
				var curAssembly = Assembly.GetExecutingAssembly();
				key.DeleteValue(curAssembly.GetName().Name);
			} catch
			{
				System.Windows.Forms.MessageBox.Show("Something went wrong...");
			}

			_autorun = false;
			startupBarItem.IsChecked = false;
		}

		/// <summary>
		/// Check if another copy of the app is already running.
		/// </summary>
		/// <returns>Already running flag.</returns>
		private bool CheckInstance()
		{
			if (File.Exists(_tmpdir + "running") && IsFileLocked(new FileInfo(_tmpdir + "running")))
			{
				return true;
			}

			if (!Directory.Exists(_tmpdir))
			{
				Directory.CreateDirectory(_tmpdir);
			}

			_instanceStream = File.Open(_tmpdir + "running", FileMode.Create);
			return false;
		}

		/// <summary>
		/// Parse entry ID from UI element Uid.
		/// </summary>
		/// <param name="sender">UI element with Uid set.</param>
		/// <returns>Entry ID.</returns>
		private int GetId(object sender)
			=> int.Parse(((UIElement) sender).Uid.Split('_')[1]);

		/// <summary>
		/// Helps assign unique Uids for UI elements.
		/// </summary>
		private int UidCounter
		{
			get => _uidCounter++;
			set => _uidCounter = value;
		}

		/// <summary>
		/// Check if file is in use by another process.
		/// </summary>
		/// <param name="file">Path to file.</param>
		/// <returns>Already in use flag.</returns>
		/* https://stackoverflow.com/questions/876473/is-there-a-way-to-check-if-a-file-is-in-use by ChrisW */
		private static bool IsFileLocked(FileInfo file)
		{
			try
			{
				using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
				{
					stream.Close();
				}
			} catch (IOException)
			{
				//the file is unavailable because it is:
				//still being written to
				//or being processed by another thread
				//or does not exist (has already been processed)
				return true;
			}

			//file is not locked
			return false;
		}

		
		/// <summary>
		/// Invert colors of an image.
		/// </summary>
		/// <param name="source">Input image.</param>
		/// <returns>Inverted image.</returns>
		/* https://stackoverflow.com/questions/40979793/how-to-invert-an-image by Patric */
		private static BitmapSource Invert(BitmapSource source)
		{
			// Calculate stride of source
			var stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;

			// Create data array to hold source pixel data
			var length = stride * source.PixelHeight;
			var data = new byte[length];

			// Copy source image pixels to the data array
			source.CopyPixels(data, stride, 0);

			// Change this loop for other formats
			for (var i = 0; i < length; i += 4)
			{
				data[i] = (byte) (255 - data[i]); //R
				data[i + 1] = (byte) (255 - data[i + 1]); //G
				data[i + 2] = (byte) (255 - data[i + 2]); //B
				//data[i + 3] = (byte)(255 - data[i + 3]); //A
			}

			// Create a new BitmapSource from the inverted pixel buffer
			return BitmapSource.Create(
				source.PixelWidth, source.PixelHeight,
				source.DpiX, source.DpiY, source.Format,
				null, data, stride);
		}
	}
}