using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace YO.Windows {
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window {
        /// <summary>
        /// Construct about window.
        /// </summary>
        /// <param name="dark_mode">Dark theme flag.</param>
        /// <param name="show_image">Show shift animated Lum pic.</param>
        /// <param name="window_title">Window title.</param>
        /// <param name="title">Text title.</param>
        /// <param name="text">Main text.</param>
        /// <param name="notice">Right-aligned notice at the end.</param>
        /// <param name="width">Window width (optional).</param>
        public AboutWindow(bool dark_mode, bool show_image, string window_title, string title, string text, string notice, double width = 800.0) {
            InitializeComponent();
            if (dark_mode)
                DarkMode();
            this.Width = width;
            this.Title = window_title;
            titleTextBox.Text = title;
            descriptionTextBox.Text = text;
            noticeTextBox.Text = notice;
            if (show_image) {
                ThicknessAnimation animation = new ThicknessAnimation {
                    From = new Thickness(-30, 0, 0, 0),
                    To = new Thickness(-330, 0, 0, 0),
                    Duration = TimeSpan.FromSeconds(10),
                    RepeatBehavior = RepeatBehavior.Forever,
                    AutoReverse = true
                };
                uyImage.BeginAnimation(Image.MarginProperty, animation);
            } else {
                imageGrid.Visibility = Visibility.Collapsed;
                mainGrid.ColumnDefinitions.RemoveAt(0);
            }
        }

        void DarkMode() {
            this.Background = new SolidColorBrush(Color.FromRgb(34, 34, 34));
            this.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
            descriptionTextBox.Foreground = this.Foreground = Brushes.White;
            this.Resources.Remove(typeof(System.Windows.Controls.Primitives.ScrollBar));
        }

        private void ButtonEsc_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }
    }
}
