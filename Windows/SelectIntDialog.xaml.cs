using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YO.Windows
{
	/// <summary>
	/// Interaction logic for SelectIntDialog.xaml
	/// </summary>
	// TODO: Refactor in MVVM pattern
	public partial class SelectIntDialog
	{
		private readonly int _minValue;
		private readonly int _currentValue;

		/// <summary>
		/// Construct select int value window.
		/// </summary>
		/// <param name="darkMode">Dark theme flag.</param>
		/// <param name="title">Window title.</param>
		/// <param name="description">Dialog description.</param>
		/// <param name="min">Min allowed value.</param>
		/// <param name="max">Max allowed value.</param>
		/// <param name="currentValue">Selected value.</param>
		/// <param name="mapping">Map values to custom strings.</param>
		public SelectIntDialog(bool darkMode,
							   string title,
							   string description,
							   int min,
							   int max,
							   int currentValue,
							   IReadOnlyList<string> mapping = null)
		{
			InitializeComponent();
			if (darkMode)
			{
				DarkMode();
			}

			_minValue = min;
			_currentValue = currentValue;
			if (currentValue < min || currentValue > max)
			{
				currentValue = min;
			}

			titleTextBox.Text = Title = title;
			descriptionTextBox.Text = description;
			_minValue = min;
			for (var i = min; i <= max; ++i)
			{
				valueBox.Items.Add(new ComboBoxItem
				{
					Content = mapping == null 
						? i.ToString() 
						: mapping[i - min],
					IsSelected = i == currentValue
				});
			}

			valueBox.SelectedIndex = currentValue - min;
		}
		
		public int Value 
			=> valueBox.SelectedIndex + _minValue;

		/// <summary>
		/// Dark Side Of The Moon.
		/// </summary>
		private void DarkMode()
		{
			Background = new SolidColorBrush(Color.FromRgb(34, 34, 34));
			BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
			Foreground = Brushes.White;
			Resources.Clear();
		}

		private void ButtonEsc_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void ButtonOK_Click(object sender, RoutedEventArgs e)
		{
			if (Value != _currentValue)
			{
				DialogResult = true;
				return;
			}

			Close();
		}
	}
}