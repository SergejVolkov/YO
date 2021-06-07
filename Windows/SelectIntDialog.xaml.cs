using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YO {
    /// <summary>
    /// Interaction logic for SelectIntDialog.xaml
    /// </summary>
    public partial class SelectIntDialog : Window
    {
        int min_value, current_value;

        /// <summary>
        /// Construct select int value window.
        /// </summary>
        /// <param name="dark_mode">Dark theme flag.</param>
        /// <param name="title">Window title.</param>
        /// <param name="description">Dialog description.</param>
        /// <param name="min">Min allowed value.</param>
        /// <param name="max">Max allowed value.</param>
        /// <param name="current_value">Selected value.</param>
        /// <param name="mapping">Map values to custom strings.</param>
        public SelectIntDialog(bool dark_mode, string title, string description, int min, int max, int current_value, string[] mapping = null)
        {
            InitializeComponent();
            if (dark_mode)
                DarkMode();
            this.min_value = min;
            this.current_value = current_value;
            if (current_value < min || current_value > max)
            {
                current_value = min;
            }
            titleTextBox.Text = this.Title = title;
            descriptionTextBox.Text = description;
            min_value = min;
            for (int i = min; i <= max; ++i)
            {
                valueBox.Items.Add(new ComboBoxItem
                {
                    Content = (mapping == null ? i.ToString() : mapping[i - min]),
                    IsSelected = i == current_value
                });
            }
            valueBox.SelectedIndex = current_value - min;
        }

        /// <summary>
        /// Dark Side Of The Moon.
        /// </summary>
        void DarkMode()
        {
            this.Background = new SolidColorBrush(Color.FromRgb(34, 34, 34));
            this.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
            this.Foreground = Brushes.White;
            this.Resources.Clear();
        }

        private void ButtonEsc_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            if (Value != current_value)
            {
                DialogResult = true;
                return;
            }
            this.Close();
        }

        public int Value => valueBox.SelectedIndex + min_value;
    }
}
