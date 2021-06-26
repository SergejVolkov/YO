using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using YO.Internals.ViewModels;

namespace YO.Avalonia.Views
{
	public class MainWindow : ReactiveWindow<MainWindowViewModel>
	{
		public MainWindow()
		{
			InitializeComponent();
		#if DEBUG
			this.AttachDevTools();
		#endif
			
			this.WhenActivated(d => d(ViewModel!.ShowLoginDialog.RegisterHandler(ShowLoginDialogAsync)));
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
		
		private async Task ShowLoginDialogAsync(InteractionContext<LoginViewModel, Unit> interaction)
		{
			var dialog = new LoginWindow
			{
				ViewModel = interaction.Input
			};

			await dialog.ShowDialog(this);
			interaction.SetOutput(Unit.Default);
		}
	}
}