using System.Windows.Media.Imaging;

namespace YO.Windows
{
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
}