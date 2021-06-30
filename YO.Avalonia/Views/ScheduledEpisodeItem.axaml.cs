using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using YO.Internals.ViewModels;

namespace YO.Avalonia.Views
{
	public class ScheduledEpisodeItem : ReactiveUserControl<ScheduledEpisodeViewModel>
	{
		public ScheduledEpisodeItem()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}