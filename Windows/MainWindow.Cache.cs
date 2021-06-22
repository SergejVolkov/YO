using System;
using System.Globalization;
using System.IO;
using System.Windows;
using YO.Modules;

namespace YO.Windows
{
	public partial class MainWindow
	{
		/// <summary>
		/// Check if cache files exist and create default files otherwise.
		/// </summary>
		/// <returns>Flag indicating whether the app runs for the first time.</returns>
		private static bool CheckFirstRun()
		{
			var reply = false;
			if (!File.Exists(_appdata + "preferences")
			 && !File.Exists(_appdata + "data"))
			{
				FirstRun();
				reply = true;
			} else if (!File.Exists(_appdata + "preferences"))
			{
				CopyCache("preferences");
			} else if (!File.Exists(_appdata + "data"))
			{
				CopyCache("data");
			}

			if (!Directory.Exists(_tmpdir))
			{
				Directory.CreateDirectory(_tmpdir);
			}

			_preferences = ReadCache("preferences");
			_data = ReadCache("data");
			return reply;
		}

		/// <summary>
		/// Create default cache files.
		/// </summary>
		private static void FirstRun()
		{
			Directory.CreateDirectory(_tmpdir);
			CopyCache("preferences");
			CopyCache("data");
		}

		/// <summary>
		/// Load cache file from disk.
		/// </summary>
		/// <param name="key">Cache file name.</param>
		/// <returns>Loaded cache xml tag.</returns>
		private static Tag ReadCache(string key)
		{
			TagReader reader;
			try
			{
				reader = new TagReader(_appdata + key);
			} catch
			{
				CopyCache(key);
				reader = new TagReader(_appdata + key);
			}

			try
			{
				var cache = reader.Read()[0];
				reader.Dispose();
				if (cache.Name.ToLower() != key) throw new OperationCanceledException();
				return cache;
			} catch
			{
				throw new CacheFileCorruptedException(key);
			}
		}

		/// <summary>
		/// Copy any resource to AppData folder.
		/// </summary>
		/// <param name="resource">Internal resource file.</param>
		/// <param name="path">Destination path.</param>
		private static void CopyRes(string resource, string path)
		{
			var resourceInfo = Application.GetResourceStream(new Uri(resource));
			var cachestream = new FileStream(path, FileMode.Create);
			if (resourceInfo != null)
			{
				resourceInfo.Stream.CopyTo(cachestream);
				cachestream.Flush();
				cachestream.Dispose();
				resourceInfo.Stream.Dispose();
			}
		}

		/// <summary>
		/// Copy default cache file to AppData folder.
		/// </summary>
		/// <param name="key">Cache file name.</param>
		private static void CopyCache(string key)
			=> CopyRes(CacheresDir + key, _appdata + key);

		/// <summary>
		/// Copy image resource to AppData folder.
		/// </summary>
		/// <param name="key">Image resource name.</param>
		/// <param name="path">Destination path.</param>
		private static void CopyImgRes(string key, string path)
			=> CopyRes(UiImgDir + key, path);

		/// <summary>
		/// Save cache to disk.
		/// </summary>
		/// <param name="cache">Xml tag with cache content.</param>
		private static void WriteCache(Tag cache)
		{
			var writer = new TagWriter(_appdata + cache.Name.ToLower());
			writer.WriteTag(cache);
			writer.Dispose();
		}

		/// <summary>
		/// Update and save preferences to disk.
		/// </summary>
		private void UpdatePrefCache()
		{
			_preferences.SetValue("WindowState", WindowState == WindowState.Maximized ? "Maximized" : "Normal");

			var sortingPreference = "Alphabet";
			switch (_sortingMode)
			{
				case SortingMode.Score:
					sortingPreference = "Score";
					break;
				case SortingMode.Year:
					sortingPreference = "Year";
					break;
				case SortingMode.Progress:
					sortingPreference = "Progress";
					break;
			}

			_preferences.SetValue("Sorting", sortingPreference);


			_preferences.SetValue("StartWeekFrom", _weekStartNow ? "Now" : "Monday");

			_preferences.SetValue("Autorun", _autorun.ToString());

			_preferences.SetValue("Language", _language == TitleLanguage.Russian ? "Russian" : "Romaji");

			_preferences.SetValue("Theme", _darkMode ? "Dark" : "Light");

			_preferences.SetValue("RealOngoingDelay", _realOngoingDelay.ToString());
			_preferences.SetValue("LastFullRefresh", _lastFullRefresh.ToString(CultureInfo.InvariantCulture));
			_preferences.SetValue("LastStartupNotification",
								  _lastStartupNotification.ToString(CultureInfo.InvariantCulture));
			_preferences.SetValue("LastEveningNotification",
								  _lastEveningNotification.ToString(CultureInfo.InvariantCulture));

			WriteCache(_preferences);
		}

		/// <summary>
		/// Update and save data to disk.
		/// </summary>
		private void UpdateDataCache()
		{
			_data.GetContent("AccountInfo").SetValue("Name", _accountName);
			_data.GetContent("StatsInfo").SetValue("TotalEpisodes", _totalEpisodes.ToString());

			if (_accountName != "")
			{
				var scheduleTag = _scheduler.Serialize();
				scheduleTag.SetValue("Account", _accountName);
				_data.Content.RemoveAll(p => p.Name == "MyOngoings"
										  && p.CheckValue("Account", _accountName));
				_data.Content.Add(scheduleTag);
			}

			WriteCache(_data);
		}

		/// <summary>
		/// Update and save everything to disk.
		/// </summary>
		private void UpdateCache()
		{
			UpdatePrefCache();
			UpdateDataCache();
		}

		/// <summary>
		/// Load schedule from cache.
		/// </summary>
		private void LoadSchedule()
		{
			var cachedData = _data.Content.Find(p => p.Name == "MyOngoings"
												  && p.CheckValue("Account", _accountName));
			_scheduler = new Scheduler(cachedData, _realOngoingDelay);
		}
	}
}