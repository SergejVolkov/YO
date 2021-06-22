using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using YO.Modules;

namespace YO.Windows
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	// TODO: Refactor whole that class
	public partial class MainWindow
	{
		// Delays, periods, intervals
		private const int CoverDelay = 300; /* milliseconds */
		private const int AnimePageDelay = 500; /* milliseconds */
		private const int TooManyRequestsDelay = 1500; /* milliseconds */
		private const int AdditionalSyncSleep = 1500; /* milliseconds */
		private const int RefreshInterval = 5; /* minutes */
		private const int EveningHour = 20; /* hours */
		private const int FullRefreshPeriod = 1; /* days */
		
		// Children indexes to get things done with UI
		private const int CoverButtonIdx = 1;
		private const int CoverTitleIdx = 2;
		private const int ListItemCheckboxIdx = 0;
		private const int ListItemProgressIdx = 2;
		private const int ListItemPeriodIdx = 4;
		private const int ListItemTitleIdx = 5;
		
		// UI sizes and gaps
		private const double SiWidth = 520;
		private const double SiHeight = 235;
		private const double NormWidth = 1070;
		private const double NormHeight = 600;
		private const double SlotHeight = 300;
		private const double SlotBorderHeight = 45;
		private const double SlotTitleHeight = 50;
		private const double ShadowBlurRadius = 12;
		private const double SlotHorizGap = 18;
		private const double SlotVertGap = 12;
		private const double MaxPosterAspectRatio = 0.75;
		private const double EditIconHeight = 33;
		private const double EditIconGap = 3;
		
		private const string ShikiUrl = "https://shikimori.one/";
		private const string ImgUrl = "https://kawai.shikimori.one/system/animes/original/";
		private const string WatchingScorePartUrl = "/list/anime/mylist/watching,rewatching/order-by/rate_score";
		private const string WatchingYearPartUrl = "/list/anime/mylist/watching,rewatching/order-by/aired_on";
		private const string WatchingProgressPartUrl = "/list/anime/mylist/watching,rewatching/order-by/episodes";
		private const string WatchingAlphabetPartUrl = "/list/anime/mylist/watching,rewatching/order-by/name";
		
		private const string ResDir = "pack://application:,,,/Resources/";
		private const string CacheresDir = ResDir + "cache/";
		private const string UiImgDir = ResDir + "img/";
		
		// Notification icon tooltips
		private const string NormalStatus = "YO: Твои Онгоинги";
		private const string NoConnectionStatus = "YO: Оффлайн";
		private const string BusyStatus = "YO: Синхронизация...";
		
		// String resources
		private static readonly string[] MonthRu =
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

		private static readonly string[] DayRu =
		{
			"Понедельник",
			"Вторник",
			"Среда",
			"Четверг",
			"Пятница",
			"Суббота",
			"Воскресенье"
		};

		private static readonly string[] Backgrounds =
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

		// Urls
		private string _watchingPartUrl;
		private string _watchingUrl;
		private string _accountName;

		// Resources dirs
		private static string _appdata;
		private static string _tmpdir;

		// Cache xml tags
		private static Tag _preferences, _data;
		
		// Cached app params
		private bool _autorun;
		private bool _darkMode;
		private bool _weekStartNow;
		private bool _fullRefreshed;
		private bool _startNotified;
		private bool _eveningNotified;

		private DateTime _lastFullRefresh;
		private DateTime _lastStartupNotification;
		private DateTime _lastEveningNotification;
		private int _realOngoingDelay, _totalEpisodes;
		private TitleLanguage _language;

		private SortingMode _sortingMode;

		// Scheduler, timer, and program instance objects
		private Scheduler _scheduler;
		private DispatcherTimer _refreshTimer;
		private DispatcherTimer _animationTimer;
		private FileStream _instanceStream;

		// UI elements, images, brushes
		private System.Windows.Forms.NotifyIcon _ni;
		private System.Drawing.Icon _normalIcon, _noConnectionIcon, _busyIcon;
		private WrapPanel[] _slots;
		private Grid[] _borders, _nothingHerePlaceholders;
		private Dictionary<int, Poster> _posters;
		private List<Grid>[] _calendarItems;
		private List<Grid> _listViewItems;
		private Dictionary<int, bool> _excludedThisRun;
		private List<Brush> _backgroundKeyFrames;
		private BitmapSource _editIconImage;

		private Brush _editButtonNormalBrush;
		private Brush _editButtonActiveBrush;
		private Brush _greenHighlightBrush;
		private Brush _yellowHighlightBrush;
		private Brush _redHighlightBrush;
		private Brush _grayHighlightBrush;
		private Brush _linkNormalBrush;
		private Brush _linkActiveBrush;
		private Brush _mainFontBrush;
		private Brush _secFontBrush;
		private Brush _inactiveTextboxBrush;

		// Not cached app params
		private Mode _mode = Mode.Normal;
		private bool _listMode;
		private bool _running = true;
		private bool _excludedAll;
		private bool _excludingTaskRunning;
		private bool _syncMode;

		private int _uidCounter, _currentFrame;

		/// <summary>
		/// App constructor, check if already running, load preferences and display schedule.
		/// </summary>
		public MainWindow()
		{
			InitializeComponent();

			_appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\YO\\";
			_tmpdir = _appdata + "tmp\\";

			PrepareInstance();
			PrepareUi();
			PrepareCache();
			PrepareSystem();
			_refreshTimer.Start();

			PrepareCachedUi();
			RefreshAsync();
		}
	}
}