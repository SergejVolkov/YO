using System.Windows.Media.Imaging;

namespace YO.Windows
{
	/// <summary>
	/// Stores anime posters with loaded flag to deal with sudden deletion of poster image file on disk.
	/// </summary>
	internal class Poster
	{
		/// <summary>
		/// Construct poster from image and optional flag.
		/// </summary>
		/// <param name="source">Poster image.</param>
		/// <param name="isLoaded">Loaded flag.</param>
		public Poster(BitmapSource source, 
					  bool isLoaded = true)
		{
			Source = source;
			IsLoaded = isLoaded;
		}

		public BitmapSource Source { get; }

		public bool IsLoaded { get; }
	}
}