using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace YO.Windows
{
	/// <summary>
	/// Interaction logic for AboutWindow.xaml
	/// </summary>
	// TODO: Refactor in MVVM pattern
	public partial class AboutWindow
	{
		/// <summary>
		/// Construct about window.
		/// </summary>
		/// <param name="darkMode">Dark theme flag.</param>
		/// <param name="showImage">Show shift animated Lum pic.</param>
		/// <param name="windowTitle">Window title.</param>
		/// <param name="title">Text title.</param>
		/// <param name="text">Main text.</param>
		/// <param name="notice">Right-aligned notice at the end.</param>
		/// <param name="width">Window width (optional).</param>
		public AboutWindow(bool darkMode, 
						   bool showImage, 
						   string windowTitle, 
						   string title, 
						   string text,
						   string notice, 
						   double width = 800.0)
		{
			InitializeComponent();
			if (darkMode)
			{
				DarkMode();
			}

			Width = width;
			Title = windowTitle;
			titleTextBox.Text = title;
			descriptionTextBox.Text = text;
			noticeTextBox.Text = notice;
			if (showImage)
			{
				var animation = new ThicknessAnimation
				{
					From = new Thickness(-30, 0, 0, 0),
					To = new Thickness(-330, 0, 0, 0),
					Duration = TimeSpan.FromSeconds(10),
					RepeatBehavior = RepeatBehavior.Forever,
					AutoReverse = true
				};
				uyImage.BeginAnimation(MarginProperty, animation);
			} else
			{
				imageGrid.Visibility = Visibility.Collapsed;
				mainGrid.ColumnDefinitions.RemoveAt(0);
			}
		}

		private void DarkMode()
		{
			Background = new SolidColorBrush(Color.FromRgb(34, 34, 34));
			BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
			descriptionTextBox.Foreground = Foreground = Brushes.White;
			Resources.Remove(typeof(System.Windows.Controls.Primitives.ScrollBar));
		}

		private void ButtonEsc_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}