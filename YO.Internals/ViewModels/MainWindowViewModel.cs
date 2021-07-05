using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;

namespace YO.Internals.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		public MainWindowViewModel(LoginViewModel loginViewModel,
								 ScheduleViewModel scheduleViewModel)
		{
			LoginViewModel = loginViewModel;
			ScheduleViewModel = scheduleViewModel;

			ShowLoginDialog = new Interaction<LoginViewModel, Unit>();

			OpenLoginDialog = ReactiveCommand.CreateFromTask(OpenLoginDialogImpl);
			RefreshList = ReactiveCommand.CreateFromTask(UpdateAnimeList);
		}
		
		public LoginViewModel LoginViewModel { get; }
		public ScheduleViewModel ScheduleViewModel { get; }
		
		public Interaction<LoginViewModel, Unit> ShowLoginDialog { get; }
		public ReactiveCommand<Unit, Unit> OpenLoginDialog { get; }
		public ReactiveCommand<Unit, Unit> RefreshList { get; }

		private async Task OpenLoginDialogImpl()
		{
			await ShowLoginDialog.Handle(LoginViewModel);
		}

		private async Task UpdateAnimeList()
		{
			await ScheduleViewModel.UpdateAnimeList();
		}
	}
}